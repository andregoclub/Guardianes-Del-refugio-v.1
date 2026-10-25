using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Velocidades")]

    [SerializeField] private float velocidadCaminar = 1f;
    [SerializeField] private float velocidadCorrer = 2f;
    [SerializeField] private float velocidadPatrulla = 0.8f;   // NUEVO


    [Header("Distancias")]

    [SerializeField] private float distanciaSeguimiento = 1f;
    [SerializeField] private float distanciaCorrer = 3f;
    [SerializeField] private float distanciaLlegadaPatrulla = 0.3f;   // NUEVO: qué tan cerca debe llegar a un punto de la ruta


    [Header("Referencias")]

    [SerializeField] private Animator anim;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyAttack enemyAttack;
    [SerializeField] private EnemyKnockback enemyKnockback;


    private bool avisoNavMeshMostrado;


    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyAttack == null)
            enemyAttack = GetComponent<EnemyAttack>();

        if (enemyKnockback == null)
            enemyKnockback = GetComponent<EnemyKnockback>();

        agent.stoppingDistance = distanciaSeguimiento;
    }

    public void Perseguir(Transform objetivo)
    {
        if (enemyHealth != null && enemyHealth.EstaMuerto)
        {
            Detener();
            return;
        }

        if (enemyAttack != null && enemyAttack.EstaAtacando)
        {
            Detener();
            return;
        }

        // Mientras dura el knockback, el propio EnemyKnockback controla el movimiento.
        if (enemyKnockback != null && enemyKnockback.EnKnockback)
        {
            return;
        }

        if (objetivo == null)
            return;

        if (!AgenteListo())
            return;

        float distancia = Vector3.Distance(transform.position, objetivo.position);

        // NUEVO: la patrulla cambia esta distancia, así que se restaura al perseguir
        agent.stoppingDistance = distanciaSeguimiento;

        agent.isStopped = false;
        agent.SetDestination(objetivo.position);

        if (distancia > distanciaCorrer)
        {
            agent.speed = velocidadCorrer;
            anim.SetFloat("Velocidad", 1f);
        }
        else
        {
            agent.speed = velocidadCaminar;
            anim.SetFloat("Velocidad", 0.5f);
        }
    }

    /// <summary>
    /// NUEVO: camina hacia un punto de patrulla, más lento que al perseguir.
    /// Devuelve true si la orden fue aceptada (false si está muerto, atacando o en knockback).
    /// </summary>
    public bool IrA(Vector3 destino)
    {
        if (enemyHealth != null && enemyHealth.EstaMuerto)
        {
            Detener();
            return false;
        }

        if (enemyAttack != null && enemyAttack.EstaAtacando)
        {
            Detener();
            return false;
        }

        // Mientras dura el knockback, el propio EnemyKnockback controla el movimiento.
        if (enemyKnockback != null && enemyKnockback.EnKnockback)
        {
            return false;
        }

        if (!AgenteListo())
            return false;

        agent.stoppingDistance = distanciaLlegadaPatrulla;
        agent.speed = velocidadPatrulla;
        agent.isStopped = false;
        agent.SetDestination(destino);

        anim.SetFloat("Velocidad", 0.5f);
        return true;
    }

    public void Detener()
    {
        // No pisamos el knockback si está en curso.
        if (enemyKnockback != null && enemyKnockback.EnKnockback)
            return;

        anim.SetFloat("Velocidad", 0f);

        if (!AgenteListo())
            return;

        agent.isStopped = true;
    }

    public bool HaLlegado()
    {
        return AgenteListo() &&
               !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance;
    }

    /// <summary>
    /// NUEVO: el agente solo puede moverse si está activo y apoyado sobre un NavMesh.
    /// Si no lo está, se avisa una sola vez en la consola (el aviso se puede clickear para ubicar al enemigo).
    /// </summary>
    private bool AgenteListo()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            return true;

        if (!avisoNavMeshMostrado)
        {
            avisoNavMeshMostrado = true;
            Debug.LogWarning($"{name}: el NavMeshAgent no está sobre un NavMesh, así que este enemigo no se mueve. " +
                             "Revisá que el NavMesh esté horneado debajo de él.", this);
        }

        return false;
    }
}