// NPCController.cs
using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float wanderTimer = 7f;
    
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
        agent.speed = 2.5f;
        agent.acceleration = 6.0f;
        agent.angularSpeed = 90f;
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
