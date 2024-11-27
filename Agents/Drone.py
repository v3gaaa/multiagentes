import json
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
            "successful_detections": 0,
            "patrols_completed": 0,
            "investigation_success_rate": 0.0
        }

    async def navigate_and_investigate(self, websocket, target_position):
        try:
            # Update drone's position to the target
            print(f"Drone navigating to position: {target_position}")
            self.position = target_position
            self.current_target = target_position

            print("Waiting for drone camera frame...")
        
        except Exception as e:
            print(f"Error during drone navigation and investigation: {e}")

    async def investigate_area(self, websocket, image_data):
        print("Investigating area...")
        self.investigating = True

        # Guardar y procesar imagen
        image_path = "images/drone_image.jpg"
        with open(image_path, "wb") as img_file:
            img_file.write(base64.b64decode(image_data))

        # Detectar específicamente scavengers
        anomalies = detect_anomalies("drone_image.jpg")
        print(f"Anomalies detected by drone: {anomalies}")
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
        print(f"Scavenger detections: {scavenger_detections}")

        if (scavenger_detections != []):
            print("Entered if")
            print(self.position)
            alert_message = {
                "type": "drone_alert",
                "detections": scavenger_detections,
                "position": self.position
            }
        
            print(f"Alert message sent to personnel: {alert_message}")
            await websocket.send(json.dumps({"type": "drone_control", "detections": scavenger_detections}))
            self.record_investigation(bool(scavenger_detections))
            print("Alert sent to personnel: Scavenger detected!")
        else:
            print("No scavenger found.")

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

    def get_success_rate(self):
        """Get the drone's success rate"""
        return self.success_metrics['investigation_success_rate']