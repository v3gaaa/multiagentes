using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float waypointReachThreshold = 0.5f;
    [SerializeField] private float waitTimeAtWaypoint = 5f;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    [SerializeField] private List<Vector3> patrolRoute;
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        agent.speed = moveSpeed;
        agent.acceleration = 6.0f;
        agent.angularSpeed = 120f;

        // Define patrol route
        patrolRoute = new List<Vector3>
        {
            new Vector3(8, 7, 9), new Vector3(16, 7, 9),
            new Vector3(15, 7, 15), new Vector3(8, 7, 15),
            new Vector3(7, 7, 20), new Vector3(16, 7, 21),
        };

        MoveToNextWaypoint();
    }

    void Update()
    {
        if (!isWaiting && !agent.pathPending && agent.remainingDistance < waypointReachThreshold)
        {
            StartCoroutine(WaitAtWaypoint());
        }
    }

    private IEnumerator WaitAtWaypoint()
    {
        isWaiting = true;
        //Debug.Log("Waiting at waypoint...");
        yield return new WaitForSeconds(waitTimeAtWaypoint);
        isWaiting = false;
        MoveToNextWaypoint();
    }

    private void MoveToNextWaypoint()
    {
        if (patrolRoute.Count == 0)
            return;

        agent.SetDestination(patrolRoute[currentWaypointIndex]);
        currentWaypointIndex = (currentWaypointIndex + 1) % patrolRoute.Count;
        //Debug.Log("NPC moving to waypoint: " + currentWaypointIndex);
    }
}
