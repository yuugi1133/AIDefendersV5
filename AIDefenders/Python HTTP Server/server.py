from fastapi import FastAPI, UploadFile, File
from fastapi.responses import StreamingResponse
from PIL import Image
import argparse
import io
import os
import threading
import time

from ultralytics import YOLO
import cv2
import numpy as np


def parse_cli():
    parser = argparse.ArgumentParser(description="AIDefenders YOLO HTTP server")
    parser.add_argument(
        "--weights",
        default=os.environ.get("YOLO_WEIGHTS", "train/weights/last.pt"),
        help="YOLO weights path (.pt)",
    )
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8000)
    return parser.parse_known_args()[0]


cli = parse_cli()
print("Loading YOLO weights:", cli.weights, flush=True)
model = YOLO(cli.weights)
print("YOLO model ready", flush=True)

app = FastAPI()

latest_frame = None
annotated_frame = None

frame_lock = threading.Lock()


@app.get("/health")
def health():
    return {
        "status": "ok",
        "weights": os.path.abspath(cli.weights),
    }


@app.post("/upload")
async def upload_image(file: UploadFile = File(...)):
    global latest_frame
    global annotated_frame
    
    contents = await file.read()
    
    np_arr = np.frombuffer(contents, np.uint8)
    frame = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)

    results = model(frame)  #객체 탐지/분석 모델 실행!!
    
    #이미지 올리기
    annotated = results[0].plot()

    _, buffer = cv2.imencode(".jpg", annotated)

    annotated_frame = buffer.tobytes()

    #결과 분석/ json으로 정리
    detections = []         
    for box in results[0].boxes:

        cls_id = int(box.cls[0])
        confidence = float(box.conf[0])

        x1, y1, x2, y2 = box.xyxy[0].tolist()

        center_x = (x1 + x2) * 0.5
        center_y = (y1 + y2) * 0.5

        detections.append({
            "class_id": cls_id,
            "confidence": confidence,
            "center_x": center_x,
            "center_y": center_y,
            "width": x2 - x1,
            "height": y2 - y1
        })

    return {"detections": detections}
    
    '''  이전 내용
    annotated = results[0].plot()

    _, buffer = cv2.imencode(".jpg", annotated)

    annotated_frame = buffer.tobytes()

    return {"status": "ok"}
    '''

    #이이전 내용
    # image = Image.open(io.BytesIO(contents))

    # print("Image received!")
    # print("Size:", image.size)

    # image.save("received.png")

    # return {
        # "status": "success",
        # "width": image.width,
        # "height": image.height
    # }

@app.get("/video_feed")
def video_feed():

    return StreamingResponse(
        generate_frames(),
        media_type='multipart/x-mixed-replace; boundary=frame'
    )

#YOLO bbox까지 표시한 형태 생성
def generate_frames():

    global annotated_frame

    while True:

        if annotated_frame is None:
            continue

        frame = annotated_frame

        yield (
            b'--frame\r\n'
            b'Content-Type: image/jpeg\r\n\r\n' +
            frame +
            b'\r\n'
        )

        time.sleep(0.03)



'''     옛날 단순 비디오 프레임 생성
def generate_frames():

    global latest_frame

    while True:

        with frame_lock:

            if latest_frame is None:
                continue

            frame = latest_frame

        yield (
            b'--frame\r\n'
            b'Content-Type: image/jpeg\r\n\r\n' +
            frame +
            b'\r\n'
        )

        time.sleep(0.03)  # 약 30 FPS   
'''

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host=cli.host, port=cli.port)
