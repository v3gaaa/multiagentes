import base64
import json
import time
from YOLO.main import detect_anomalies, alert_drone


class Camera:
    def __init__(self, camera_id, position):
        self.camera_id = camera_id
        self.position = position
        self.success_metrics = {
            "total_detections": 0,
            "true_detections": 0,
            "false_alarms": 0,
            "detection_rate": 0.0,
            "last_activity": None   
        }

    async def process_image(self, image_data, websocket):
        image_path = f"images/camera_{self.camera_id}.jpg"
        with open(image_path, "wb") as img_file:
            img_file.write(base64.b64decode(image_data))

        print(f"Camera {self.camera_id} processing image")

        # Detectar anomalías con YOLO
        anomalies = detect_anomalies(f"camera_{self.camera_id}.jpg")
        print(f"Anomalies detected by camera {self.camera_id}: {anomalies}")
        
        # Filtrar específicamente para detección de scavenger
        scavenger_detections = [
            {
                "x": anomaly["x"],
                "y": anomaly["y"],
                "width": anomaly.get("width", 0),
                "height": anomaly.get("height", 0),
                "confidence": anomaly["confidence"],
                "className": anomaly["class"]
            }
            for anomaly in anomalies
            if anomaly["class"] == "thiefs"
        ]
        if (scavenger_detections != []):
            print("Entered if camera")
            self.record_detection(True)
            print(f"Scavenger detections in camera, sending to drone: {scavenger_detections}")
            await websocket.send(json.dumps({"type": "camera_alert", "detections": scavenger_detections}))
            alert_drone(websocket, scavenger_detections, self.position)
        
        return scavenger_detections

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