// GuardController.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class GuardController : MonoBehaviour
{
    [Header("Guard Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float catchDistance = 2f;
    //[SerializeField] private float rotationSpeed = 100f;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private bool isMovingToTarget = false;
    private Animator animator; // If you have animations
    private GameController gameController;
    private List<Vector3> patrolRoute;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        gameController = FindObjectOfType<GameController>();

        patrolRoute = new List<Vector3>
        {
            new Vector3(4, 7, 3), new Vector3(14, 7, 3), new Vector3(21, 7, 3),
            new Vector3(21, 7, 9), new Vector3(14, 7, 9), new Vector3(4, 7, 9),
            new Vector3(4, 7, 15), new Vector3(14, 7, 15), new Vector3(21, 7, 15),
            new Vector3(21, 7, 21), new Vector3(14, 7, 21), new Vector3(4, 7, 21),
            new Vector3(4, 7, 27), new Vector3(14, 7, 27), new Vector3(21, 7, 27),
            new Vector3(4, 7, 3), new Vector3(14, 7, 3), new Vector3(21, 7, 3),
            new Vector3(21, 7, 9), new Vector3(14, 7, 9), new Vector3(4, 7, 9),
            new Vector3(4, 7, 15), new Vector3(14, 7, 15), new Vector3(21, 7, 15),
            new Vector3(21, 7, 21), new Vector3(14, 7, 21), new Vector3(4, 7, 21),
            new Vector3(4, 7, 27), new Vector3(14, 7, 27), new Vector3(21, 7, 27),
            new Vector3(4, 7, 3), new Vector3(14, 7, 3), new Vector3(21, 7, 3),
            new Vector3(21, 7, 9), new Vector3(14, 7, 9), new Vector3(4, 7, 9),
            new Vector3(4, 7, 15), new Vector3(14, 7, 15), new Vector3(21, 7, 15),
            new Vector3(21, 7, 21), new Vector3(14, 7, 21), new Vector3(4, 7, 21),
            new Vector3(4, 7, 27), new Vector3(14, 7, 27), new Vector3(21, 7, 27)
        };

        MoveToNextWaypoint();
    }

    void Update()
    {
        if (isMovingToTarget)
        {
            agent.SetDestination(agent.destination);
            if (Vector3.Distance(transform.position, agent.destination) <= catchDistance)
            {
                isMovingToTarget = false;
                Debug.Log("Guard caught a scavenger!");
                OnScavengerCaught();
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