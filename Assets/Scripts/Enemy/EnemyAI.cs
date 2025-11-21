using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection Ranges")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float chaseRange = 20f;
    [SerializeField] private float attackRange = 10f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("References")]
    [SerializeField] private Gun gun;
    [SerializeField] private Transform firePoint;

    // Components
    private NavMeshAgent agent;
    private EnemyStats stats;
    private Transform player;
    private PlayerController playerController;

    // States
    private IEnemyState currentState;
    private PatrolState patrolState;
    private ChaseState chaseState;
    private AttackState attackState;
    private DeadState deadState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();

        // Find player
        playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }

        // Initialize states
        patrolState = new PatrolState(this);
        chaseState = new ChaseState(this);
        attackState = new AttackState(this);
        deadState = new DeadState(this);
    }

    private void Start()
    {
        // Set agent speed
        if (agent != null)
        {
            agent.speed = stats.GetMoveSpeed();
        }

        // Set gun owner tag
        if (gun != null)
        {
            gun.SetOwnerTag("Enemy");
            gun.SetFirePoint(firePoint);
        }

        // Subscribe to death event
        stats.OnDeath += OnDeath;

        // Subscribe to damage event
        stats.OnDamaged += OnDamaged;

        // Start in patrol state
        ChangeState(patrolState);
    }

    private void Update()
    {
        if (currentState != null)
        {
            currentState.Execute();
        }
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    private void OnDeath()
    {
        ChangeState(deadState);
    }

    private void OnDamaged()
    {
        // Only switch to chase if not already dead or attacking
        if (currentState != deadState && currentState != attackState)
        {
            ChangeState(chaseState);
            Debug.Log("Enemy damaged! Switching to chase state.");
        }
    }

    // State getters
    public IEnemyState GetPatrolState() => patrolState;
    public IEnemyState GetChaseState() => chaseState;
    public IEnemyState GetAttackState() => attackState;
    public IEnemyState GetDeadState() => deadState;

    // Public accessors for states
    public NavMeshAgent GetAgent() => agent;
    public EnemyStats GetStats() => stats;
    public Transform GetPlayer() => player;
    public Gun GetGun() => gun;
    public Transform GetFirePoint() => firePoint;

    public float GetDetectionRange() => detectionRange;
    public float GetChaseRange() => chaseRange;
    public float GetAttackRange() => attackRange;

    public Transform[] GetPatrolPoints() => patrolPoints;
    public int GetCurrentPatrolIndex() => currentPatrolIndex;
    public void SetCurrentPatrolIndex(int index) => currentPatrolIndex = index;

    public float GetDistanceToPlayer()
    {
        if (player == null)
            return float.MaxValue;

        // If player is in vehicle, target the vehicle instead
        if (playerController != null && playerController.IsInVehicle())
        {
            Vehicle vehicle = playerController.GetCurrentVehicle();
            if (vehicle != null)
            {
                return Vector3.Distance(transform.position, vehicle.transform.position);
            }
        }

        return Vector3.Distance(transform.position, player.position);
    }

    public Vector3 GetDirectionToPlayer()
    {
        if (player == null)
            return transform.forward;

        Vector3 targetPosition;

        // If player is in vehicle, aim at the vehicle
        if (playerController != null && playerController.IsInVehicle())
        {
            Vehicle vehicle = playerController.GetCurrentVehicle();
            if (vehicle != null)
            {
                targetPosition = vehicle.transform.position;
            }
            else
            {
                targetPosition = player.position;
            }
        }
        else
        {
            targetPosition = player.position;
        }

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        return direction.normalized;
    }

    public Transform GetTargetTransform()
    {
        if (player == null)
            return null;

        // If player is in vehicle, return vehicle transform
        if (playerController != null && playerController.IsInVehicle())
        {
            Vehicle vehicle = playerController.GetCurrentVehicle();
            if (vehicle != null)
            {
                return vehicle.transform;
            }
        }

        return player;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.647f, 0f); // Orange
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
