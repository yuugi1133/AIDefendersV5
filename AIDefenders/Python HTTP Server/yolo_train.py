import argparse
import json
import shutil
import sys
from pathlib import Path


def write_result(path, payload):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False), encoding="utf-8")


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
    return parser.parse_args()


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
    }
    write_result(result_path, payload)

    print("Python:", sys.executable)
    print("Dataset yaml:", data_path)
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
        model = YOLO(args.model)
        results = model.train(
            data=str(data_path),
            epochs=args.epochs,
            imgsz=args.imgsz,
            batch=args.batch,
            workers=args.workers,
            project=project,
            name="train",
            exist_ok=True,
            pretrained=True,
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

        payload["status"] = "completed"
        payload["message"] = "학습이 완료되었습니다."
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
