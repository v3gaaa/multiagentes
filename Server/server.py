import asyncio
import websockets
import json
import os
import sys

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
    Camera(camera_id=3, position={"x": 0, "y": 8, "z": 1})
]

# Límites del almacén: (x_min, x_max, y_min, y_max, z_min, z_max)
boundaries = (0, 24, 0, 9, 0, 30)

# Inicialización del dron con los límites del almacén
drone = Drone(
    position={"x": 16, "y": 0, "z": 1},
    patrol_route=[
        {"x": 14, "y": 8, "z": 8},
        {"x": 20, "y": 8, "z": 20},
        {"x": 8, "y": 8, "z": 8}
    ],
    boundaries=boundaries
)

personnel = Personnel(control_station={"x": 14, "y": 0, "z": 1})

environment = Environment(
    boundaries=boundaries,
    cameras=cameras,
    drone=drone,
    personnel=personnel,
)

# Manejo de conexiones y mensajes
async def handler(websocket):
    print(f"[Server] Connection established with {websocket.remote_address}")
    try:
        async for message in websocket:
            data = json.loads(message)
            message_type = data.get("type")
            print(f"[Server] Received message")

            if message_type == "camera_frame":
                camera_id = data["camera_id"]
                for camera in cameras:
                    if camera.camera_id == camera_id:
                        await camera.process_image(data["image"], websocket)

            elif message_type == "drone_camera_frame":
                await drone.investigate_area(websocket, data["image"])

            elif message_type == "drone_investigation_command":
                target_position = data.get("target_position")
                await drone.navigate_and_investigate(websocket, target_position)

            elif message_type == "guard_at_control_station":
                personnel.take_control_of_drone(websocket)

            


    except Exception as e:
        print(f"[Server] Error: {e}")


async def start_server():
    """
    Inicia el servidor WebSocket.
    """
    print("Starting WebSocket server...")
    async with websockets.serve(handler, "localhost", 8765):
        print("Server started")
        await asyncio.Future()  # Mantiene el servidor corriendo

if __name__ == "__main__":
    asyncio.run(start_server())
