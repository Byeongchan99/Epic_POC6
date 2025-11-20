using UnityEngine;

public abstract class MissionBase : MonoBehaviour
{
    [Header("Mission Info")]
    [SerializeField] protected string missionName;
    [SerializeField] protected string missionDescription;

    protected bool isCompleted = false;
    protected bool isActive = false;

    public System.Action<MissionBase> OnMissionComplete;

    public virtual void Initialize()
    {
        isActive = true;
        Debug.Log($"Mission initialized: {missionName}");
    }

    public virtual void UpdateMission()
    {
        // Override in derived classes
    }

    protected virtual void CompleteMission()
    {
        if (isCompleted)
            return;

        isCompleted = true;
        isActive = false;

        Debug.Log($"Mission completed: {missionName}");

        // Update UI checkbox
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateMissionStatus(missionName, true);
        }

        OnMissionComplete?.Invoke(this);
    }

    public bool IsCompleted() => isCompleted;
    public bool IsActive() => isActive;
    public string GetMissionName() => missionName;
    public string GetMissionDescription() => missionDescription;
}
