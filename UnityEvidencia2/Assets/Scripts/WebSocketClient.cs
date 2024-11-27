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

    private readonly Queue<Action> mainThreadQueue = new Queue<Action>();

    public float droneSpeed = 5f;
    public float guardSpeed = 5f;

    private void Start()
    {
        EnsureSingleAudioListener();
        InitializeComponents();
        ConnectWebSocket();
        StartCoroutine(SendCameraFrames());
        StartCoroutine(SendDroneCameraFrames());
    }

    private void Update()
    {
        // Ejecuta las acciones en cola
        lock (mainThreadQueue)
        {
            while (mainThreadQueue.Count > 0)
            {
                mainThreadQueue.Dequeue().Invoke();
            }
        }

        if (!isDroneControlled && !isInvestigating)
        {
            PatrolDrone();
        }
    }


    private Vector3 ConvertToUnityPosition(Dictionary<string, float> position)
    {
        return new Vector3(position["x"], position["y"], position["z"]);
    }

    private void PatrolDrone()
    {
        if (currentWaypointIndex >= patrolRoute.Count)
        {
            // Drone lands when patrol completes
            MoveDrone(landingStation);
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
        try
        {
            Debug.Log("Message received from server: " + e.Data); 

            var data = JsonUtility.FromJson<Message>(e.Data);
            switch (data.type)
            {
                case "camera_alert":
                    Debug.Log("[Unity] Camera Alert Data: " + JsonUtility.ToJson(data));
                    if (data.position != null)
                    {
                        lock (mainThreadQueue)
                        {
                            mainThreadQueue.Enqueue(() =>
                            {
                                HandleCameraAlert(data.position);
                            });
                        }
                    }
                    else
                    {
                        Debug.LogError("Camera alert received but position is null.");
                    }
                    break;

                case "drone_control":
                    lock (mainThreadQueue)
                    {
                        mainThreadQueue.Enqueue(() =>
                        {
                            isDroneControlled = data.status == "TAKE_CONTROL";
                            Debug.Log(isDroneControlled ? "Personnel took control of the drone." : "Personnel released control of the drone.");
                        });
                    }
                    break;

                case "alarm":
                    lock (mainThreadQueue)
                    {
                        mainThreadQueue.Enqueue(() =>
                        {
                            Debug.Log(data.status == "ALERT" ? "ALERT! Real threat detected." : "False alarm.");
                        });
                    }
                    break;
                
                case "drone_alert":
                    lock (mainThreadQueue)
                    {
                        mainThreadQueue.Enqueue(() =>
                        {
                            Debug.Log("Drone Alert Data: " + JsonUtility.ToJson(data));
                            if (data.position != null)
                            {
                                HandleDroneAlert(data.position);
                            }
                            else
                            {
                                Debug.LogError("Drone alert received but position is null.");
                            }
                        });
                    }
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




    private void HandleCameraAlert(Vector3 alertPosition)
    {
        Debug.Log($"[Unity] Camera Alert Received! Drone moving to investigate position: {alertPosition}");
        lock (mainThreadQueue)
        {
            mainThreadQueue.Enqueue(() =>
            {
                StopAllCoroutines();
                isInvestigating = true;

                StartCoroutine(MoveDrone(alertPosition, () =>
                {
                    Debug.Log("[Unity] Drone arrived at investigation site. Starting investigation...");
                    StartCoroutine(InvestigateArea(() =>
                    {
                        Debug.Log("[Unity] Investigation complete. Resuming patrol...");
                        currentWaypointIndex = 0;
                        isInvestigating = false;
                    }));
                }));
            });
        }
    }


    private IEnumerator MoveDrone(Vector3 target, Action onArrival)
    {
        Debug.Log($"Drone moving to target position: {target}");
        while (Vector3.Distance(drone.transform.position, target) > 0.1f)
        {
            float step = droneSpeed * Time.deltaTime;
            drone.transform.position = Vector3.MoveTowards(drone.transform.position, target, step);
            yield return null;
        }
        Debug.Log("Drone reached the target position.");
        onArrival?.Invoke();
    }

    private void MoveDrone(Vector3 target)
    {
        StartCoroutine(MoveDrone(target, null));
    }

    private IEnumerator InvestigateArea(Action onComplete)
    {
        Debug.Log("Drone investigating the area...");

        // Calcular una posición ajustada cerca del área investigada
        Vector3 adjustedTarget = new Vector3(12, drone.transform.position.y, 15); // Ajusta según el área relevante
        Quaternion lookAtTarget = Quaternion.LookRotation(adjustedTarget - drone.transform.position);

        // Rotar suavemente hacia la posición ajustada
        while (Quaternion.Angle(drone.transform.rotation, lookAtTarget) > 0.1f)
        {
            drone.transform.rotation = Quaternion.RotateTowards(drone.transform.rotation, lookAtTarget, Time.deltaTime * 100);
            yield return null;
        }

        // Realizar paneo durante la investigación
        float investigationTime = 5f;
        float elapsed = 0f;
        while (elapsed < investigationTime)
        {
            drone.transform.Rotate(Vector3.up, Mathf.Sin(elapsed * Mathf.PI) * 10 * Time.deltaTime); // Paneo más sutil
            elapsed += Time.deltaTime;
            yield return null;
        }

        byte[] droneImageBytes = CaptureDroneCameraFrame();
        SendDroneCameraFrame(droneImageBytes);

        Debug.Log("Drone investigation complete. Resuming patrol...");
        currentWaypointIndex = 0; // Reiniciar patrullaje
        isInvestigating = false;
        onComplete?.Invoke();
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

    public void HandleDroneAlert(Vector3 alertPosition)
    {
        Debug.Log($"[Unity] Drone Alert Received! Guard alerted");
        
        lock (mainThreadQueue)
        {
            mainThreadQueue.Enqueue(() =>
            {
                StopAllCoroutines();
                isDroneControlled = true;

                StartCoroutine(MoveGuard(controlStation, () =>
                {
                    Debug.Log("[Unity] Guard arrived to control station. Starting drone control...");

                    SendGuardAtControlStationMessage();

                    StartCoroutine(HandleControlStationProcedure(() =>
                    {
                        Debug.Log("[Unity] Guard finished drone control. Resuming patrol...");
                        currentWaypointIndex = 0;
                        isDroneControlled = false;
                    }));
                }));
            });
        }

        // después de mover guard, añadir algo de que toma control del dron
    }

    private void SendGuardAtControlStationMessage()
    {
        if (ws.ReadyState == WebSocketState.Open)
        {
            var message = new Message
            {
                type = "guard_at_control_station",
                status = "READY"
            };

            string jsonMessage = JsonUtility.ToJson(message);
            ws.Send(jsonMessage);
        }
    }

    private IEnumerator HandleControlStationProcedure(Action onComplete)
    {
        Debug.Log("Guard taking control of the drone...");
        // aquí poner lo que sea que haga el guardia con el dron
        // Wait for a few seconds to simulate drone control
        for (int i = 0; i < 10; i++)
        {
            yield return null;
        }

        onComplete?.Invoke();
    }

    private IEnumerator MoveGuard(Vector3 target, Action onArrival)
    {
        Debug.Log($"Guard moving to control station: {target}");
        while (Vector3.Distance(guard.transform.position, target) > 0.1f)
        {
            float step = guardSpeed * Time.deltaTime;
            
            guard.transform.position = Vector3.MoveTowards(guard.transform.position, target, step);
            
            Vector3 direction = (target - guard.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
                guard.transform.rotation = Quaternion.RotateTowards(guard.transform.rotation, toRotation, step * 100);
            }
            
            yield return null;
        }
        
        Debug.Log("Guard reached the control station.");
        
        onArrival?.Invoke();
    }

    private void MoveGuard(Vector3 target)
    {
        StartCoroutine(MoveGuard(target, null));
    }

    private void OnError(object sender, WebSocketSharp.ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket Error: {e.Message}");
    }

    private void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            Camera mainCamera = Camera.main;
            foreach (AudioListener listener in listeners)
            {
                if (listener.gameObject != mainCamera.gameObject)
                {
                    listener.enabled = false;
                }
            }
            Debug.Log("Disabled extra AudioListeners. Keeping only the one on Main Camera.");
        }
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
