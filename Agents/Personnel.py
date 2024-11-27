import json

class Personnel:
    def __init__(self, control_station):
        self.control_station = control_station
        self.drone_control = False
        self.success_metrics = {
            'alerts_handled': 0,
            'correct_assessments': 0,
            'assessment_accuracy': 0.0,
            'manual_control_instances': 0
        }

    async def take_control_of_drone(self, websocket):
        self.drone_control = True
        control_message = {
            "type": "guard_control",
            "status": "TAKE_CONTROL"
        }
        await websocket.send(json.dumps(control_message))
        print("[Personnel] Personnel has taken control of the drone.")

    def release_control_of_drone(self, websocket):
        self.drone_control = False
        control_message = {
            "type": "guard_control",
            "status": "RELEASE_CONTROL"
        }
        websocket.send(json.dumps(control_message))
        print("Personnel has released control of the drone.")

    def handle_alert(self, websocket, alert_data):
        print(f"Handling alert: {alert_data}")
        if self.assess_threat(alert_data["anomalies"]):
            print("ALERT! Scavenger detected. Sending alarm signal.")
            alert_message = {
                "type": "alarm",
                "status": "ALERT",
                "position": alert_data.get("position", None)
            }
            self.record_assessment(True)
            websocket.send(json.dumps(alert_message))
        else:
            print("False alarm. No scavenger detected.")
            alert_message = {"type": "alarm", "status": "FALSE_ALARM"}
            websocket.send(json.dumps(alert_message))

    def assess_threat(self, anomalies):
        for anomaly in anomalies:
            # Check if the detected object is a scavenger with high confidence
            if anomaly["class"] == "thief" and anomaly["confidence"] > 0.8:
                return True
        return False
    
    def record_assessment(self, was_correct):
        """Record the success/failure of a threat assessment"""
        self.success_metrics['alerts_handled'] += 1
        if was_correct:
            self.success_metrics['correct_assessments'] += 1
        
        if self.success_metrics['alerts_handled'] > 0:
            self.success_metrics['assessment_accuracy'] = (
                self.success_metrics['correct_assessments'] / 
                self.success_metrics['alerts_handled']
            )

    def get_success_rate(self):
        """Get the personnel's success rate"""
        return self.success_metrics['assessment_accuracy']