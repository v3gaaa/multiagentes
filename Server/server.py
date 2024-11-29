import asyncio
import websockets
import json
import os
import sys
import matplotlib.pyplot as plt

# Añadir el directorio raíz del proyecto al Python Path
current_dir = os.path.dirname(os.path.abspath(__file__))
project_root = os.path.abspath(os.path.join(current_dir, ".."))
sys.path.append(project_root)

from Agents.Camera import Camera
from Agents.Drone import Drone
from Agents.Personnel import Personnel
from Agents.Environment import Environment
from successMetrics.metrics_tracker import SuccessMetricsTracker

# Inicialización de los agentes y entorno
cameras = [
    Camera(camera_id=1, position={"x": 23.5, "y": 8, "z": 29}),
    Camera(camera_id=2, position={"x": 23.5, "y": 8, "z": 0}),
    Camera(camera_id=3, position={"x": 0, "y": 8, "z": 1})
]

# Límites del almacén: (x_min, x_max, y_min, y_max, z_min, z_max)
boundaries = (0, 24, 0, 9, 0, 30)

# Inicialización del dron con los límites del almacén
drone = Drone(
    position={"x": 16, "y": 0, "z": 1},
    patrol_route=[
        {"x": 14, "y": 8, "z": 8},
        {"x": 20, "y": 8, "z": 20},
        {"x": 8, "y": 8, "z": 8}
    ],
    boundaries=boundaries
)

personnel = Personnel(control_station={"x": 14, "y": 0, "z": 1})

environment = Environment(
    boundaries=boundaries,
    cameras=cameras,
    drone=drone,
    personnel=personnel,
)

# Manejo de conexiones y mensajes
async def handler(websocket):
    print(f"[Server] Connection established with {websocket.remote_address}")
    try:
        async for message in websocket:
            data = json.loads(message)
            message_type = data.get("type")
            print(f"[Server] Received message of type: {message_type}")

            if message_type == "camera_frame":
                camera_id = data["camera_id"]
                for camera in cameras:
                    if camera.camera_id == camera_id:
                        await camera.process_image(data["image"], websocket)

            elif message_type == "drone_camera_frame":
                await drone.investigate_area(websocket, data["image"])

            elif message_type == "drone_investigation_command":
                target_position = data.get("target_position")
                await drone.navigate_and_investigate(websocket, target_position)

            elif message_type == "guard_control":
                if data.get("status") == "TAKE_CONTROL":
                    await personnel.handle_guard_control(websocket, drone)
                elif data.get("status") == "RELEASE_CONTROL":
                    await personnel.release_control_of_drone()
            elif message_type == "guard_control_detection":
                detection = data.get("detection")
                print(f"[Server] Detection during guard control: {detection}")
                if detection and detection.get("confidence", 0) > 0.8:
                    alert_message = {
                        "type": "alarm",
                        "status": "ALERT",
                        "position": detection.get("world_position", None),
                        "confidence": detection.get("confidence", 0)
                    }
                    print(f"[Server] High-confidence detection during guard control. Sending alert: {alert_message}")
                    await websocket.send(json.dumps(alert_message))
                else:
                    print("[Server] Detection during guard control did not meet confidence threshold.")



    except Exception as e:
        print(f"[Server] Error: {e}")


async def start_server():
    """
    Inicia el servidor WebSocket.
    """
    try:
        print("Starting WebSocket server...")
        async with websockets.serve(handler, "localhost", 8765):
            print("Server started")
            await asyncio.Future()  # Mantiene el servidor corriendo
    
    except asyncio.CancelledError:
        print("Server shut down.")
    
    finally: 
        generate_combined_metrics_report() 


def generate_combined_metrics_report():
    """Generate comprehensive metrics reports for all agents"""
    output_dir = 'metrics_reports'
    os.makedirs(output_dir, exist_ok=True)

    # Generate reports for each agent
    camera_reports = [camera.metrics_tracker.generate_metrics_report() for camera in cameras]
    drone_report = drone.metrics_tracker.generate_metrics_report()
    personnel_report = personnel.metrics_tracker.generate_metrics_report()

    # Combine metrics
    combined_metrics = {
        'Camera': cameras[0].metrics_tracker.get_metrics(),
        'Drone': drone.metrics_tracker.get_metrics(),
        'Personnel': personnel.metrics_tracker.get_metrics()
    }

    # Generate a text summary
    summary_path = os.path.join(output_dir, 'combined_metrics_summary.txt')
    with open(summary_path, 'w') as f:
        f.write("Security system success metrics summary\n")
        f.write("===========================================\n\n")
        for agent, metrics in combined_metrics.items():
            f.write(f"{agent} Agent metrics:\n")
            for key, value in metrics.items():
                f.write(f"  {key.replace('_', ' ').title()}: {value}\n")
            f.write("\n")

    # Print confirmation
    print("Metrics reports generated:")
    print(f"Camera reports: {camera_reports}")
    print(f"Drone report: {drone_report}")
    print(f"Personnel report: {personnel_report}")
    print(f"Combined summary: {summary_path}")

    # Generate a combined visualization
    try:
        plt.figure(figsize=(15, 10))
        plt.suptitle("Combined security system success metrics summary", fontsize=16)

        # Detection accuracy subplot
        plt.subplot(2, 2, 1)
        for name, metrics in combined_metrics.items():
            plt.bar(name, metrics['detection_accuracy'], label=name)
        plt.title('Detection accuracy comparison')
        plt.ylabel('Accuracy')
        plt.ylim(0, 1)

        # Investigation efficiency subplot
        plt.subplot(2, 2, 2)
        for name, metrics in combined_metrics.items():
            plt.bar(name, metrics['investigation_efficiency'], label=name)
        plt.title('Investigation efficiency comparison')
        plt.ylabel('Efficiency')
        plt.ylim(0, 1)

        # Total alerts subplot
        plt.subplot(2, 2, 3)
        for name, metrics in combined_metrics.items():
            plt.bar(name, metrics['total_alerts'], label=name)
        plt.title('Total alerts by agent')
        plt.ylabel('Number of alerts')

        # True positives subplot
        plt.subplot(2, 2, 4)
        for name, metrics in combined_metrics.items():
            plt.bar(name, metrics['true_positives'], label=name)
        plt.title('True positive detections by agent')
        plt.ylabel('True positive count')

        plt.tight_layout()
        combined_plot_path = os.path.join(output_dir, 'combined_system_metrics.png')
        plt.savefig(combined_plot_path)
        plt.close()

        print(f"Combined system metrics plot: {combined_plot_path}")
    except Exception as e:
        print(f"Error generating combined plot: {e}")


if __name__ == "__main__":
    asyncio.run(start_server())