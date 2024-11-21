# Personnel.py
# Este agente representa al personal de seguridad. Es un agente BDI (Belief-Desire-Intention).

class Personnel:
    def __init__(self, control_station):
        """
        Inicializa el personal de seguridad.
        :param control_station: Posición de la estación de control (x, y, z)
        """
        self.in_control = False  # Indica si el personal está controlando el dron
        self.control_station = control_station

    def take_control(self, drone, certainty):
        """
        Simula el proceso de tomar el control del dron.
        :param drone: Instancia del dron a controlar.
        :param certainty: Certeza proporcionada por la cámara o el dron.
        """
        if self.is_at_control_station(drone.current_position):
            print(f"Taking control of the drone with certainty: {certainty}")
            self.in_control = True

            # Simulación de evaluación
            is_dangerous = certainty > 0.7
            if is_dangerous:
                print("Threat confirmed. Triggering alarm.")
                self.trigger_alarm()
            else:
                print("False alarm. Returning drone to patrol.")
                drone.patrol([drone.current_position])  # Retomar patrulla
        else:
            print("Guard not at control station. Cannot take control.")

    def trigger_alarm(self):
        """Simula el disparo de una alarma."""
        print("Alarm triggered! Security notified.")

    def is_at_control_station(self, position):
        """
        Verifica si el guardia está en la estación de control.
        :param position: Posición actual del dron.
        :return: True si está en la estación, False si no.
        """
        return position == self.control_station
