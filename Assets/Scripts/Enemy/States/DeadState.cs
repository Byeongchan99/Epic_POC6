using UnityEngine;
using UnityEngine.AI;

public class DeadState : IEnemyState
{
    private EnemyAI enemy;
    private NavMeshAgent agent;
    private float deathTimer = 2f; // Time before destroying/disabling

    public DeadState(EnemyAI enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log("Enemy entering Dead state");

        agent = enemy.GetAgent();

        // Stop movement
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Disable gun
        Gun gun = enemy.GetGun();
        if (gun != null)
        {
            gun.enabled = false;
        }

        // TODO: Play death animation
    }

    public void Execute()
    {
        deathTimer -= Time.deltaTime;

        if (deathTimer <= 0)
        {
            // Destroy or disable the enemy
            GameObject.Destroy(enemy.gameObject);
        }
    }

    public void Exit()
    {
        // Dead state is final, no exit
    }
}
