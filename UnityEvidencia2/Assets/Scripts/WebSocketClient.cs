using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System.IO;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private string serverUrl = "ws://localhost:8765"; // URL del servidor WebSocket
    private Dictionary<int, Camera> surveillanceCameras;
    private GameObject drone;
    private GameObject guard;
    private Vector3[] patrolRoute;

    private void Start()
    {
        // Inicialización de WebSocket
        ws = new WebSocket(serverUrl);
        ws.OnMessage += OnMessageReceived;
        ws.Connect();

        Debug.Log("Connected to WebSocket server");

        // Configuración inicial
        drone = GameObject.Find("drone");
        guard = GameObject.Find("Guard");
        InitializeCameras();
        InitializePatrolRoute();

        // Iniciar simulación
        StartCoroutine(DronePatrol());
        StartCoroutine(SendCameraFrames());
    }

    private void InitializeCameras()
    {
        surveillanceCameras = new Dictionary<int, Camera>();

        for (int i = 0; i < 3; i++)
        {
            GameObject cameraObject = GameObject.Find($"Surveillance Camera ({i})");
            Camera camera = cameraObject.GetComponentInChildren<Camera>();
            surveillanceCameras.Add(i + 1, camera);
        }

        Debug.Log("Cameras initialized");
    }

    private void InitializePatrolRoute()
    {
        // Definir una ruta de patrulla predefinida para el dron
        patrolRoute = new Vector3[]
        {
            new Vector3(5, 8, 5),
            new Vector3(20, 8, 5),
            new Vector3(20, 8, 20),
            new Vector3(5, 8, 20)
        };
    }

    private IEnumerator DronePatrol()
    {
        while (true)
        {
            foreach (var waypoint in patrolRoute)
            {
                // Mover el dron a la siguiente posición
                drone.transform.position = waypoint;

                // Enviar posición del dron al servidor
                SendDronePosition(drone.transform.position);
                Debug.Log($"Drone patrolling to {waypoint}");

                yield return new WaitForSeconds(3); // Esperar antes de moverse al siguiente punto
            }
        }
    }

    private IEnumerator SendCameraFrames()
    {
        while (true)
        {
            foreach (var kvp in surveillanceCameras)
            {
                int cameraId = kvp.Key;
                Camera camera = kvp.Value;

                // Capturar una imagen de la cámara
                RenderTexture renderTexture = new RenderTexture(1920, 1080, 24);
                camera.targetTexture = renderTexture;
                Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
                camera.targetTexture = null;
                RenderTexture.active = null;

                // Guardar la imagen como bytes
                byte[] imageBytes = texture.EncodeToJPG();
                Destroy(renderTexture);

                // Enviar la imagen al servidor
                SendCameraFrame(cameraId, imageBytes);
                Debug.Log($"Sent frame from camera {cameraId}");
            }

            yield return new WaitForSeconds(5); // Esperar antes de enviar el siguiente frame
        }
    }

    private void SendDronePosition(Vector3 position)
    {
        if (ws.ReadyState == WebSocketState.Open)
        {
            var message = new
            {
                type = "drone_position",
                position = new { x = position.x, y = position.y, z = position.z }
            };

            ws.Send(JsonUtility.ToJson(message));
        }
    }

    private void SendCameraFrame(int cameraId, byte[] imageBytes)
    {
        if (ws.ReadyState == WebSocketState.Open)
        {
            var message = new
            {
                type = "camera_frame",
                camera_id = cameraId,
                image = System.Convert.ToBase64String(imageBytes)
            };

            ws.Send(JsonUtility.ToJson(message));
        }
    }

    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        Debug.Log($"Message received from server: {e.Data}");

        // Procesar el mensaje recibido desde el servidor
        // Aquí puedes implementar acciones basadas en los mensajes del servidor
    }

    private void OnDestroy()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Close();
            Debug.Log("WebSocket closed");
        }
    }
}
