using UnityEngine;
using UnityEngine.AI;

public class AttackState : IEnemyState
{
    private EnemyAI enemy;
    private NavMeshAgent agent;
    private Transform player;
    private Gun gun;

    public AttackState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log("Enemy entering Attack state");

        agent = enemy.GetAgent();
        player = enemy.GetPlayer();
        gun = enemy.GetGun();

        // Stop moving
        agent.isStopped = true;
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

        // Check if player left attack range
        if (distToPlayer > enemy.GetAttackRange())
        {
            // Return to chase
            enemy.ChangeState(enemy.GetChaseState());
            return;
        }

        // Rotate towards player
        Vector3 direction = enemy.GetDirectionToPlayer();
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                targetRotation,
                Time.deltaTime * 5f
            );
        }

        // Fire at player
        if (gun != null)
        {
            gun.Fire(direction);
        }
    }

    public void Exit()
    {
        Debug.Log("Enemy exiting Attack state");
        agent.isStopped = false;
    }
}
