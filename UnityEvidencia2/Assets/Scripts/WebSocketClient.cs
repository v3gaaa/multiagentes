using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System.IO;
using System;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private string serverUrl = "ws://localhost:8765"; // URL del servidor WebSocket
    private Dictionary<int, Camera> surveillanceCameras;
    private GameObject drone;
    private Camera droneCamera;
    private Vector3[] patrolRoute;

    // Message class to help with JSON serialization
    [Serializable]
    private class Message
    {
        public string type;
        public object position;
        public int camera_id;
        public string image;
    }

    private void Start()
    {
        // Inicialización de WebSocket
        ws = new WebSocket(serverUrl);
        ws.OnMessage += OnMessageReceived;
        ws.OnError += OnError;
        ws.Connect();

        Debug.Log("Connected to WebSocket server");

        // Configuración inicial
        drone = GameObject.Find("drone");
        droneCamera = drone.GetComponentInChildren<Camera>();
        
        if (droneCamera == null)
        {
            Debug.LogError("Drone camera not found!");
        }

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
            if (cameraObject != null)
            {
                Camera camera = cameraObject.GetComponentInChildren<Camera>();
                surveillanceCameras.Add(i + 1, camera);
            }
            else
            {
                Debug.LogError($"Surveillance Camera ({i}) not found!");
            }
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

                // Capturar y enviar frame de la cámara del dron
                SendDroneFrame();

                yield return new WaitForSeconds(3); // Esperar antes de moverse al siguiente punto
            }
        }
    }

    private void SendDroneFrame()
    {
        if (droneCamera == null)
        {
            Debug.LogError("Drone camera is null. Cannot capture frame.");
            return;
        }

        // Capturar una imagen de la cámara del dron
        RenderTexture renderTexture = new RenderTexture(960, 540, 24);
        droneCamera.targetTexture = renderTexture;
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        droneCamera.Render();
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        droneCamera.targetTexture = null;
        RenderTexture.active = null;

        // Convertir la imagen a formato JPG
        byte[] imageBytes = texture.EncodeToJPG();
        Destroy(renderTexture);
        Destroy(texture);

        // Validar que los bytes no estén vacíos antes de enviar
        if (imageBytes.Length > 0)
        {
            SendCameraFrame(4, imageBytes); // Usar ID 4 para la cámara del dron
            Debug.Log("Sent frame from drone camera");
        }
        else
        {
            Debug.LogError("Failed to capture image from drone camera");
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
                RenderTexture renderTexture = new RenderTexture(960, 540, 24);
                camera.targetTexture = renderTexture;
                Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
                camera.targetTexture = null;
                RenderTexture.active = null;

                // Convertir la imagen a formato JPG
                byte[] imageBytes = texture.EncodeToJPG();
                Destroy(renderTexture);
                Destroy(texture);

                // Validar que los bytes no estén vacíos antes de enviar
                if (imageBytes.Length > 0)
                {
                    SendCameraFrame(cameraId, imageBytes);
                    Debug.Log($"Sent frame from camera {cameraId}");
                }
                else
                {
                    Debug.LogError($"Failed to capture image from camera {cameraId}");
                }
            }

            yield return new WaitForSeconds(5); // Esperar antes de enviar el siguiente frame
        }
    }

    private void SendDronePosition(Vector3 position)
    {
        if (ws.ReadyState == WebSocketState.Open)
        {
            var message = new Message
            {
                type = "drone_position",
                position = new { x = position.x, y = position.y, z = position.z }
            };

            string jsonMessage = JsonUtility.ToJson(message);
            Debug.Log($"Sending drone position: {jsonMessage}");
            ws.Send(jsonMessage);
        }
        else
        {
            Debug.LogError("WebSocket is not open. Cannot send drone position.");
        }
    }

    private void SendCameraFrame(int cameraId, byte[] imageBytes)
    {
        if (ws.ReadyState == WebSocketState.Open)
        {
            var message = new Message
            {
                type = "camera_frame",
                camera_id = cameraId,
                image = Convert.ToBase64String(imageBytes)
            };

            string jsonMessage = JsonUtility.ToJson(message);
            Debug.Log($"Sending camera frame: {jsonMessage}");
            ws.Send(jsonMessage);
        }
        else
        {
            Debug.LogError("WebSocket is not open. Cannot send camera frame.");
        }
    }

    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        Debug.Log($"Message received from server: {e.Data}");
    }

    private void OnError(object sender, WebSocketSharp.ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket error: {e.Message}");
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