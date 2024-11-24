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
        self.success_metrics = {
            "areas_investigated": 0,
            "succesful_detections": 0,
            "patrols_completed": 0,
            "investigation_success_rate": 0.0
        }

    def investigate_area(self, websocket, image_data):
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
            self.record_investigation(bool(scavenger_detections))
            print("Alert sent to personnel: Scavenger detected!")
        else:
            print("No scavenger found. Resuming patrol.")

        self.investigating = False

    def record_investigation(self, was_successful):
        """Record the success/failure of an investigation"""
        self.success_metrics['areas_investigated'] += 1
        if was_successful:
            self.success_metrics['successful_detections'] += 1
        
        if self.success_metrics['areas_investigated'] > 0:
            self.success_metrics['investigation_success_rate'] = (
                self.success_metrics['successful_detections'] / 
                self.success_metrics['areas_investigated']
            )
    
    def record_patrol_completion(self):
        """Record completion of a patrol route"""
        self.success_metrics['patrols_completed'] += 1

    def get_success_rate(self):
        """Get the drone's success rate"""
        return self.success_metrics['investigation_success_rate']