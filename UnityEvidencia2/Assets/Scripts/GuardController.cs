// GuardController.cs
using UnityEngine;
using UnityEngine.AI;

public class GuardController : MonoBehaviour
{
    [Header("Guard Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float catchDistance = 2f;
    [SerializeField] private float rotationSpeed = 100f;
    
    private NavMeshAgent agent;
    private Vector3 targetPosition;
    private bool isMovingToTarget = false;
    private Animator animator; // If you have animations
    private GameController gameController;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        animator = GetComponent<Animator>();
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
        
        // Configure NavMeshAgent
        agent.speed = moveSpeed;
        agent.stoppingDistance = catchDistance;
        agent.angularSpeed = rotationSpeed;
    }

    void Update()
    {
        if (isMovingToTarget)
        {
            if (Vector3.Distance(transform.position, targetPosition) <= catchDistance)
            {
                isMovingToTarget = false;
                OnScavengerCaught();
            }
            
            // Update animation if available
            if (animator != null)
            {
                animator.SetBool("IsMoving", agent.velocity.magnitude > 0.1f);
            }
        }
    }

    public void MoveToScavenger(Vector3 scavengerPosition)
    {
        targetPosition = scavengerPosition;
        isMovingToTarget = true;
        agent.SetDestination(scavengerPosition);
    }

    public void MoveToAlert(Vector3 alertPosition)
    {
        targetPosition = alertPosition;
        isMovingToTarget = true;
        Debug.Log("Guard moving to alert position.");
    }

    private void OnScavengerCaught()
    {
        Debug.Log("Guard resolved the alert.");
        gameController.EndSimulation();
    }
}