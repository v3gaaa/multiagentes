import base64
import json
import time
from YOLO.main import detect_anomalies, alert_drone
from utils.PositionUtils import PositionUtils

class Camera:
    def __init__(self, camera_id, position):
        self.camera_id = camera_id
        self.position = position
        self.last_detection_time = None
        self.detection_history = []
        self.confidence_threshold = 0.7
        self.valid_classes = ['thiefs']  # Your trained class
        
    async def process_image(self, image_data, websocket):
        """Process incoming camera frames and detect anomalies"""
        image_path = f"images/camera_{self.camera_id}.jpg"
        with open(image_path, "wb") as img_file:
            img_file.write(base64.b64decode(image_data))

        print(f"Camera {self.camera_id} processing image")

        # Detect anomalies
        detections = detect_anomalies(f"camera_{self.camera_id}.jpg")
        print(f"Raw detections from camera {self.camera_id}: {detections}")
        
        # Filter detections based on confidence
        high_confidence_detections = []
        for detection in detections:
            if (detection['class'] == 'thiefs' and 
                detection['confidence'] > self.confidence_threshold):
                
                # Convert detection coordinates to Unity world space
                world_pos = PositionUtils.yolo_to_unity_position(detection, self.position)
                
                detection_data = {
                    "x": detection["x"],
                    "y": detection["y"],
                    "width": detection.get("width", 0),
                    "height": detection.get("height", 0),
                    "confidence": detection["confidence"],
                    "className": "thiefs",
                    "world_position": world_pos
                }
                high_confidence_detections.append(detection_data)
                
                print(f"Camera {self.camera_id} detected high-confidence scavenger: {detection_data}")

        if high_confidence_detections:
            # Take the highest confidence detection
            primary_detection = max(high_confidence_detections, key=lambda x: x['confidence'])
            
            # Create alert with world position
            alert_data = {
                "type": "camera_alert",
                "camera_id": self.camera_id,
                "detection": primary_detection,
                "position": primary_detection["world_position"],
                "timestamp": time.time()
            }
            
            print(f"Camera {self.camera_id} sending alert: {alert_data}")
            await websocket.send(json.dumps(alert_data))
            await alert_drone(websocket, [primary_detection], self.position)

        return high_confidence_detections