import asyncio
import websockets
import json
import base64
import os
import sys

yolo_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "../YOLO")
sys.path.insert(0, yolo_path)

# Import detect_anomalies from YOLO/main.py
from main import detect_anomalies

IMAGE_FOLDER = "images"

# Añadir el directorio raíz del proyecto al Python Path
current_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.abspath(os.path.join(current_dir, ".."))
sys.path.append(project_root)

from Agents.Camera import Camera
from Agents.Drone import Drone
from Agents.Personnel import Personnel
from Agents.Environment import Environment


# Inicialización de los agentes y entorno
cameras = [
    Camera(camera_id=1, position={"x": 23.5, "y": 8, "z": 29}),
    Camera(camera_id=2, position={"x": 23.5, "y": 8, "z": 0}),
    Camera(camera_id=3, position={"x": 0, "y": 8, "z": 1}),
]

drone = Drone(
    position={"x": 16, "y": 0, "z": 1},
    patrol_route=[
        {"x": 6, "y": 8, "z": 4},      # Start at first aisle
        {"x": 6, "y": 8, "z": 26},     # Move down first aisle
        {"x": 10, "y": 8, "z": 26},    # Shift right to second aisle
        {"x": 10, "y": 8, "z": 4},     # Move up second aisle
        {"x": 14, "y": 8, "z": 4},     # Shift right to third aisle
        {"x": 14, "y": 8, "z": 26},    # Move down third aisle
        {"x": 18, "y": 8, "z": 26},    # Shift right to fourth aisle
        {"x": 18, "y": 8, "z": 4},     # Move up fourth aisle
        {"x": 6, "y": 8, "z": 4},      # Return to start
    ]
)

personnel = Personnel(control_station={"x": 14, "y": 0, "z": 1})

environment = Environment(
    boundaries=(0, 24, 0, 9, 0, 30),
    cameras=cameras,
    drone=drone,
    personnel=personnel,
)

def handle_camera_frame(websocket, data):
    """
    Handles camera frame messages by detecting anomalies and alerting the drone if necessary.
    :param websocket: The WebSocket connection.
    :param data: Incoming message data from the camera.
    """
    camera_id = data["camera_id"]
    image_data = data["image"]
    image_path = save_image(image_data, f"camera_{camera_id}.jpg")

    anomalies = detect_anomalies(image_path)
    if anomalies:
        print(f"Anomalies detected by camera {camera_id}: {anomalies}")
        alert_message = {
            "type": "camera_alert",
            "camera_id": camera_id,
            "position": anomalies[0]["position"],  # Assumes anomalies have a 'position' field
            "anomalies": anomalies,
        }
        websocket.send(json.dumps(alert_message))
    else:
        print(f"No anomalies detected by camera {camera_id}.")

def handle_drone_camera_frame(websocket, data):
    image_data = data["image"]
    image_path = save_image(image_data, "drone_camera.jpg")

    anomalies = detect_anomalies(image_path)
    if anomalies:
        print(f"Anomalies detected by the drone: {anomalies}")
        alert_message = {
            "type": "drone_alert",
            "position": anomalies[0]["position"],
            "anomalies": anomalies,
        }
        websocket.send(json.dumps(alert_message))
    else:
        print("No anomalies detected by the drone.")

# Manejo de conexiones y mensajes
async def handler(websocket):
    async for message in websocket:
        data = json.loads(message)
        message_type = data.get("type")

        if message_type == "camera_frame":
            handle_camera_frame(websocket, data)
        elif message_type == "drone_camera_frame":
            handle_drone_camera_frame(websocket, data)
        elif message_type == "manual_control":
            if data["action"] == "take_control":
                personnel.take_control_of_drone(drone, websocket)
            elif data["action"] == "release_control":
                personnel.release_control_of_drone(drone, websocket)


async def start_server():
    """
    Inicia el servidor WebSocket.
    """
    print("Starting WebSocket server...")
    async with websockets.serve(handler, "localhost", 8765):
        print("Server started")
        await asyncio.Future()  # Mantiene el servidor corriendo

def save_image(base64_image, filename):
    if not os.path.exists(IMAGE_FOLDER):
        os.makedirs(IMAGE_FOLDER)
    image_path = os.path.join(IMAGE_FOLDER, filename)
    with open(image_path, "wb") as img_file:
        img_file.write(base64.b64decode(base64_image))
    return image_path


if __name__ == "__main__":
    asyncio.run(start_server())



