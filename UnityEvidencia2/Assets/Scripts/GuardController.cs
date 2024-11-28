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
    private bool isMovingToTarget = false;
    private Animator animator; 
    private GameController gameController;

    private Vector3 controlStationPosition;
    private bool isAtControlStation = false;
    private float controlStationTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        gameController = FindObjectOfType<GameController>();

        // Comenta o elimina estas líneas
        //patrolRoute = new List<Vector3>
        //{
        //    new Vector3(21, 0, 3)
        //};

        controlStationPosition = new Vector3(14f, 0f, 1f);

        // Comenta o elimina esta línea
        //MoveToNextWaypoint();
    }

    void Update()
    {
        if (isMovingToTarget)
        {
            agent.SetDestination(agent.destination);

            // Verifica si ha llegado al destino (puede ser un intruso o la estación de control)
            if (Vector3.Distance(transform.position, agent.destination) <= catchDistance)
            {
                isMovingToTarget = false;

                // Si está en la estación de control, inicia la rutina para quedarse allí
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
        // Elimina o comenta este bloque
        /*
        else
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                MoveToNextWaypoint();
            }
        }
        */

        // Actualiza la animación si está disponible
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

        // Elimina el temporizador si quieres que se quede allí para siempre
        // Si deseas que realice alguna acción, puedes mantenerlo o ajustarlo
        //yield return new WaitForSeconds(controlStationStayDuration);

        // Elimina o comenta estas líneas
        //isAtControlStation = false;
        //Debug.Log("Guard finished staying at control station.");
        //MoveToNextWaypoint();

        // El guardia se queda en la estación de control indefinidamente
        yield break;
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