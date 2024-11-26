using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System.IO;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private string serverUrl = "ws://localhost:8765";
    private Dictionary<int, Camera> surveillanceCameras;
    private GameObject drone;
    private GameObject guard;
    private Camera droneCamera;
    private List<Vector3> patrolRoute;
    private int currentWaypointIndex = 0;
    private bool isDroneControlled = false;
    private bool isInvestigating = false;
    private Vector3 landingStation = new Vector3(3, 0, 1);
    private Vector3 controlStation = new Vector3(14, 0, 1);

    public float droneSpeed = 5f;
    public float guardSpeed = 3f;

    private void Start()
    {
        EnsureSingleAudioListener();
        InitializeComponents();
        ConnectWebSocket();
        StartCoroutine(SendCameraFrames());
        StartCoroutine(SendDroneCameraFrames());

        ws = new WebSocket("ws://localhost:8765");
        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Message from server: " + e.Data);
        };
        ws.Connect();
    }

    private void Update()
    {
        if (!isDroneControlled && !isInvestigating)
        {
            PatrolDrone();
        }
        else if (isDroneControlled)
        {
            Debug.Log("Drone is under manual control.");
        }
    }

    private void PatrolDrone()
    {
        if (currentWaypointIndex >= patrolRoute.Count)
        {
            // Drone lands when patrol completes
            MoveDroneTo(landingStation);
            Debug.Log("Drone returning to landing station.");
            return;
        }

        Vector3 target = patrolRoute[currentWaypointIndex];
        float step = droneSpeed * Time.deltaTime;
        drone.transform.position = Vector3.MoveTowards(drone.transform.position, target, step);

        // Ajustar la rotación del dron
        Vector3 direction = (target - drone.transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            drone.transform.rotation = Quaternion.RotateTowards(drone.transform.rotation, toRotation, step * 100);
        }

        if (Vector3.Distance(drone.transform.position, target) < 0.1f)
        {
            currentWaypointIndex++;
        }
    }

    private void InitializeComponents()
    {
        drone = GameObject.Find("drone");
        droneCamera = drone.GetComponentInChildren<Camera>();
        guard = GameObject.Find("Guard");

        if (droneCamera == null)
            Debug.LogError("Drone camera not found!");

        InitializeCameras();
        InitializePatrolRoute();

        // Set initial positions
        drone.transform.position = landingStation;
        guard.transform.position = new Vector3(11, 0, 8);
    }

    private void InitializeCameras()
    {
        surveillanceCameras = new Dictionary<int, Camera>();
        Vector3[] cameraPositions = new Vector3[]
        {
            new Vector3(23.5f, 8f, 29f),
            new Vector3(23.5f, 8f, 0f),
            new Vector3(0f, 8f, 1f)
        };

        for (int i = 0; i < cameraPositions.Length; i++)
        {
            GameObject cameraObject = GameObject.Find($"Surveillance Camera ({i})");
            if (cameraObject != null)
            {
                Camera camera = cameraObject.GetComponentInChildren<Camera>();
                surveillanceCameras.Add(i + 1, camera);
                cameraObject.transform.position = cameraPositions[i];
            }
            else
            {
                Debug.LogError($"Surveillance Camera ({i}) not found!");
            }
        }
    }

    private void InitializePatrolRoute()
    {
        patrolRoute = new List<Vector3>
        {
            new Vector3(4, 7, 3),     
            new Vector3(14, 7, 3),  
            new Vector3(21, 7, 3), 

            new Vector3(21, 7, 9),    
            new Vector3(14, 7, 9),    
            new Vector3(4, 7, 9), 

            new Vector3(4, 7, 15), 
            new Vector3(14, 7, 15),    
            new Vector3(21, 7, 15),

            new Vector3(21, 7, 21), 
            new Vector3(14, 7, 21),   
            new Vector3(4, 7, 21),     

            new Vector3(4, 7, 27),    
            new Vector3(14, 7, 27),    
            new Vector3(21, 7, 27), 

            new Vector3(4, 7, 3),     
            new Vector3(14, 7, 3),  
            new Vector3(21, 7, 3), 

            new Vector3(21, 7, 9),    
            new Vector3(14, 7, 9),    
            new Vector3(4, 7, 9), 

            new Vector3(4, 7, 15), 
            new Vector3(14, 7, 15),    
            new Vector3(21, 7, 15),

            new Vector3(21, 7, 21), 
            new Vector3(14, 7, 21),   
            new Vector3(4, 7, 21),     

            new Vector3(4, 7, 27),    
            new Vector3(14, 7, 27),    
            new Vector3(21, 7, 27), 

            new Vector3(4, 7, 3),     
            new Vector3(14, 7, 3),  
            new Vector3(21, 7, 3), 

            new Vector3(21, 7, 9),    
            new Vector3(14, 7, 9),    
            new Vector3(4, 7, 9), 

            new Vector3(4, 7, 15), 
            new Vector3(14, 7, 15),    
            new Vector3(21, 7, 15),

            new Vector3(21, 7, 21), 
            new Vector3(14, 7, 21),   
            new Vector3(4, 7, 21),     

            new Vector3(4, 7, 27),    
            new Vector3(14, 7, 27),    
            new Vector3(21, 7, 27),
        };
    }

    private void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);
        ws.OnMessage += OnMessageReceived;
        ws.OnError += OnError;
        ws.Connect();
        Debug.Log("Connected to WebSocket server");
    }

    private void OnMessageReceived(object sender, MessageEventArgs e)
    {
        Debug.Log("Message received from server: " + e.Data);

        try
        {
            var data = JsonUtility.FromJson<Message>(e.Data);
            switch (data.type)
            {
                case "camera_alert":
                    // Log details of detected objects
                    if (data.detections != null && data.detections.Count > 0)
                    {
                        foreach (var detection in data.detections)
                        {
                            if (detection.className == "thiefs") // Check for enemies
                            {
                                Debug.Log($"Enemy detected! Details: Position: ({detection.x}, {detection.y}), Size: {detection.width}x{detection.height}, Confidence: {detection.confidence}");
                            }
                        }
                    }
                    break;

                case "drone_control":
                    isDroneControlled = true;
                    isDroneControlled = data.status == "TAKE_CONTROL";
                    Debug.Log(isDroneControlled ? "Personnel took control of the drone." : "Personnel released control of the drone.");
                    break;

                case "alarm":
                    Debug.Log(data.status == "ALERT" ? "ALERT! Real threat detected." : "False alarm.");
                    break;

                default:
                    Debug.LogWarning("Unknown message type received.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to process message: " + ex.Message);
        }
    }

    private void MoveDroneTo(Vector3 target)
    {
        StopAllCoroutines(); // Stop patrol
        StartCoroutine(MoveDrone(target));
    }

    private IEnumerator MoveDrone(Vector3 target)
    {
        while (Vector3.Distance(drone.transform.position, target) > 0.1f)
        {
            float step = droneSpeed * Time.deltaTime;
            drone.transform.position = Vector3.MoveTowards(drone.transform.position, target, step);
            yield return null;
        }

        // Simulate investigation
        yield return new WaitForSeconds(5f);

        // Notify server to analyze drone image
        byte[] droneImageBytes = CaptureDroneCameraFrame();
        SendDroneCameraFrame(droneImageBytes);

        isInvestigating = false;
    }

    private byte[] CaptureDroneCameraFrame()
    {
        if (droneCamera != null)
        {
            RenderTexture renderTexture = new RenderTexture(960, 540, 24);
            droneCamera.targetTexture = renderTexture;
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            droneCamera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            droneCamera.targetTexture = null;
            RenderTexture.active = null;

            byte[] imageBytes = texture.EncodeToJPG();
            Destroy(renderTexture);
            Destroy(texture);

            return imageBytes;
        }

        Debug.LogError("Drone camera is null! Unable to capture frame.");
        return null;
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
                    //Debug.Log($"Sent frame from camera {cameraId}");
                }
                else
                {
                    Debug.LogError($"Failed to capture image from camera {cameraId}");
                }
            }

            yield return new WaitForSeconds(5f); //Speed to change pictirus
        }
    }

    private IEnumerator SendDroneCameraFrames()
    {
        while (true)
        {
            byte[] imageBytes = CaptureDroneCameraFrame();
            if (imageBytes != null)
            {
                SendDroneCameraFrame(imageBytes);
            }

            yield return new WaitForSeconds(1f);
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

    private void SendDroneCameraFrame(byte[] imageBytes)
    {
        if (ws.ReadyState == WebSocketState.Open)
        {
            var message = new Message
            {
                type = "drone_camera_frame",
                image = Convert.ToBase64String(imageBytes)
            };

            string jsonMessage = JsonUtility.ToJson(message);
            ws.Send(jsonMessage);
        }
    }

    private void OnError(object sender, WebSocketSharp.ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket Error: {e.Message}");
    }

    private void EnsureSingleAudioListener()
    {
        AudioListener[] audioListeners = FindObjectsOfType<AudioListener>();
        if (audioListeners.Length > 1)
        {
            for (int i = 1; i < audioListeners.Length; i++)
            {
                audioListeners[i].enabled = false;
            }
            Debug.LogWarning("Multiple AudioListeners found. Disabled all except one.");
        }
    }

    public void TakeControl()
    {
        var message = new Message { type = "manual_control", action = "take_control" };
        ws.Send(JsonUtility.ToJson(message));
        Debug.Log("Personnel took control of the drone.");
    }

    public void ReleaseControl()
    {
        var message = new Message { type = "manual_control", action = "release_control" };
        ws.Send(JsonUtility.ToJson(message));
        Debug.Log("Personnel released control of the drone.");
    }

    void OnDestroy()
    {
        ws.Close();
    }

    

        [Serializable]
    private class Message
    {
        public string type;
        public int camera_id;
        public string image;
        public Vector3 position;
        public string status;
        public string action;
        public List<Detection> detections;
    }

    [Serializable]
    private class Detection
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public float confidence;
        public string className;
        public int classId;
        public string detectionId;
    }

}
