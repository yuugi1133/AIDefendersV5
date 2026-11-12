import argparse
import json
import shutil
import sys
from pathlib import Path


def write_result(path, payload):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(payload, indent=2, ensure_ascii=False)
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(text, encoding="utf-8")
    try:
        tmp.replace(path)
    except OSError:
        path.write_text(text, encoding="utf-8")
        if tmp.exists():
            tmp.unlink()


def as_float(value, default=0.0):
    try:
        if value is None:
            return default
        return float(value)
    except (TypeError, ValueError):
        return default


def pick_metric(data, *keys):
    if not isinstance(data, dict):
        return 0.0
    for key in keys:
        if key in data:
            return as_float(data[key])
    return 0.0


def extract_metrics(results):
    payload = {
        "precision": 0.0,
        "recall": 0.0,
        "map50": 0.0,
        "map50_95": 0.0,
    }

    results_dict = getattr(results, "results_dict", None)
    if isinstance(results_dict, dict):
        payload["precision"] = pick_metric(
            results_dict, "metrics/precision(B)", "metrics/precision"
        )
        payload["recall"] = pick_metric(
            results_dict, "metrics/recall(B)", "metrics/recall"
        )
        payload["map50"] = pick_metric(
            results_dict, "metrics/mAP50(B)", "metrics/mAP50"
        )
        payload["map50_95"] = pick_metric(
            results_dict, "metrics/mAP50-95(B)", "metrics/mAP50-95"
        )

    box = getattr(results, "box", None)
    if box is not None:
        if payload["precision"] == 0.0:
            payload["precision"] = as_float(getattr(box, "mp", 0.0))
        if payload["recall"] == 0.0:
            payload["recall"] = as_float(getattr(box, "mr", 0.0))
        if payload["map50"] == 0.0:
            payload["map50"] = as_float(getattr(box, "map50", 0.0))
        if payload["map50_95"] == 0.0:
            payload["map50_95"] = as_float(getattr(box, "map", 0.0))

    return payload


def copy_if_exists(src, dest_dir):
    src = Path(src)
    if not src.is_file():
        return ""
    dest_dir = Path(dest_dir)
    dest_dir.mkdir(parents=True, exist_ok=True)
    dest = dest_dir / src.name
    shutil.copy2(src, dest)
    return str(dest.resolve())


def parse_args():
    parser = argparse.ArgumentParser(description="AIDefenders YOLO 학습")
    parser.add_argument("--data", required=True, help="dataset.yaml 경로")
    parser.add_argument("--epochs", type=int, default=30)
    parser.add_argument("--batch", type=int, default=32)
    parser.add_argument("--imgsz", type=int, default=640)
    parser.add_argument("--workers", type=int, default=4)
    parser.add_argument("--model", default="yolov8n.pt")
    parser.add_argument("--dataset", default="")
    parser.add_argument("--result", required=True, help="Unity가 읽을 JSON 경로")
    parser.add_argument("--weights-dir", required=True, help="last.pt / best.pt 보관 폴더")
    parser.add_argument("--project", default="", help="ultralytics 출력 폴더")
    parser.add_argument("--stop-file", default="", help="Unity 중지 플래그 파일")
    return parser.parse_args()


def tensor_float(value):
    if value is None:
        return None
    try:
        if hasattr(value, "detach"):
            return float(value.detach().float().mean().cpu())
        if isinstance(value, (int, float)):
            return float(value)
        return float(value)
    except (TypeError, ValueError, RuntimeError):
        return None


def loss_items(tloss):
    values = []
    if tloss is None:
        return values
    if isinstance(tloss, dict):
        items = list(tloss.items())
        preferred = [v for k, v in items if "box" in str(k).lower()]
        source = preferred or [v for _, v in items]
        for value in source:
            number = tensor_float(value)
            if number is not None:
                values.append(number)
        return values

    number = tensor_float(tloss)
    if number is not None:
        return [number]
    if hasattr(tloss, "__len__") and not isinstance(tloss, (str, bytes)):
        for value in tloss:
            number = tensor_float(value)
            if number is not None:
                values.append(number)
    return values


