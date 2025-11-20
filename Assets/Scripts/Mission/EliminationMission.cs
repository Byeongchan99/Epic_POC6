using UnityEngine;
using System.Collections.Generic;

public class EliminationMission : MissionBase
{
    [Header("Elimination Mission")]
    [SerializeField] private List<EnemyAI> enemies = new List<EnemyAI>();

    [Header("Minimap Marker")]
    [SerializeField] private GameObject enemyMarkerPrefab; // Optional: Custom marker for enemies on minimap

    public override void Initialize()
    {
        base.Initialize();

        // Find all enemies in the mission area
        if (enemies.Count == 0)
        {
            // Auto-find enemies in children
            EnemyAI[] foundEnemies = GetComponentsInChildren<EnemyAI>();
            enemies.AddRange(foundEnemies);
        }

        // Subscribe to death events and add minimap markers
        MinimapController minimap = FindAnyObjectByType<MinimapController>();
        foreach (EnemyAI enemy in enemies)
        {
            EnemyStats stats = enemy.GetStats();
            if (stats != null)
            {
                stats.OnDeath += OnEnemyDied;
            }

            // Add enemy marker to minimap (optional)
            if (minimap != null && enemyMarkerPrefab != null)
            {
                minimap.AddMissionMarker(enemy.transform.position, enemyMarkerPrefab);
            }
        }

        Debug.Log($"Elimination mission initialized with {enemies.Count} enemies");
    }

    private void OnEnemyDied()
    {
        // Check completion after a short delay to ensure enemy is fully dead
        Invoke(nameof(CheckCompletion), 0.1f);
    }

    public override void UpdateMission()
    {
        // No continuous update needed for elimination mission
    }

    private void CheckCompletion()
    {
        // Remove dead enemies from list
        enemies.RemoveAll(e => e == null || e.GetStats().IsDead());

        Debug.Log($"Elimination mission: {enemies.Count} enemies remaining");

        if (enemies.Count == 0)
        {
            CompleteMission();
        }
    }

    public int GetRemainingEnemies() => enemies.Count;
}
