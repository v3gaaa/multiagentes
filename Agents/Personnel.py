import json

class Personnel:
    def __init__(self, control_station):
        self.control_station = control_station
        self.drone_control = False

    def take_control_of_drone(self, drone, websocket):
        """
        Toma control del dron.
        :param drone: Objeto del dron.
        :param websocket: WebSocket para comunicación.
        """
        self.drone_control = True
        control_message = {
            "type": "guard_control",
            "status": "TAKE_CONTROL"
        }
        websocket.send(json.dumps(control_message))
        print("Personnel has taken control of the drone.")

    def release_control_of_drone(self, drone, websocket):
        """
        Libera el control del dron.
        :param drone: Objeto del dron.
        :param websocket: WebSocket para comunicación.
        """
        self.drone_control = False
        control_message = {
            "type": "guard_control",
            "status": "RELEASE_CONTROL"
        }
        websocket.send(json.dumps(control_message))
        print("Personnel has released control of the drone.")

    def handle_alert(self, websocket, alert_data):
        """
        Maneja una alerta recibida.
        :param websocket: WebSocket de comunicación.
        :param alert_data: Datos de la alerta.
        """
        print(f"Handling alert: {alert_data}")
        if self.assess_threat(alert_data["anomalies"]):
            print("ALERT! Scavenger detected. Sending alarm signal.")
            alert_message = {
                "type": "alarm",
                "status": "ALERT",
                "position": alert_data.get("position", None)
            }
            websocket.send(json.dumps(alert_message))
        else:
            print("False alarm. No scavenger detected.")
            alert_message = {"type": "alarm", "status": "FALSE_ALARM"}
            websocket.send(json.dumps(alert_message))

    def assess_threat(self, anomalies):
        """
        Evalúa si las anomalías representan una amenaza real (scavenger).
        :param anomalies: Lista de anomalías detectadas.
        :return: True si se detecta un scavenger, False si no.
        """
        for anomaly in anomalies:
            # Check if the detected object is a scavenger with high confidence
            if anomaly["class"] == "scavenger" and anomaly["confidence"] > 0.8:
                return True
        return False