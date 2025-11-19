using UnityEngine;
using UnityEngine.AI;

public class ChaseState : IEnemyState
{
    private EnemyAI enemy;
    private NavMeshAgent agent;
    private Transform player;

    public ChaseState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log("Enemy entering Chase state");

        agent = enemy.GetAgent();
        player = enemy.GetPlayer();

        agent.isStopped = false;
    }

    public void Execute()
    {
        if (player == null)
        {
            // No player, return to patrol
            enemy.ChangeState(enemy.GetPatrolState());
            return;
        }

        float distToPlayer = enemy.GetDistanceToPlayer();

        // Check if player is in attack range
        if (distToPlayer <= enemy.GetAttackRange())
        {
            enemy.ChangeState(enemy.GetAttackState());
            return;
        }

        // Check if player escaped chase range
        if (distToPlayer > enemy.GetChaseRange())
        {
            // Return to patrol
            enemy.ChangeState(enemy.GetPatrolState());
            return;
        }

        // Chase player
        agent.SetDestination(player.position);
    }

    public void Exit()
    {
        Debug.Log("Enemy exiting Chase state");
    }
}
