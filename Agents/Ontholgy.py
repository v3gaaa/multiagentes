class Onthology:
    def __init__(self):
        self.known_objects = []

    def classify_object(self, obj):
        """
        Clasifica un objeto como sospechoso o no basado en reglas predefinidas.
        :param obj: Objeto detectado.
        :return: True si es sospechoso, False si no.
        """
        suspicious_objects = ["intruder"]
        return obj["class"] in suspicious_objects
