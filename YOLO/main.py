from inference_sdk import InferenceHTTPClient
import os
import base64
import json

# Initialize YOLO client
CLIENT = InferenceHTTPClient(
    api_url="https://detect.roboflow.com",
    api_key="5Pdz8tW7hi78Qf6oXAQt"
)

MODEL_ID = "thieforsusdetection/1"
IMAGE_FOLDER = "images"

def detect_anomalies(image_name):
    """
    Detect anomalies using YOLO model.
    :param image_name: Name of the image file in the 'images' folder.
    :return: Detected objects or an empty list if none.
    """
    image_path = os.path.join(IMAGE_FOLDER, image_name)
    try:
        print(f"Performing inference on {image_name}...")
        result = CLIENT.infer(image_path, model_id=MODEL_ID)
        predictions = result.get("predictions", [])
        print(f"Detected objects in {image_name}: {predictions}")
        return predictions
    except Exception as e:
        print(f"Error during inference on {image_name}: {e}")
        return []
    
def save_image(base64_image, filename):
    if not os.path.exists(IMAGE_FOLDER):
        os.makedirs(IMAGE_FOLDER)
    image_path = os.path.join(IMAGE_FOLDER, filename)
    with open(image_path, "wb") as img_file:
        img_file.write(base64.b64decode(base64_image))
    return image_path

def alert_drone(websocket, anomalies):
    if anomalies:
        alert_message = {
            "type": "camera_alert",
            "anomalies": anomalies,
            "position": anomalies[0].get("position"),  # Assumes the anomaly includes a position
        }
        websocket.send(json.dumps(alert_message))
        print("Alert sent to the drone:", alert_message)
    else:
        print("No anomalies to alert the drone about.")

def alert_personnel(websocket, anomalies):
    if anomalies:
        alert_message = {
            "type": "drone_alert",
            "anomalies": anomalies,
            "position": anomalies[0].get("position"),  # Assumes the anomaly includes a position
        }
        websocket.send(json.dumps(alert_message))
        print("Alert sent to personnel:", alert_message)
    else:
        print("No anomalies to alert personnel about.")
    
def handle_camera_frame(websocket, data):
    camera_id = data["camera_id"]
    image_path = save_image(data["image"], f"camera_{camera_id}.jpg")
    anomalies = detect_anomalies(image_path)
    if anomalies:
        alert_drone(websocket, anomalies)

def handle_drone_camera_frame(websocket, data):
    image_path = save_image(data["image"], "drone_camera.jpg")
    anomalies = detect_anomalies(image_path)
    if anomalies:
        alert_personnel(websocket, anomalies)

