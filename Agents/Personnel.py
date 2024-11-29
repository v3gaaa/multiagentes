import json
import asyncio
import base64

from successMetrics.metrics_tracker import SuccessMetricsTracker

class Personnel:
    def __init__(self, control_station):
        self.control_station = control_station
        self.drone_control = False
        self.metrics_tracker = SuccessMetricsTracker(name="Personnel")

    async def handle_guard_control(self, websocket, drone):
        """
        Handle guard control of the drone, keeping the guard at the control station
        and initiating a drone investigation lap
        """
        print("[Personnel] Guard taking control of the drone for investigation")
        
        self.drone_control = True
        
        # Define an investigation route that covers the warehouse area
        investigation_route = [
            {"x": 4, "y": 8, "z": 3},
            {"x": 14, "y": 8, "z": 15},
            {"x": 21, "y": 8, "z": 27},
            {"x": 8, "y": 8, "z": 20},
            {"x": 16, "y": 8, "z": 10}
        ]
        
        for waypoint in investigation_route:
            investigation_command = {
                "type": "drone_investigation_command",
                "target_position": waypoint
            }
            await websocket.send(json.dumps(investigation_command))
        
            await asyncio.sleep(0.5)  # Reduce the interval to 0.5 seconds
        
        # self.release_control_of_drone()

    async def release_control_of_drone(self):
        self.drone_control = False
        print("[Personnel] Personnel has released control of the drone.")

    def handle_alert(self, websocket, alert_data):
        print(f"Handling alert: {alert_data}")
        threat_detected = self.assess_threat(alert_data["anomalies"])

        if threat_detected:
            print("ALERT! Scavenger detected. Sending alarm signal.")
            alert_message = {
                "type": "alarm",
                "status": "ALERT",
                "position": alert_data.get("position", None)
            }
            self.metrics_tracker.record_detection(is_true_positive=True)
            websocket.send(json.dumps(alert_message))
        else:
            print("False alarm. No scavenger detected.")
            alert_message = {"type": "alarm", "status": "FALSE_ALARM"}
            self.metrics_tracker.record_detection(is_true_positive=False)
            websocket.send(json.dumps(alert_message))

    def assess_threat(self, anomalies):
        for anomaly in anomalies:
            # Check if the detected object is a scavenger with high confidence
            if anomaly["class"] == "thief" and anomaly["confidence"] > 0.8:
                return True
        return False
    
    async def handle_guard_control_detection(self, websocket, detection):
        print(f"[Personnel] Handling detection during guard control: {detection}")
        if detection and detection.get("confidence", 0) > 0.8:
            print("ALERT! Scavenger detected during guard control. Activating alarm.")
            alert_message = {
                "type": "alarm",
                "status": "GUARD_CONTROL_ALERT",
                "position": detection.get("world_position", None),
                "confidence": detection.get("confidence", 0)
            }
            # Aquí puedes enviar la alerta al cliente (Unity) o procesarla como desees
            self.metrics_tracker.record_investigation(was_successful=True)
            await websocket.send(json.dumps(alert_message))
        else:
            print("No valid detection during guard control.")
            self.metrics_tracker.record_investigation(was_successful=False)

    async def periodic_metrics_report(self, websocket):
       """Generate and send metrics report periodically"""
       report_path = self.metrics_tracker.generate_metrics_report()
      
       with open(report_path, 'rb') as report_file:
           report_data = base64.b64encode(report_file.read()).decode('utf-8')
      
       await websocket.send(json.dumps({
           "type": "personnel_metrics_report",
           "report_data": report_data,
           "metrics": self.metrics_tracker.get_metrics()
       }))

