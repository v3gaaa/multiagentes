import base64
import json
import time
from YOLO.main import detect_anomalies
from utils.PositionUtils import PositionUtils  # Asegúrate de importar el módulo correcto

class Camera:
    def __init__(self, camera_id, position, confidence_threshold=0.7):
        self.camera_id = camera_id
        self.position = position
        self.confidence_threshold = confidence_threshold
        self.valid_classes = ["thiefs"]

    async def process_image(self, image_data, websocket):
        """Process incoming camera frames and detect anomalies."""
        image_path = f"images/camera_{self.camera_id}.jpg"
        with open(image_path, "wb") as img_file:
            img_file.write(base64.b64decode(image_data))

        print(f"[Camera] Camera {self.camera_id} processing image")

        # Detect anomalies
        detections = detect_anomalies(f"camera_{self.camera_id}.jpg")
        high_confidence_detections = []

        for detection in detections:
            if detection["class"] == "thiefs" and detection["confidence"] >= self.confidence_threshold:
                # Calcular posición mundial
                detection["world_position"] = PositionUtils.yolo_to_unity_position(detection, self.position, boundaries=(0, 24, 0, 9, 0, 30))
                high_confidence_detections.append(detection)

        if high_confidence_detections:
            detection = high_confidence_detections[0]  # Take the highest-confidence detection
            alert_data = {
                "type": "camera_alert",
                "camera_id": self.camera_id,
                "detection": detection,
                "position": detection.get("world_position"),  # Verificar que esté calculado
                "timestamp": time.time()
            }

            if alert_data["position"] is not None:  # Evitar enviar datos sin `world_position`
                print(f"[Camera] Camera {self.camera_id} sending alert: {alert_data}")
                await websocket.send(json.dumps(alert_data))
            else:
                print(f"[Camera] Failed to calculate world_position for detection: {detection}")

        return high_confidence_detections
