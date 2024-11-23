using UnityEngine;
using UnityEngine.AI;

public class ScavengerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float wanderRadius = 20f;
    [SerializeField] private float wanderTimer = 5f;
    
    private NavMeshAgent agent;
    private float timer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        timer = wanderTimer;
        
        // Configure NavMeshAgent
        agent.speed = 3.5f;
        agent.acceleration = 8.0f;
        agent.angularSpeed = 120f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= wanderTimer)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
            {
                agent.SetDestination(hit.position);
            }
            timer = 0;
        }
    }
}