# Camera.py
# Este agente representa una cámara fija en el entorno. Es un agente reactivo que detecta movimiento o actividades sospechosas.

class Camera:
    def __init__(self, id, position):
        """
        Inicializa la cámara con un ID único y una posición fija en el entorno.
        :param id: ID único de la cámara
        :param position: Posición fija de la cámara en el espacio (x, y, z)
        """
        self.id = id
        self.position = position
        self.certainty = 0.0  # Certeza inicial de la detección
        self.alert_triggered = False

    def detect_movement(self):
        """
        Simula la detección de movimiento usando valores arbitrarios.
        Aquí debería integrarse el modelo de visión computacional (como YOLO).
        """
        import random
        self.certainty = random.uniform(0.0, 1.0)  # Generar certeza aleatoria
        
        if self.certainty > 0.5:  # Activar alerta si la certeza es mayor al umbral
            self.alert_triggered = True
            print(f"Camera {self.id}: Movement detected at {self.position} with certainty {self.certainty}.")
            return {
                "id": self.id,
                "position": self.position,
                "certainty": self.certainty
            }
        return None
