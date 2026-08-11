from ultralytics import YOLO

model = YOLO("yolov8n.pt")

results = model(
    "Enemy_17.jpg",
    show=True
)

print(results)

results = model(
    "Friendly_1.jpg",
    show=True
)

print(results)