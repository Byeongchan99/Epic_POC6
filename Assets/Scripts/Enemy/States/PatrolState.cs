using UnityEngine;
using UnityEngine.AI;

public class PatrolState : IEnemyState
{
    private EnemyAI enemy;
    private NavMeshAgent agent;
    private Transform[] patrolPoints;
    private int currentIndex;

    public PatrolState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log("Enemy entering Patrol state");

        agent = enemy.GetAgent();
        patrolPoints = enemy.GetPatrolPoints();
        currentIndex = enemy.GetCurrentPatrolIndex();

        // Move to current patrol point
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            MoveToCurrentPatrolPoint();
        }
    }

    public void Execute()
    {
        // Check for player detection
        float distToPlayer = enemy.GetDistanceToPlayer();

        if (distToPlayer <= enemy.GetDetectionRange())
        {
            // Player detected, switch to chase
            enemy.ChangeState(enemy.GetChaseState());
            return;
        }

        // Patrol logic
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            // No patrol points, just stay in place
            return;
        }

        // Check if reached current patrol point
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                // Reached patrol point, move to next
                if (patrolPoints.Length > 1)
                {
                    currentIndex = (currentIndex + 1) % patrolPoints.Length;
                    enemy.SetCurrentPatrolIndex(currentIndex);
                    MoveToCurrentPatrolPoint();
                }
            }
        }
    }

    public void Exit()
    {
        Debug.Log("Enemy exiting Patrol state");
    }

    private void MoveToCurrentPatrolPoint()
    {
        if (patrolPoints[currentIndex] != null && agent != null)
        {
            agent.SetDestination(patrolPoints[currentIndex].position);
            agent.isStopped = false;
        }
    }
}