def mean_loss(trainer, cached=None):
    values = loss_items(getattr(trainer, "tloss", None))
    if not values:
        values = loss_items(cached)
    if not values:
        metrics = getattr(trainer, "metrics", None)
        data = metrics_dict(metrics)
        box = pick_metric(data, "train/box_loss")
        if box > 0.0:
            values = [box]
    if not values:
        return 0.0
    return sum(values) / len(values)


def train_accuracy(trainer, cached=None):
    loss = mean_loss(trainer, cached)
    if loss <= 0.0:
        return 0.0
    return max(0.0, min(1.0, 1.0 / (1.0 + loss)))


def metrics_dict(metrics):
    if isinstance(metrics, dict):
        return metrics
    results_dict = getattr(metrics, "results_dict", None)
    if isinstance(results_dict, dict):
        return results_dict
    return {}


def val_accuracy(trainer):
    metrics = getattr(trainer, "metrics", None)
    data = metrics_dict(metrics)
    value = pick_metric(data, "metrics/mAP50(B)", "metrics/mAP50")
    if value <= 0.0:
        value = pick_metric(data, "metrics/precision(B)", "metrics/precision")
    if value <= 0.0:
        box = getattr(metrics, "box", None)
        if box is not None:
            value = as_float(getattr(box, "map50", 0.0))
    return max(0.0, min(1.0, value))


def stop_requested(stop_path):
    if not stop_path:
        return False
    path = Path(stop_path)
    if not path.is_file():
        return False
    try:
        text = path.read_text(encoding="utf-8", errors="ignore").strip().lower()
    except OSError:
        return False
    return text in ("stop", "1", "true", "kill")


class UnityTrainCallback:
    def __init__(self, payload, result_path, stop_path):
        self.payload = payload
        self.result_path = result_path
        self.stop_path = stop_path
        self.train_history = []
        self.val_history = []
        self.last_tloss = None

    def on_train_epoch_end(self, trainer):
        self.last_tloss = getattr(trainer, "tloss", None)

    def on_fit_epoch_end(self, trainer):
        epoch = int(getattr(trainer, "epoch", 0)) + 1
        total = int(getattr(trainer, "epochs", self.payload.get("epochs", 1)) or 1)
        train_acc = train_accuracy(trainer, self.last_tloss)
        val_acc = val_accuracy(trainer)
        metrics = metrics_dict(getattr(trainer, "metrics", None))

        self.train_history.append(round(train_acc, 6))
        self.val_history.append(round(val_acc, 6))
        self.payload["epoch"] = epoch
        self.payload["epochs"] = total
        self.payload["trainAcc"] = train_acc
        self.payload["valAcc"] = val_acc
        self.payload["precision"] = pick_metric(metrics, "metrics/precision(B)", "metrics/precision")
        self.payload["recall"] = pick_metric(metrics, "metrics/recall(B)", "metrics/recall")
        self.payload["map50"] = pick_metric(metrics, "metrics/mAP50(B)", "metrics/mAP50") or val_acc
        self.payload["map50_95"] = pick_metric(metrics, "metrics/mAP50-95(B)", "metrics/mAP50-95")
        self.payload["trainHistory"] = list(self.train_history)
        self.payload["valHistory"] = list(self.val_history)
        self.payload["status"] = "running"
        self.payload["message"] = "에포크 {0}/{1}".format(epoch, total)

        if stop_requested(self.stop_path):
            trainer.stop = True
            if hasattr(trainer, "stop_training"):
                trainer.stop_training = True
            self.payload["status"] = "stopping"
            self.payload["message"] = "중지 요청: 현재 에포크를 저장한 뒤 종료합니다."

        write_result(self.result_path, self.payload)
        print(
            "Epoch {0}/{1} trainAcc={2:.4f} valAcc={3:.4f}".format(
                epoch, total, train_acc, val_acc
            )
        )
        sys.stdout.flush()


