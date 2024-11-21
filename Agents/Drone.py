# Drone.py
# Este agente representa un dron que patrulla el entorno y responde a alertas. Es un agente híbrido (reactivo y deductivo).

class Drone:
    def __init__(self, start_position, boundaries):
        """
        Inicializa el dron con su posición de inicio.
        :param start_position: Posición inicial del dron (x, y, z)
        :param boundaries: Límites del almacén (x_min, x_max, z_min, z_max)
        """
        self.current_position = start_position
        self.target_position = None
        self.certainty = None  # Certeza de la detección actual
        self.investigating = False  # Indica si está investigando un área específica
        self.flying = False
        self.boundaries = boundaries  # Límites del almacén (x_min, x_max, z_min, z_max)

    def take_off(self):
        """Simula el despegue del dron."""
        self.flying = True
        print(f"Drone took off from {self.current_position}")

    def land(self):
        """Simula el aterrizaje del dron."""
        self.flying = False
        print(f"Drone landed at {self.current_position}")

    def patrol(self, route):
        """
        Simula la patrulla del dron siguiendo una ruta.
        :param route: Lista de posiciones (x, y, z) que el dron debe recorrer.
        """
        for position in route:
            if self.is_within_boundaries(position):
                self.current_position = position
                print(f"Drone is patrolling at {self.current_position}")
            else:
                print(f"Skipped position {position}, out of boundaries.")

    def investigate(self, position, certainty):
        """
        Simula la investigación de un área sospechosa.
        Aquí debería integrarse el modelo de visión computacional.
        :param position: Posición objetivo a investigar.
        :param certainty: Certeza de la alerta.
        """
        if self.is_within_boundaries(position):
            print(f"Drone moving to {position} to investigate.")
            self.current_position = position
            self.certainty = certainty

            # Simulación de detección: Valor arbitrario para pruebas
            is_dangerous = self.certainty > 0.7
            print(f"Investigation complete. Dangerous: {is_dangerous}")
            return is_dangerous
        else:
            print(f"Target position {position} is out of boundaries.")
            return False

    def is_within_boundaries(self, position):
        """
        Verifica si una posición está dentro de los límites del almacén.
        :param position: (x, y, z)
        :return: True si está dentro de los límites, False si no.
        """
        x, _, z = position
        x_min, x_max, z_min, z_max = self.boundaries
        return x_min <= x <= x_max and z_min <= z <= z_max
