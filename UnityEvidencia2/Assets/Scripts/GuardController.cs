// GuardController.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class GuardController : MonoBehaviour
{
    [Header("Guard Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float catchDistance = 2f;
    [SerializeField] private float controlStationStayDuration = 10f;
    //[SerializeField] private float rotationSpeed = 100f;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private bool isMovingToTarget = false;
    private Animator animator; 
    private GameController gameController;
    private List<Vector3> patrolRoute;

    private Vector3 controlStationPosition;
    private bool isAtControlStation = false;
    private float controlStationTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        gameController = FindObjectOfType<GameController>();

        patrolRoute = new List<Vector3>
        {
            new Vector3(21, 0, 3)
        };

        controlStationPosition = new Vector3(14f, 0f, 1f);

        MoveToNextWaypoint();
    }

    void Update()
    {
        if (isMovingToTarget)
        {
            agent.SetDestination(agent.destination);
            
            // Check if reached the target (could be scavenger or control station)
            if (Vector3.Distance(transform.position, agent.destination) <= catchDistance)
            {
                isMovingToTarget = false;
                
                // If at control station, start staying timer
                if (IsAtControlStation())
                {
                    StartCoroutine(StayAtControlStation());
                }
                else
                {
                    Debug.Log("Guard caught a scavenger!");
                    OnScavengerCaught();
                }
            }
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                MoveToNextWaypoint();
            }
        }

        // Update animation if available
        if (animator != null)
        {
            animator.SetBool("IsMoving", agent.velocity.magnitude > 0.1f);
        }
    }

    // New method to move to control station
    public void MoveToControlStation()
    {
        agent.SetDestination(controlStationPosition);
        isMovingToTarget = true;
        Debug.Log("Guard moving to control station.");
    }

    // Check if guard is at control station
    private bool IsAtControlStation()
    {
        return Vector3.Distance(transform.position, controlStationPosition) <= catchDistance;
    }

    // Coroutine to stay at control station for specified duration
    private IEnumerator StayAtControlStation()
    {
        isAtControlStation = true;
        Debug.Log("Guard arrived at control station.");

        // Wait for specified number of frames
        for (float timer = 0; timer < controlStationStayDuration; timer += Time.deltaTime)
        {
            yield return null;
        }

        isAtControlStation = false;
        Debug.Log("Guard finished staying at control station.");

        // Resume patrol or other previous activities
        MoveToNextWaypoint();
    }

    private void MoveToNextWaypoint()
    {
        if (patrolRoute.Count == 0)
            return;

        agent.SetDestination(patrolRoute[currentWaypointIndex]);
        currentWaypointIndex = (currentWaypointIndex + 1) % patrolRoute.Count;
        //Debug.Log("Guard moving to waypoint: " + currentWaypointIndex);
    }

    public void MoveToAlert(Vector3 alertPosition)
    {
        agent.SetDestination(alertPosition);
        isMovingToTarget = true;
        Debug.Log("Guard moving to alert position.");
    }

    private void OnScavengerCaught()
    {
        Debug.Log("Guard has seized the scavenger");
        gameController.EndSimulation();
    }

    public void SetTargetPosition(Vector3 position)
    {
        agent.SetDestination(position);
        isMovingToTarget = true;
    }
}