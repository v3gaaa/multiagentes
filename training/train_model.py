from ultralytics import YOLO
import os

# Make sure we're in the correct directory
current_dir = os.path.dirname(os.path.abspath(__file__))
data_yaml_path = os.path.join(current_dir, "dataset", "data.yaml")

# Train YOLOv8 model
model = YOLO('yolov8n.pt')  # load pretrained model
model.train(
    data=data_yaml_path,
    epochs=100,
    imgsz=640,
    batch=16,
    name='thief_detector'
)

# The trained model will be saved in 'runs/detect/thief_detector/weights/best.pt'
print("Training completed! Model saved in runs/detect/thief_detector/weights/best.pt")