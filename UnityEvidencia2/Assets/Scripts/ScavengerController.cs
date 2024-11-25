using UnityEngine;
using UnityEngine.AI;

public class ScavengerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float wanderRadius = 20f; // Radius within which the scavenger can move

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        // Configure NavMeshAgent
        agent.speed = 3.5f;
        agent.acceleration = 12.0f;
        agent.angularSpeed = 360f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; // Smooth obstacle avoidance
        agent.autoRepath = true; // Automatically recalculate paths if blocked

        Wander(); // Start wandering immediately
    }

    void Update()
    {
        // Check if the agent has reached its current destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Wander(); // Pick a new random destination
        }
    }

    private void Wander()
    {
        // Generate a random direction within the wander radius
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y; // Keep movement on the same horizontal plane

        // Check if the random position is valid and reachable
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position); // Move to the random valid position
        }
        else
        {
            Wander(); // Retry if no valid position was found
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the wander radius in the editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}
