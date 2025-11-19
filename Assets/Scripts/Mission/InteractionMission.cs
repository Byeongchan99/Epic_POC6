using UnityEngine;

public class InteractionMission : MissionBase
{
    [Header("Interaction Mission")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private MinigameType minigameType = MinigameType.MouseHold;

    [Header("Minigame Settings")]
    [SerializeField] private float requiredHoldTime = 3f; // For mouse hold minigame

    private Transform player;
    private bool minigameStarted = false;
    private float minigameProgress = 0f;

    public enum MinigameType
    {
        MouseHold,      // Hold mouse in a circle for X seconds
        // Can add more types later
    }

    public override void Initialize()
    {
        base.Initialize();

        // Find player
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }

        Debug.Log("Interaction mission initialized");
    }

    public override void UpdateMission()
    {
        if (player == null)
            return;

        if (!minigameStarted)
        {
            // Check if player is near interaction point
            if (IsPlayerNearPoint())
            {
                // Show prompt
                if (Input.GetKeyDown(KeyCode.F))
                {
                    StartMinigame();
                }
            }
        }
        else
        {
            UpdateMinigame();
        }
    }

    private bool IsPlayerNearPoint()
    {
        if (interactionPoint == null || player == null)
            return false;

        float distance = Vector3.Distance(player.position, interactionPoint.position);
        return distance <= interactionRange;
    }

    private void StartMinigame()
    {
        minigameStarted = true;
        minigameProgress = 0f;
        Debug.Log("Minigame started!");

        // TODO: Show minigame UI
    }

    private void UpdateMinigame()
    {
        switch (minigameType)
        {
            case MinigameType.MouseHold:
                UpdateMouseHoldMinigame();
                break;
        }
    }

    private void UpdateMouseHoldMinigame()
    {
        // Simple implementation: Hold left mouse button to increase progress
        if (Input.GetMouseButton(0))
        {
            minigameProgress += Time.deltaTime;

            Debug.Log($"Minigame progress: {minigameProgress}/{requiredHoldTime}");

            if (minigameProgress >= requiredHoldTime)
            {
                SucceedMinigame();
            }
        }
        else
        {
            // Optional: Decrease progress if not holding
            minigameProgress -= Time.deltaTime * 0.5f;
            minigameProgress = Mathf.Max(minigameProgress, 0f);
        }

        // Can escape with Esc or move away
        if (Input.GetKeyDown(KeyCode.Escape) || !IsPlayerNearPoint())
        {
            FailMinigame();
        }
    }

    private void SucceedMinigame()
    {
        Debug.Log("Minigame succeeded!");
        minigameStarted = false;

        // TODO: Hide minigame UI

        CompleteMission();
    }

    private void FailMinigame()
    {
        Debug.Log("Minigame failed. Try again!");
        minigameStarted = false;
        minigameProgress = 0f;

        // TODO: Hide minigame UI
        // Player can retry by pressing F again
    }

    private void OnDrawGizmos()
    {
        // Draw interaction point
        if (interactionPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(interactionPoint.position, interactionRange);
        }
    }
}
