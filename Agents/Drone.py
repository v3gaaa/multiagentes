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

        # Detectar específicamente scavengers
        anomalies = detect_anomalies("drone_image.jpg")
        scavenger_detections = [
            {
                "class": anomaly["class"],
                "confidence": anomaly["confidence"],
                "position": anomaly["position"]
            }
            for anomaly in anomalies
            if anomaly["class"] == "scavenger"
        ]

        if scavenger_detections:
            alert_message = {
                "type": "drone_alert",
                "anomalies": scavenger_detections,
                "position": self.position,
            }
            websocket.send(json.dumps(alert_message))
            print("Alert sent to personnel: Scavenger detected!")
        else:
            print("No scavenger found. Resuming patrol.")

        self.investigating = False