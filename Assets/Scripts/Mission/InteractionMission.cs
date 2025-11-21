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

    [Header("Key Sequence Settings")]
    [SerializeField] private Difficulty difficulty = Difficulty.Easy;
    [SerializeField] private KeyCode[] availableKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D }; // Keys to use for random generation

    private Transform player;
    private PlayerController playerController;
    private bool minigameStarted = false;
    private float minigameProgress = 0f;

    // Key sequence minigame state
    private KeyCode[] generatedSequence; // Randomly generated sequence
    private int currentKeyIndex = 0;

    // Minimap marker reference
    private GameObject minimapMarker;

    public enum Difficulty
    {
        Easy = 4,      // 4 keys
        Medium = 6,    // 6 keys
        Hard = 8       // 8 keys
    }

    public enum MinigameType
    {
        MouseHold,      // Hold mouse in a circle for X seconds
        KeySequence     // Press keys in correct order
    }

    public override void Initialize()
    {
        base.Initialize();

        // Find player
        playerController = FindAnyObjectByType<PlayerController>();
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

        // Disable player controls
        if (playerController != null)
        {
            playerController.SetControlsEnabled(false);
            Debug.Log("Player controls disabled during minigame");
        }

        // Generate random key sequence for key sequence minigame
        if (minigameType == MinigameType.KeySequence)
        {
            GenerateRandomKeySequence();
        }

        // Show minigame UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMinigamePanel(true);

            // If key sequence minigame, show the required keys
            if (minigameType == MinigameType.KeySequence && generatedSequence != null && generatedSequence.Length > 0)
            {
                string sequenceText = "Press: ";
                foreach (KeyCode key in generatedSequence)
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

    private void GenerateRandomKeySequence()
    {
        if (availableKeys == null || availableKeys.Length == 0)
        {
            Debug.LogError("No available keys for random sequence generation!");
            return;
        }

        int sequenceLength = (int)difficulty;
        generatedSequence = new KeyCode[sequenceLength];

        for (int i = 0; i < sequenceLength; i++)
        {
            generatedSequence[i] = availableKeys[Random.Range(0, availableKeys.Length)];
        }

        Debug.Log($"Generated sequence ({difficulty}): {string.Join(", ", generatedSequence)}");
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

        // Re-enable player controls
        if (playerController != null)
        {
            playerController.SetControlsEnabled(true);
            Debug.Log("Player controls re-enabled after minigame success");
        }

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
        if (generatedSequence == null || generatedSequence.Length == 0)
        {
            Debug.LogError("No generated sequence available!");
            FailMinigame();
            return;
        }

        // Check if player presses the correct key
        if (currentKeyIndex < generatedSequence.Length)
        {
            KeyCode expectedKey = generatedSequence[currentKeyIndex];

            if (Input.GetKeyDown(expectedKey))
            {
                currentKeyIndex++;
                Debug.Log($"Correct key! Progress: {currentKeyIndex}/{generatedSequence.Length}");

                // Update UI to show progress
                if (UIManager.Instance != null)
                {
                    string sequenceText = "Press: ";
                    for (int i = 0; i < generatedSequence.Length; i++)
                    {
                        if (i < currentKeyIndex)
                        {
                            sequenceText += $"<color=green>{generatedSequence[i]}</color> ";
                        }
                        else
                        {
                            sequenceText += generatedSequence[i].ToString() + " ";
                        }
                    }
                    UIManager.Instance.UpdateMinigameText(sequenceText);
                    UIManager.Instance.UpdateMinigameProgress((float)currentKeyIndex / generatedSequence.Length);
                }

                // Check if sequence is complete
                if (currentKeyIndex >= generatedSequence.Length)
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

        // Re-enable player controls
        if (playerController != null)
        {
            playerController.SetControlsEnabled(true);
            Debug.Log("Player controls re-enabled after minigame failure");
        }

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
