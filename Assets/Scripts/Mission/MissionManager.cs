using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [SerializeField] private List<MissionBase> allMissions = new List<MissionBase>();

    private int completedMissionCount = 0;

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
        InitializeMissions();
    }

    private void InitializeMissions()
    {
        // Find all missions in the scene
        MissionBase[] foundMissions = FindObjectsOfType<MissionBase>();
        allMissions.AddRange(foundMissions);

        foreach (MissionBase mission in allMissions)
        {
            mission.Initialize();
            mission.OnMissionComplete += OnMissionCompleted;

            // Add to UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.AddMissionToList(mission.GetMissionName());
            }
        }

        Debug.Log($"Initialized {allMissions.Count} missions");
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
        if (completedMissionCount >= allMissions.Count)
        {
            OnAllMissionsComplete?.Invoke();
            Debug.Log("All missions completed! Player can now escape!");
        }
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
