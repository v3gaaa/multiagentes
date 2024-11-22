class Environment:
    def __init__(self, boundaries, cameras, drone, personnel):
        self.boundaries = boundaries
        self.cameras = cameras
        self.drone = drone
        self.personnel = personnel

    def is_within_boundaries(self, position):
        """
        Verifica si una posición está dentro de los límites del almacén.
        :param position: Coordenadas (x, y, z).
        :return: True si está dentro, False si no.
        """
        x, y, z = position
        x_min, x_max, y_min, y_max, z_min, z_max = self.boundaries
        return x_min <= x <= x_max and y_min <= y <= y_max and z_min <= z <= z_max
