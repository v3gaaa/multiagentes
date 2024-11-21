using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System.IO;
using System;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private string serverUrl = "ws://localhost:8765";
    private Dictionary<int, Camera> surveillanceCameras;
    private GameObject drone;
    private Camera droneCamera;
    private List<Vector3> patrolRoute;
    private int currentWaypointIndex = 0;
    
    public float droneSpeed = 5f;
    public float rotationSpeed = 90f;
    public float cameraPanSpeed = 30f;
    public float cameraTiltAngle = 30f; // Fixed tilt angle
    private Vector3 warehouseCenter = new Vector3(12, 0, 12); // Assuming warehouse center

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
        // WebSocket initialization
        ws = new WebSocket(serverUrl);
        ws.OnMessage += OnMessageReceived;
        ws.OnError += OnError;
        ws.Connect();

        Debug.Log("Connected to WebSocket server");

        // Initial setup
        drone = GameObject.Find("drone");
        droneCamera = drone.GetComponentInChildren<Camera>();
        if (droneCamera == null)
        {
            Debug.LogError("Drone camera not found!");
        }

        InitializeCameras();
        InitializePatrolRoute();

        // Start simulation
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
        patrolRoute = new List<Vector3>
        {
            new Vector3(4, 8, 4),
            new Vector3(18, 8, 4),
            new Vector3(18, 8, 18),
            new Vector3(4, 8, 18),
            new Vector3(10, 8, 10) // Center waypoint
        };
    }

    private IEnumerator DronePatrol()
    {
        while (true)
        {
            Vector3 targetWaypoint = patrolRoute[currentWaypointIndex];

            // Move towards the waypoint
            while (Vector3.Distance(drone.transform.position, targetWaypoint) > 0.1f)
            {
                Vector3 direction = (targetWaypoint - drone.transform.position).normalized;

                // Smooth movement
                drone.transform.position = Vector3.MoveTowards(drone.transform.position, targetWaypoint, droneSpeed * Time.deltaTime);

                // Smooth rotation towards the waypoint
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    drone.transform.rotation = Quaternion.RotateTowards(drone.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                // Adjust camera to focus on the warehouse center or areas of interest
                FocusCameraOnRelevantArea();

                // Send drone position periodically
                SendDronePosition(drone.transform.position);

                yield return null;
            }

            // Move to next waypoint
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolRoute.Count;

            // Short pause at waypoint
            yield return new WaitForSeconds(1f);
        }
    }

    private void FocusCameraOnRelevantArea()
    {
        if (droneCamera != null)
        {
            // Calculate direction to the center of the warehouse or other relevant area
            Vector3 focusPoint = warehouseCenter - drone.transform.position;
            Quaternion targetCameraRotation = Quaternion.LookRotation(focusPoint, Vector3.up);

            // Tilt camera slightly downward
            targetCameraRotation *= Quaternion.Euler(cameraTiltAngle, 0, 0);

            // Smoothly rotate the camera to focus on the target area
            droneCamera.transform.localRotation = Quaternion.RotateTowards(droneCamera.transform.localRotation, targetCameraRotation, cameraPanSpeed * Time.deltaTime);
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

                // Capture an image from the camera
                RenderTexture renderTexture = new RenderTexture(960, 540, 24);
                camera.targetTexture = renderTexture;
                Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
                camera.targetTexture = null;
                RenderTexture.active = null;

                byte[] imageBytes = texture.EncodeToJPG();
                Destroy(renderTexture);
                Destroy(texture);

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

            yield return new WaitForSeconds(5f);
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
            ws.Send(jsonMessage);
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
            ws.Send(jsonMessage);
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
