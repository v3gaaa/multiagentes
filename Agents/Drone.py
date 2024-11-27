import json
import base64
from YOLO.main import detect_anomalies
import time

class Drone:
    def __init__(self, position, patrol_route, boundaries):
        self.position = position
        self.patrol_route = patrol_route
        self.current_target = None
        self.investigating = False
        self.confidence_threshold = 0.7
        self.boundaries = boundaries  # (x_min, x_max, y_min, y_max, z_min, z_max)

    def is_within_boundaries(self, position):
        """Validate if a position is within warehouse boundaries."""
        x_min, x_max, y_min, y_max, z_min, z_max = self.boundaries
        return (
            x_min <= position["x"] <= x_max
            and y_min <= position["y"] <= y_max
            and z_min <= position["z"] <= z_max
        )

    async def navigate_and_investigate(self, websocket, target_position):
        """Navigate to target position and begin investigation."""
        try:
            if not self.is_within_boundaries(target_position):
                print(f"[Drone] Target position {target_position} is outside warehouse boundaries.")
                # Ajustar a la posición más cercana dentro de los límites
                target_position["x"] = max(min(target_position["x"], self.boundaries[1]), self.boundaries[0])
                target_position["y"] = max(min(target_position["y"], self.boundaries[3]), self.boundaries[2])
                target_position["z"] = max(min(target_position["z"], self.boundaries[5]), self.boundaries[4])

            print(f"[Drone] Navigating to adjusted position: {target_position}")
            self.position = target_position
            self.current_target = target_position
            self.investigating = True

            # Notify Unity to pause patrol and move to investigation
            command = {
                "type": "drone_investigation_command",
                "target_position": target_position
            }
            await websocket.send(json.dumps(command))
        except Exception as e:
            print(f"[Drone] Error during navigation: {e}")


    async def investigate_area(self, websocket, image_data):
        """Process drone camera feed and detect threats."""
        print("[Drone] Analyzing Drone Camera...")

        # Save and process image
        image_path = "images/drone_image.jpg"
        with open(image_path, "wb") as img_file:
            img_file.write(base64.b64decode(image_data))

        # Detect anomalies
        detections = detect_anomalies("drone_image.jpg")
        print(f"[Drone] Raw detections: {detections}")

        high_confidence_detections = [
            {
                "x": detection["x"],
                "y": detection["y"],
                "width": detection.get("width", 0),
                "height": detection.get("height", 0),
                "confidence": detection["confidence"],
                "className": detection["class"],
                "world_position": {
                    "x": self.position["x"] + (detection["x"] - 0.5) * 30,
                    "y": self.position["y"],
                    "z": self.position["z"] + (detection["y"] - 0.5) * 30
                }
            }
            for detection in detections
            if detection["class"] == "thiefs" and detection["confidence"] >= self.confidence_threshold
        ]

        if high_confidence_detections:
            primary_detection = max(high_confidence_detections, key=lambda x: x["confidence"])
            alert_message = {
                "type": "drone_alert",
                "detection": primary_detection,
                "position": primary_detection["world_position"],
                "timestamp": time.time()
            }
            print(f"[Drone] High-confidence detection found. Sending alert: {alert_message}")
            await websocket.send(json.dumps(alert_message))
        else:
            print("[Drone] No high-confidence detections found.")

        self.investigating = False
