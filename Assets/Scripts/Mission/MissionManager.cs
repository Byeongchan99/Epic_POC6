using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [SerializeField] private List<MissionBase> allMissions = new List<MissionBase>();
    [SerializeField] private GameObject escapeZonePrefab;

    private int completedMissionCount = 0;
    private bool escapeZoneSpawned = false;

    public System.Action OnAllMissionsComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Wait for map generation to complete before finding missions
        // MapGenerator spawns mission zones in its Start(), so we need to wait
        Invoke(nameof(InitializeMissions), 0.5f);
    }

    private void InitializeMissions()
    {
        Debug.Log("MissionManager: Searching for missions in scene...");

        // Find all missions in the scene
        MissionBase[] foundMissions = FindObjectsByType<MissionBase>(FindObjectsSortMode.None);
        allMissions.AddRange(foundMissions);

        Debug.Log($"MissionManager: Found {foundMissions.Length} mission(s)");

        foreach (MissionBase mission in allMissions)
        {
            Debug.Log($"Initializing mission: {mission.GetMissionName()}");
            mission.Initialize();
            mission.OnMissionComplete += OnMissionCompleted;

            // Add to UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.AddMissionToList(mission.GetMissionName());
            }
        }

        Debug.Log($"MissionManager: Initialized {allMissions.Count} missions");

        // Refresh minimap to show mission zones
        MinimapController minimap = FindAnyObjectByType<MinimapController>();
        if (minimap != null)
        {
            minimap.RefreshMissionZones();
            Debug.Log("MissionManager: Minimap refreshed to show mission zones");
        }
    }

    private void Update()
    {
        // Update active missions
        foreach (MissionBase mission in allMissions)
        {
            if (mission.IsActive())
            {
                mission.UpdateMission();
            }
        }
    }

    private void OnMissionCompleted(MissionBase mission)
    {
        completedMissionCount++;

        Debug.Log($"Mission completed: {mission.GetMissionName()} ({completedMissionCount}/{allMissions.Count})");

        // Check if all missions are complete
        if (completedMissionCount >= allMissions.Count && !escapeZoneSpawned)
        {
            OnAllMissionsComplete?.Invoke();
            Debug.Log("All missions completed! Spawning escape zone...");
            SpawnEscapeZone();
        }
    }

    private void SpawnEscapeZone()
    {
        if (escapeZonePrefab == null)
        {
            Debug.LogError("Escape zone prefab not assigned!");
            return;
        }

        if (escapeZoneSpawned)
        {
            Debug.LogWarning("Escape zone already spawned!");
            return;
        }

        // Find MapGenerator to get a random land position
        MapGenerator mapGenerator = FindAnyObjectByType<MapGenerator>();
        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator not found! Cannot spawn escape zone.");
            return;
        }

        // Get random land position
        Vector3 spawnPosition = mapGenerator.GetRandomLandPosition();
        spawnPosition.y = 0f; // Ensure it's on the ground

        // Spawn escape zone
        GameObject escapeZone = Instantiate(escapeZonePrefab, spawnPosition, Quaternion.identity);
        escapeZoneSpawned = true;

        Debug.Log($"Escape zone spawned at {spawnPosition}");
    }

    public void RegisterMission(MissionBase mission)
    {
        if (!allMissions.Contains(mission))
        {
            allMissions.Add(mission);
            mission.OnMissionComplete += OnMissionCompleted;
        }
    }

    public bool AreAllMissionsComplete()
    {
        return completedMissionCount >= allMissions.Count;
    }

    public int GetTotalMissions() => allMissions.Count;
    public int GetCompletedMissions() => completedMissionCount;
}
