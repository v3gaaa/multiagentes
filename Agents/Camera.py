import base64
import json
from YOLO.main import detect_anomalies


class Camera:
    def __init__(self, camera_id, position):
        self.camera_id = camera_id
        self.position = position

    def process_image(self, image_data):
        """
        Procesa una imagen recibida desde Unity.
        :param image_data: Imagen en formato base64.
        :return: Lista de anomalías detectadas.
        """
        image_path = f"images/camera_{self.camera_id}.jpg"
        with open(image_path, "wb") as img_file:
            img_file.write(base64.b64decode(image_data))

        # Detectar anomalías con YOLO
        anomalies = detect_anomalies(f"camera_{self.camera_id}.jpg")
        
        # Filtrar específicamente para detección de scavenger
        scavenger_detections = [
            {
                "class": anomaly["class"],
                "confidence": anomaly["confidence"],
                "position": anomaly["position"]
            }
            for anomaly in anomalies
            if anomaly["class"] == "scavenger"
        ]
        
        return scavenger_detections

    def alert_drone(self, websocket, anomalies):
        """
        Envía una alerta al dron si se detecta un scavenger.
        :param websocket: WebSocket de comunicación.
        :param anomalies: Lista de anomalías detectadas.
        """
        if anomalies:
            # Take the position of the first detected scavenger
            scavenger_position = anomalies[0]["position"]
            alert_message = {
                "type": "camera_alert",
                "camera_id": self.camera_id,
                "anomalies": anomalies,
                "position": scavenger_position
            }
            websocket.send(json.dumps(alert_message))
            print(f"Scavenger alert sent from camera {self.camera_id}")