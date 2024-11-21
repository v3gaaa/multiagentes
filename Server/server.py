# server.py
# Este archivo maneja la comunicación entre Unity y el entorno/agentes en Python usando WebSockets.

import asyncio
import websockets
import json
import os
import base64
from Agents.Camera import Camera
from Agents.Drone import Drone
from Agents.Personnel import Personnel
from Agents.Environment import Environment

# Asegúrate de que la carpeta `images` exista
if not os.path.exists("images"):
    os.makedirs("images")

# Configuración inicial: Cámaras, Dron, Guardia y Entorno
cameras = [
    Camera(id=1, position=(23.5, 8, 29)),
    Camera(id=2, position=(23.5, 8, 0)),
    Camera(id=3, position=(0, 8, 1))
]

drone = Drone(start_position=(14, 8, 8), boundaries=(0, 24, 0, 24))
personnel = Personnel(control_station=(14, 0, 1))
environment = Environment(dimensions=(24, 9, 24), cameras=cameras)

# Manejo de WebSockets
async def handler(websocket, path):
    """
    Manejador principal para las conexiones WebSocket.
    """
    print(f"Connection established: {path}")
    
    try:
        async for message in websocket:
            # Procesar mensaje recibido
            data = json.loads(message)
            print(f"Received message: {data}")

            # Determinar acción basada en el tipo de mensaje
            if data["type"] == "drone_position":
                response = handle_drone_position(data)
            elif data["type"] == "camera_alert":
                response = handle_camera_alert(data)
            elif data["type"] == "control_request":
                response = handle_personnel_control(data)
            elif data["type"] == "camera_frame":
                response = await handle_camera_frame(data)
            else:
                response = {"status": "error", "message": "Unknown message type"}

            # Enviar respuesta a Unity
            await websocket.send(json.dumps(response))

    except websockets.ConnectionClosed:
        print("Connection closed")
    except Exception as e:
        print(f"Error: {e}")

def handle_drone_position(data):
    """
    Maneja actualizaciones de posición del dron.
    """
    position = data.get("position", None)
    if position:
        if drone.is_within_boundaries(position):
            drone.current_position = tuple(position)
            return {"status": "success", "message": f"Drone position updated to {drone.current_position}"}
        return {"status": "error", "message": "Position out of boundaries"}
    return {"status": "error", "message": "Invalid position data"}

def handle_camera_alert(data):
    """
    Maneja alertas enviadas por las cámaras.
    """
    camera_id = data.get("camera_id", None)
    certainty = data.get("certainty", None)
    position = data.get("position", None)

    if camera_id and certainty and position:
        print(f"Camera {camera_id} detected movement at {position} with certainty {certainty}")
        is_dangerous = drone.investigate(position, certainty)  # Dron investiga el área
        return {"status": "success", "danger": is_dangerous}
    return {"status": "error", "message": "Invalid camera alert data"}

def handle_personnel_control(data):
    """
    Maneja las solicitudes de control por parte del personal de seguridad.
    """
    certainty = data.get("certainty", None)
    if certainty is not None:
        personnel.take_control(drone, certainty)
        return {"status": "success", "message": "Personnel took control"}
    return {"status": "error", "message": "Invalid control request"}

async def handle_camera_frame(data):
    """
    Maneja los frames de las cámaras enviados desde Unity.
    """
    camera_id = data.get("camera_id", None)
    image_base64 = data.get("image", None)

    if camera_id and image_base64:
        try:
            # Decodificar la imagen base64 y guardar como archivo
            image_data = base64.b64decode(image_base64)
            file_path = os.path.join("images", f"camera_{camera_id}.jpg")
            with open(file_path, "wb") as img_file:
                img_file.write(image_data)
            
            print(f"Frame from camera {camera_id} saved at {file_path}")
            return {"status": "success", "message": f"Frame from camera {camera_id} saved"}
        except Exception as e:
            print(f"Error saving frame from camera {camera_id}: {e}")
            return {"status": "error", "message": f"Failed to save frame from camera {camera_id}"}
    return {"status": "error", "message": "Invalid camera frame data"}

async def start_server():
    """
    Inicia el servidor WebSocket.
    """
    print("Starting WebSocket server...")
    async with websockets.serve(handler, "localhost", 8765):
        await asyncio.Future()  # Mantiene el servidor corriendo

if __name__ == "__main__":
    asyncio.run(start_server())