def main():
    args = parse_args()
    script_dir = Path(__file__).resolve().parent
    data_path = Path(args.data).resolve()
    weights_dir = Path(args.weights_dir).resolve()
    result_path = Path(args.result).resolve()
    dataset_name = args.dataset.strip() or data_path.parent.name

    payload = {
        "status": "running",
        "message": "학습을 시작합니다.",
        "dataset": dataset_name,
        "epochs": args.epochs,
        "batch": args.batch,
        "precision": 0.0,
        "recall": 0.0,
        "map50": 0.0,
        "map50_95": 0.0,
        "lastWeights": "",
        "bestWeights": "",
        "epoch": 0,
        "trainAcc": 0.0,
        "valAcc": 0.0,
        "trainHistory": [],
        "valHistory": [],
    }
    write_result(result_path, payload)
    stop_path = args.stop_file.strip()
    if stop_path:
        stop_file = Path(stop_path)
        if stop_file.exists():
            stop_file.unlink()

    print("Python:", sys.executable)
    print("Dataset yaml:", data_path)
    print("Model:", args.model)
    print("Epochs:", args.epochs, "Batch:", args.batch)
    sys.stdout.flush()

    if not data_path.is_file():
        payload["status"] = "failed"
        payload["message"] = "dataset.yaml을 찾을 수 없습니다: " + str(data_path)
        write_result(result_path, payload)
        print(payload["message"], file=sys.stderr)
        return 1

    try:
        from ultralytics import YOLO
    except Exception as exc:
        payload["status"] = "failed"
        payload["message"] = (
            "ultralytics를 import하지 못했습니다. 메인 메뉴에서 Python 3.11 가상환경(.venv)을 설치하세요.\n"
            + str(exc)
        )
        write_result(result_path, payload)
        print(payload["message"], file=sys.stderr)
        return 1

    project = args.project.strip()
    if not project:
        project = str(script_dir / "models" / dataset_name)

    print("Project:", project)
    print("Weights dir:", weights_dir)
    sys.stdout.flush()

    try:
        model_path = args.model
        if not model_path:
            raise RuntimeError("학습 모델 경로가 비어 있습니다.")
        from_scratch = str(model_path).lower().endswith((".yaml", ".yml"))
        if from_scratch and not Path(model_path).is_file():
            raise RuntimeError("커스텀 모델 YAML을 찾을 수 없습니다: " + str(model_path))
        model = YOLO(model_path)
        if from_scratch:
            print("Custom YAML: train from scratch (no pretrained weights)")
            sys.stdout.flush()
        callback = UnityTrainCallback(payload, result_path, stop_path)
        model.add_callback("on_train_epoch_end", callback.on_train_epoch_end)
        model.add_callback("on_fit_epoch_end", callback.on_fit_epoch_end)
        results = model.train(
            data=str(data_path),
            epochs=args.epochs,
            imgsz=args.imgsz,
            batch=args.batch,
            workers=args.workers,
            project=project,
            name="train",
            exist_ok=True,
            pretrained=not from_scratch,
            amp=True,
        )

        metrics = extract_metrics(results)
        payload.update(metrics)

        save_dir = None
        trainer = getattr(model, "trainer", None)
        if trainer is not None:
            save_dir = getattr(trainer, "save_dir", None)
        if save_dir is None:
            save_dir = getattr(results, "save_dir", None)
        save_dir = Path(save_dir) if save_dir else Path(project) / "train"

        last_src = save_dir / "weights" / "last.pt"
        best_src = save_dir / "weights" / "best.pt"
        payload["lastWeights"] = copy_if_exists(last_src, weights_dir)
        payload["bestWeights"] = copy_if_exists(best_src, weights_dir)

        stopped = stop_requested(stop_path) or payload.get("status") == "stopping"
        payload["status"] = "stopped" if stopped else "completed"
        payload["message"] = "학습을 중지하고 가중치를 저장했습니다." if stopped else "학습이 완료되었습니다."
        write_result(result_path, payload)

        print("")
        print("Training complete")
        print("mAP50:", payload["map50"])
        print("mAP50-95:", payload["map50_95"])
        print("Precision:", payload["precision"])
        print("Recall:", payload["recall"])
        print("last.pt:", payload["lastWeights"])
        print("best.pt:", payload["bestWeights"])
        return 0
    except Exception as exc:
        payload["status"] = "failed"
        payload["message"] = str(exc)
        write_result(result_path, payload)
        print("Training failed:", exc, file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
