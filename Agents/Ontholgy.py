class Onthology:
    def __init__(self):
        self.known_objects = []

    def classify_object(self, obj):
        suspicious_objects = ["thief"]
        return obj["class"] in suspicious_objects
    