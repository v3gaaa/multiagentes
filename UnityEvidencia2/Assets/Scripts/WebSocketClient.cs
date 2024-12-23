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

    private List<Detection> latestDetections = new List<Detection>();


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
        if (isDroneControlled || isInvestigating)
        {
            // Skip automated patrol if the drone is under manual control or investigating
            return;
        }

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

        // Adjust rotation
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
        guard.transform.position = new Vector3(21, 0, 3);
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
                    
                case "drone_camera_frame":
                    Debug.Log("[Unity] Drone Camera Frame Data: " + JsonUtility.ToJson(data));
                    lock (mainThreadQueue)
                    {
                        mainThreadQueue.Enqueue(() =>
                        {
                            UpdateDetections(data.detections);
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

    private void UpdateDetections(List<Detection> detections)
    {
        latestDetections = detections;
        Debug.Log($"[Unity] Updated detections: {JsonUtility.ToJson(detections)}");
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
                }
                else
                {
                    Debug.LogError($"Failed to capture image from camera {cameraId}");
                }
            }

            yield return new WaitForSeconds(0.5f); // Reduce the interval to 0.5 seconds
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

            yield return new WaitForSeconds(0.5f); // Reduce the interval to 0.5 seconds
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
        if (isDroneControlled)
        {
            Debug.Log("[Unity] Alert received during guard control. Triggering alarm system.");

                // Activa el sistema de alarmas
                HandleAlertSystem();

            
                lock (mainThreadQueue)
                {
                    mainThreadQueue.Enqueue(() =>
                    {
                        // Libera el control manual del dron
                        isDroneControlled = false;

                        // Envía el dron a la estación de aterrizaje
                        StopAllCoroutines();
                        StartCoroutine(MoveDrone(landingStation, () =>
                        {
                            Debug.Log("[Unity] Drone returned to landing station after alert.");
                        }));

                        // Notifica al servidor que el guardia ha terminado el control
                        if (ws.ReadyState == WebSocketState.Open)
                        {
                            var message = new Message
                            {
                                type = "guard_control",
                                status = "RELEASE_CONTROL"
                            };
                            string jsonMessage = JsonUtility.ToJson(message);
                            ws.Send(jsonMessage);
                        }
                    });
                }
            return;
        }

        Debug.Log($"[Unity] Drone Alert Received! Guard alerted.");
        lock (mainThreadQueue)
        {
            mainThreadQueue.Enqueue(() =>
            {
                StopAllCoroutines();
                isDroneControlled = true;

                if (ws.ReadyState == WebSocketState.Open)
                {
                    var message = new Message
                    {
                        type = "guard_control",
                        status = "TAKE_CONTROL",
                        position = alertPosition
                    };

                    string jsonMessage = JsonUtility.ToJson(message);
                    ws.Send(jsonMessage);
                }

                StartCoroutine(MoveGuard(controlStation, () =>
                {
                    Debug.Log("[Unity] Guard arrived at control station. Starting drone control...");
                    StartCoroutine(HandleDroneInvestigationLap(() =>
                    {
                        Debug.Log("[Unity] Guard finished drone control. Resuming patrol...");
                        currentWaypointIndex = 0;
                        isDroneControlled = false;
                    }));
                }));
            });
        }
    }



    private IEnumerator HandleDroneInvestigationLap(Action onArrival = null)
    {
        Vector3[] investigationRoute = new Vector3[]
        {
            new Vector3(4, 7, 3),
            new Vector3(14, 7, 15),
            new Vector3(21, 7, 27),
            new Vector3(8, 7, 20),
            new Vector3(16, 7, 10),
            new Vector3(10, 7, 5),
            new Vector3(18, 7, 25),
            new Vector3(6, 7, 18),
            new Vector3(12, 7, 12)
        };

        isInvestigating = true;
        Debug.Log("[Unity] Starting guard-controlled drone investigation lap.");

        bool scavengerDetected = false;

        while (!scavengerDetected)
        {
            for (int i = 0; i < investigationRoute.Length; i++)
            {
                Vector3 target = investigationRoute[i];
                Debug.Log($"[Unity] Moving to waypoint {i + 1}: {target}");
                yield return StartCoroutine(SmoothMoveDrone(target, () =>
                {
                    Debug.Log("[Unity] Drone reached waypoint. Capturing frame for analysis.");
                    byte[] droneImageBytes = CaptureDroneCameraFrame();
                    SendDroneCameraFrame(droneImageBytes);
                }));

                yield return new WaitForSeconds(1f); // Reduce the interval to 1 second

                // Verifica si hay detección después de cada punto
                if (CheckForDetection())
                {
                    Debug.Log("[Unity] Scavenger detected during guard investigation! Triggering alert system.");
                    
                    // Activa el sistema de alarmas
                    HandleAlertSystem();

                    // Notifica al servidor sobre la detección
                    if (ws.ReadyState == WebSocketState.Open)
                    {
                        var message = new Message
                        {
                            type = "drone_alert",
                            status = "DETECTED",
                            detection = latestDetections[0]
                        };
                        string jsonMessage = JsonUtility.ToJson(message);
                        ws.Send(jsonMessage);
                    }

                    // Cambia el estado para detener el bucle
                    scavengerDetected = true;
                    break;
                }
            }

            if (!scavengerDetected)
            {
                Debug.Log("[Unity] No scavenger detected. Restarting investigation route...");
            }
        }

        Debug.Log("[Unity] Investigation complete. Returning to landing station.");
        yield return StartCoroutine(SmoothMoveDrone(landingStation, () =>
        {
            Debug.Log("[Unity] Drone has returned to landing station.");
            Debug.Log("[Unity] Ending simulation.");
            Application.Quit();
        }));

        isInvestigating = false;

        // Notifica que el guardia ha liberado el control del dron
        if (ws.ReadyState == WebSocketState.Open)
        {
            var message = new Message
            {
                type = "guard_control",
                status = "RELEASE_CONTROL"
            };

            string jsonMessage = JsonUtility.ToJson(message);
            ws.Send(jsonMessage);
        }

        onArrival?.Invoke();
    }

    private IEnumerator SmoothMoveDrone(Vector3 target, Action onArrival)
    {
        Debug.Log($"Drone moving to target position: {target}");
        while (Vector3.Distance(drone.transform.position, target) > 0.1f)
        {
            float step = droneSpeed * Time.deltaTime;
            drone.transform.position = Vector3.MoveTowards(drone.transform.position, target, step);

            // Adjust rotation smoothly towards the center
            Vector3 center = new Vector3(12, drone.transform.position.y, 15);
            Vector3 direction = (center - drone.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
                drone.transform.rotation = Quaternion.RotateTowards(drone.transform.rotation, toRotation, step * 100);
            }

            yield return null;
        }
        Debug.Log("Drone reached the target position.");
        onArrival?.Invoke();
    }

    private bool CheckForDetection()
    {
        if (latestDetections != null && latestDetections.Count > 0)
        {
            foreach (var detection in latestDetections)
            {
                if (detection.className == "thiefs" && detection.confidence > 0.8f)
                {
                    Debug.Log("[Unity] High-confidence scavenger detection confirmed.");
                    return true;
                }
            }
        }

        Debug.Log("[Unity] No high-confidence detections found.");
        return false;
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
    public void HandleAlertSystem()
    {
        Debug.Log("Drone alert received - initiating warning lights");

        // Asegúrate de encontrar el GameController y activar la alarma.
        GameController gameController = FindObjectOfType<GameController>();
        if (gameController != null)
        {
            gameController.TriggerGeneralAlarm(true); // Activa la alarma como una amenaza real
        }
        else
        {
            Debug.LogError("GameController not found! Cannot trigger alarm.");
        }
    }


    private IEnumerator FlickerLightsRed(float duration, float interval)
    {
        //Debug.Log("First case");
        Light[] lights = FindObjectsOfType<Light>();
        float elapsedTime = 0f;
        bool lightsOn = true;
        //Debug.Log("Second case");
        // Store original light colors to restore later
        Dictionary<Light, Color> originalColors = new Dictionary<Light, Color>();
        //Debug.Log("Third case");
        foreach (Light light in lights)
        {
            originalColors[light] = light.color;
        }

        while (elapsedTime < duration)
        {
            foreach (Light light in lights)
            {
                light.enabled = lightsOn;
                light.color = Color.red;
            }
            lightsOn = !lightsOn;
            yield return new WaitForSeconds(interval);
            elapsedTime += interval;
        }

        // Reset lights to original state
        foreach (Light light in lights)
        {
            light.enabled = true;
            light.color = originalColors[light];
        }
    }

    [Serializable]
    private class Message
    {
        public string type; // Message type
        public int camera_id; // Camera ID for camera messages
        public string image; // Base64 encoded image string
        public Vector3 position; // Position data for alerts
        public string status; // Status (e.g., "ALERT" or "RELEASE_CONTROL")
        public string action; // Action performed (optional)
        public List<Detection> detections; // List of detections (for image analysis)
        public Detection detection; // Single detection for alerts
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

