import json
import time
import base64
from YOLO.main import detect_anomalies


class Drone:
    def __init__(self, position, patrol_route):
        self.position = position
        self.patrol_route = patrol_route
        self.current_target = None
        self.investigating = False

    def move_to(self, target_position):
        """
        Mueve el dron hacia una posición objetivo.
        :param target_position: Coordenadas (x, y, z) del objetivo.
        """
        print(f"Moving drone to position {target_position}")
        self.position = target_position

    def investigate_area(self, websocket, image_data):
        """
        Inspecciona un área y envía resultados al servidor.
        :param websocket: WebSocket de comunicación.
        :param image_data: Imagen en formato base64.
        """
        print("Investigating area...")
        self.investigating = True

        # Guardar y procesar imagen
        image_path = "images/drone_image.jpg"
        with open(image_path, "wb") as img_file:
            img_file.write(base64.b64decode(image_data))

        anomalies = detect_anomalies("drone_image.jpg")

        if anomalies:
            alert_message = {
                "type": "drone_alert",
                "anomalies": anomalies,
                "position": self.position,
            }
            websocket.send(json.dumps(alert_message))
            print("Alert sent to personnel: Anomalies detected!")
        else:
            print("No anomalies found. Resuming patrol.")

        self.investigating = False

    def resume_patrol(self):
        """
        Reanuda la patrulla del dron.
        """
        self.current_target = None
        print("Drone resuming patrol...")
