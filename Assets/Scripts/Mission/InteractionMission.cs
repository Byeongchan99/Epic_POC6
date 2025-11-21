using UnityEngine;

public class InteractionMission : MissionBase
{
    [Header("Interaction Mission")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private MinigameType minigameType = MinigameType.MouseHold;

    [Header("Minimap Marker")]
    [SerializeField] private GameObject interactionMarkerPrefab; // Optional: Custom marker for interaction point on minimap

    [Header("Minigame Settings")]
    [SerializeField] private float requiredHoldTime = 3f; // For mouse hold minigame
    [SerializeField] private KeyCode[] keySequence = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D }; // For key sequence minigame

    private Transform player;
    private bool minigameStarted = false;
    private float minigameProgress = 0f;

    // Key sequence minigame state
    private int currentKeyIndex = 0;

    // Minimap marker reference
    private GameObject minimapMarker;

    public enum MinigameType
    {
        MouseHold,      // Hold mouse in a circle for X seconds
        KeySequence     // Press keys in correct order
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

        // Add minimap marker for interaction point
        if (interactionPoint != null)
        {
            MinimapController minimap = FindAnyObjectByType<MinimapController>();
            if (minimap != null && interactionMarkerPrefab != null)
            {
                minimapMarker = minimap.AddMissionMarker(interactionPoint.position, interactionMarkerPrefab);
                Debug.Log("Interaction mission marker added to minimap");
            }
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
                // Show interaction prompt
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowInteractionPrompt(true, "Press F to interact");
                }

                // Start minigame when F is pressed
                if (Input.GetKeyDown(KeyCode.F))
                {
                    StartMinigame();

                    // Hide interaction prompt
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowInteractionPrompt(false, "");
                    }
                }
            }
            else
            {
                // Hide interaction prompt when player moves away
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowInteractionPrompt(false, "");
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
        currentKeyIndex = 0; // Reset key sequence progress
        Debug.Log("Minigame started!");

        // Show minigame UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMinigamePanel(true);

            // If key sequence minigame, show the required keys
            if (minigameType == MinigameType.KeySequence && keySequence.Length > 0)
            {
                string sequenceText = "Press: ";
                foreach (KeyCode key in keySequence)
                {
                    sequenceText += key.ToString() + " ";
                }
                UIManager.Instance.UpdateMinigameText(sequenceText);
            }
            else if (minigameType == MinigameType.MouseHold)
            {
                UIManager.Instance.UpdateMinigameText("Hold Left Mouse Button!");
            }
        }
    }

    private void UpdateMinigame()
    {
        switch (minigameType)
        {
            case MinigameType.MouseHold:
                UpdateMouseHoldMinigame();
                break;
            case MinigameType.KeySequence:
                UpdateKeySequenceMinigame();
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

        // Hide minigame UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMinigamePanel(false);
        }

        // Remove minimap marker
        if (minimapMarker != null)
        {
            MinimapController minimap = FindAnyObjectByType<MinimapController>();
            if (minimap != null)
            {
                minimap.RemoveMissionMarker(minimapMarker);
                minimapMarker = null;
                Debug.Log("Interaction mission marker removed from minimap");
            }
        }

        CompleteMission();
    }

    private void UpdateKeySequenceMinigame()
    {
        // Check if player presses the correct key
        if (currentKeyIndex < keySequence.Length)
        {
            KeyCode expectedKey = keySequence[currentKeyIndex];

            if (Input.GetKeyDown(expectedKey))
            {
                currentKeyIndex++;
                Debug.Log($"Correct key! Progress: {currentKeyIndex}/{keySequence.Length}");

                // Update UI to show progress
                if (UIManager.Instance != null)
                {
                    string sequenceText = "Press: ";
                    for (int i = 0; i < keySequence.Length; i++)
                    {
                        if (i < currentKeyIndex)
                        {
                            sequenceText += $"<color=green>{keySequence[i]}</color> ";
                        }
                        else
                        {
                            sequenceText += keySequence[i].ToString() + " ";
                        }
                    }
                    UIManager.Instance.UpdateMinigameText(sequenceText);
                    UIManager.Instance.UpdateMinigameProgress((float)currentKeyIndex / keySequence.Length);
                }

                // Check if sequence is complete
                if (currentKeyIndex >= keySequence.Length)
                {
                    SucceedMinigame();
                }
            }
            else if (Input.anyKeyDown)
            {
                // Wrong key pressed - fail the minigame
                Debug.Log("Wrong key! Try again.");
                FailMinigame();
            }
        }

        // Can escape with Esc or move away
        if (Input.GetKeyDown(KeyCode.Escape) || !IsPlayerNearPoint())
        {
            FailMinigame();
        }
    }

    private void FailMinigame()
    {
        Debug.Log("Minigame failed. Try again!");
        minigameStarted = false;
        minigameProgress = 0f;
        currentKeyIndex = 0;

        // Hide minigame UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMinigamePanel(false);
        }

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
