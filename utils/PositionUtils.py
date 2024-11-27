# PositionUtils.py

class PositionUtils:
    @staticmethod
    def yolo_to_unity_position(detection, camera_position, boundaries):
        """
        Convert YOLO detection coordinates to Unity world space with adjusted target position.
        """
        try:
            normalized_x = detection["x"]
            normalized_y = detection["y"]

            # Adjust target position to be closer to the camera and away from edges
            offset = 5  # Offset to keep the drone closer to the camera's center
            world_x = camera_position["x"] + (normalized_x - 0.5) * 15 + offset
            world_y = camera_position["y"]
            world_z = camera_position["z"] + (normalized_y - 0.5) * 15 + offset

            # Ensure the position is within boundaries
            x_min, x_max, y_min, y_max, z_min, z_max = boundaries
            world_x = max(min(world_x, x_max - 3), x_min + 3)
            world_y = max(min(world_y, y_max), y_min)
            world_z = max(min(world_z, z_max - 3), z_min + 3)

            return {"x": world_x, "y": world_y, "z": world_z}
        except KeyError as e:
            print(f"[PositionUtils] Missing key in detection data: {e}")
            return None


    
    @staticmethod
    def unity_to_yolo_position(world_position, camera_position):
        """
        Convert Unity world position to YOLO coordinate space
        
        Args:
            world_position (dict): Position in Unity world space
            camera_position (dict): Camera position in Unity world space
        
        Returns:
            dict: Position in YOLO coordinate space (0-1 range)
        """
        # Convert back to YOLO space (0-1 range)
        x = (world_position['x'] - camera_position['x']) / 30 + 0.5
        z = (world_position['z'] - camera_position['z']) / 30 + 0.5
        
        return {
            "x": x,
            "y": z  # YOLO uses y for what Unity considers z
        }
    
    @staticmethod
    def validate_position(position, boundaries):
        """
        Validate if a position is within specified boundaries
        
        Args:
            position (dict): Position to validate
            boundaries (tuple): (x_min, x_max, y_min, y_max, z_min, z_max)
        
        Returns:
            bool: True if position is valid
        """
        x_min, x_max, y_min, y_max, z_min, z_max = boundaries
        return (x_min <= position['x'] <= x_max and
                y_min <= position['y'] <= y_max and
                z_min <= position['z'] <= z_max)