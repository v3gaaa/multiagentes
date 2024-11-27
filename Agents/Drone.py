import json
import base64
from YOLO.main import detect_anomalies
import time

class Drone:
    def __init__(self, position, patrol_route):
        self.position = position
        self.patrol_route = patrol_route
        self.current_target = None
        self.investigating = False
        self.confidence_threshold = 0.7

    async def navigate_and_investigate(self, websocket, target_position):
        """Navigate to target position and begin investigation"""
        try:
            print(f"Drone navigating to position: {target_position}")
            self.position = target_position
            self.current_target = target_position
            self.investigating = True
        except Exception as e:
            print(f"Error during drone navigation: {e}")

    async def investigate_area(self, websocket, image_data):
        """Process drone camera feed and detect threats"""
        print("Investigating area...")
        
        # Save and process image
        image_path = "images/drone_image.jpg"
        with open(image_path, "wb") as img_file:
            img_file.write(base64.b64decode(image_data))

        # Detect specifically scavengers
        detections = detect_anomalies("drone_image.jpg")
        print(f"Raw detections from drone: {detections}")
        
        # Filter high confidence detections
        high_confidence_detections = []
        for detection in detections:
            if detection['class'] == 'thiefs' and detection['confidence'] > self.confidence_threshold:
                # Convert coordinates
                world_pos = {
                    'x': self.position['x'] + (detection['x'] - 0.5) * 30,
                    'y': self.position['y'],
                    'z': self.position['z'] + (detection['y'] - 0.5) * 30
                }
                
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

        if high_confidence_detections:
            # Take highest confidence detection
            primary_detection = max(high_confidence_detections, key=lambda x: x['confidence'])
            
            alert_message = {
                "type": "drone_alert",
                "detection": primary_detection,
                "position": primary_detection["world_position"],
                "timestamp": time.time()
            }
            
            print(f"Drone sending alert: {alert_message}")
            await websocket.send(json.dumps(alert_message))
            print("Alert sent to personnel: High-confidence scavenger detected!")
        else:
            print("No high-confidence scavenger found.")
            self.investigating = False