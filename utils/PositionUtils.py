# PositionUtils.py

class PositionUtils:
    @staticmethod
    def yolo_to_unity_position(prediction, camera_position):
        """
        Convert YOLO detection coordinates to Unity world position
        
        Args:
            prediction (dict): YOLO detection with x,y coordinates (0-1 range)
            camera_position (dict): Camera position in Unity world space
        
        Returns:
            dict: Position in Unity world coordinates
        """
        x = prediction['x']  # Center x of detection
        y = prediction['y']  # Center y of detection
        
        # Convert to world space (30 units is the scene scale)
        world_x = camera_position['x'] + (x - 0.5) * 30
        world_z = camera_position['z'] + (y - 0.5) * 30
        world_y = camera_position.get('y', 0)  # Use camera height or default to ground level
        
        return {
            "x": world_x,
            "y": world_y,
            "z": world_z
        }
    
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