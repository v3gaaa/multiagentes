# Onthology.py
# Define una ontología básica para los agentes y propiedades en el entorno.

class Entity:
    """Clase base para todos los agentes del entorno."""
    def __init__(self, name, position):
        self.name = name
        self.position = position

class Drone(Entity):
    def __init__(self, position):
        super().__init__("Drone", position)

class Camera(Entity):
    def __init__(self, position):
        super().__init__("Camera", position)

class Personnel(Entity):
    def __init__(self, position):
        super().__init__("Personnel", position)
