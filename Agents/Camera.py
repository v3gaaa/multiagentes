import base64
import json
import time
from YOLO.main import detect_anomalies


class Camera:
    def __init__(self, camera_id, position):
        self.camera_id = camera_id
        self.position = position
        self.succes_metrics = {
            "total_detections": 0,
            "false_alarms": 0,
            "detection_rate": 0.0,
            "last_activity": None   
        }

    def process_image(self, image_data):
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

    def record_detection(self, was_true_detection):
        """Record the success/failure of a detection"""
        if was_true_detection:
            self.success_metrics['true_detections'] += 1
        else:
            self.success_metrics['false_alarms'] += 1
        
        total_detections = (self.success_metrics['true_detections'] + 
                          self.success_metrics['false_alarms'])
        if total_detections > 0:
            self.success_metrics['detection_rate'] = (
                self.success_metrics['true_detections'] / total_detections
            )
        self.success_metrics['last_activity'] = time.time()

    def get_success_rate(self):
        """Get the camera's success rate"""
        return self.success_metrics['detection_rate']