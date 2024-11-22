import json


class Personnel:
    def __init__(self, control_station):
        self.control_station = control_station
        self.drone_control = False

    def take_control_of_drone(self, drone):
        """
        Toma control del dron.
        :param drone: Objeto del dron.
        """
        self.drone_control = True
        print("Personnel has taken control of the drone.")

    def release_control_of_drone(self, drone):
        """
        Libera el control del dron.
        :param drone: Objeto del dron.
        """
        self.drone_control = False
        print("Personnel has released control of the drone.")

    def handle_alert(self, websocket, alert_data):
        """
        Maneja una alerta recibida.
        :param websocket: WebSocket de comunicación.
        :param alert_data: Datos de la alerta.
        """
        print(f"Handling alert: {alert_data}")
        if self.assess_threat(alert_data["anomalies"]):
            print("ALERT! Sending alarm signal.")
            alert_message = {"type": "alarm", "status": "ALERT"}
            websocket.send(json.dumps(alert_message))
        else:
            print("False alarm. Resuming normal operations.")
            alert_message = {"type": "alarm", "status": "FALSE_ALARM"}
            websocket.send(json.dumps(alert_message))

    def assess_threat(self, anomalies):
        """
        Evalúa si las anomalías representan una amenaza real.
        :param anomalies: Lista de anomalías detectadas.
        :return: True si es una amenaza, False si no.
        """
        for anomaly in anomalies:
            if anomaly["confidence"] > 0.8:
                return True
        return False
