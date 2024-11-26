from ultralytics import YOLO
import os
import base64
import json
import cv2
import numpy as np
from pathlib import Path

# Initialize YOLO model
print("Loading YOLO model...")
MODEL_PATH = "models/best.pt"  # Path to your trained model
if not os.path.exists(MODEL_PATH):
    # If no custom model exists, use a pretrained one
    model = YOLO('yolov8n.pt')
else:
    model = YOLO(MODEL_PATH)

IMAGE_FOLDER = "images"
if not os.path.exists(IMAGE_FOLDER):
    os.makedirs(IMAGE_FOLDER)

def detect_anomalies(image_name):
    """
    Detect anomalies using YOLO model.
    :param image_name: Name of the image file in the 'images' folder.
    :return: Detected objects or an empty list if none.
    """
    image_path = os.path.join(IMAGE_FOLDER, image_name)
    try:
        print(f"Performing inference on {image_name}...")
        
        # Run inference
        results = model(image_path)
        
        # Process results
        predictions = []
        for r in results[0]:  # Get first image results
            # Get box coordinates
            box = r.boxes[0]
            x1, y1, x2, y2 = box.xyxy[0].tolist()
            
            # Calculate center point and normalized coordinates
            img = cv2.imread(image_path)
            height, width = img.shape[:2]
            
            center_x = (x1 + x2) / 2 / width  # normalize to 0-1
            center_y = (y1 + y2) / 2 / height  # normalize to 0-1
            box_width = (x2 - x1) / width
            box_height = (y2 - y1) / height
            
            # Get confidence and class
            conf = float(box.conf[0])
            cls = int(box.cls[0])
            
            prediction = {
                "x": float(center_x),
                "y": float(center_y),
                "width": float(box_width),
                "height": float(box_height),
                "confidence": conf,
                "class": "thiefs",  # Your target class
                "position": {"x": center_x, "y": center_y, "z": 0}
            }
            predictions.append(prediction)
        
        print(f"Detected objects in {image_name}: {predictions}")
        return predictions
        
    except Exception as e:
        print(f"Error during inference on {image_name}: {e}")
        import traceback
        traceback.print_exc()
        return []

def save_image(base64_image, filename):
    """Save base64 image to file"""
    if not os.path.exists(IMAGE_FOLDER):
        os.makedirs(IMAGE_FOLDER)
    image_path = os.path.join(IMAGE_FOLDER, filename)
    try:
        with open(image_path, "wb") as img_file:
            img_file.write(base64.b64decode(base64_image))
        return image_path
    except Exception as e:
        print(f"Error saving image {filename}: {e}")
        return None

async def alert_drone(websocket, anomalies, camera_position):
    """Send alert to drone with detected anomalies"""
    # alert_drone(websocket, scavenger_detections, self.position)
    if anomalies:
        world_position = convert_yolo_to_unity_position(anomalies[0], camera_position)
        alert_message = {
            "type": "camera_alert",
            "anomalies": anomalies,
            "target_position": world_position
        }
        await websocket.send(json.dumps(alert_message))
        print(f"Alert sent to drone. Position: {world_position}")
    else:
        print("No anomalies to alert about")

def alert_personnel(websocket, anomalies):
    """Send alert to personnel with detected anomalies"""
    if anomalies:
        alert_message = {
            "type": "drone_alert",
            "anomalies": anomalies,
            "position": anomalies[0].get("position")
        }
        websocket.send(json.dumps(alert_message))
        print("Alert sent to personnel:", alert_message)
    else:
        print("No anomalies to alert personnel about")

def convert_yolo_to_unity_position(prediction, camera_position):
    """Convert YOLO detection coordinates to Unity world position"""
    x = prediction['x']  # Center x of detection
    y = prediction['y']  # Center y of detection
    
    # Convert to world space (adjust multipliers based on your Unity scene scale)
    world_x = camera_position['x'] + (x - 0.5) * 30
    world_z = camera_position['z'] + (y - 0.5) * 30
    world_y = 0  # Ground level
    
    return {
        "x": world_x,
        "y": world_y,
        "z": world_z
    }

def handle_camera_frame(websocket, data):
    """Handle incoming camera frame"""
    camera_id = data["camera_id"]
    image_path = save_image(data["image"], f"camera_{camera_id}.jpg")
    if image_path:
        anomalies = detect_anomalies(f"camera_{camera_id}.jpg")
        if anomalies:
            alert_drone(websocket, anomalies, data["camera_position"])

def handle_drone_camera_frame(websocket, data):
    """Handle incoming drone camera frame"""
    image_path = save_image(data["image"], "drone_camera.jpg")
    if image_path:
        anomalies = detect_anomalies("drone_camera.jpg")
        if anomalies:
            alert_personnel(websocket, anomalies)