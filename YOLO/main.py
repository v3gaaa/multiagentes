from inference_sdk import InferenceHTTPClient
import os

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
