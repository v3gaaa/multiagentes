# Environment.py
# Representa el entorno de simulación. Es un entorno discreto y parcialmente observable.

class Environment:
    def __init__(self, dimensions, cameras):
        """
        Inicializa el entorno.
        :param dimensions: Dimensiones del entorno (x, y, z).
        :param cameras: Lista de cámaras en el entorno.
        """
        self.dimensions = dimensions  # (x_max, y_max, z_max)
        self.cameras = cameras  # Lista de instancias de Camera

    def get_alerts(self):
        """
        Simula la recopilación de alertas de todas las cámaras.
        """
        alerts = []
        for camera in self.cameras:
            alert = camera.detect_movement()
            if alert:
                alerts.append(alert)
        return alerts
