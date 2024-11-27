from utils.PositionUtils import PositionUtils

class Environment:
    def __init__(self, boundaries, cameras, drone, personnel):
        self.boundaries = boundaries
        self.cameras = cameras
        self.drone = drone
        self.personnel = personnel

    def is_within_boundaries(self, position):
        return PositionUtils.validate_position(position, self.boundaries)