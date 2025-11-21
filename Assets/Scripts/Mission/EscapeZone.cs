using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    [Header("Escape Settings")]
    [SerializeField] private float requiredStayTime = 3f;
    [SerializeField] private float escapeRadius = 5f;

    [Header("Visual")]
    [SerializeField] private Color gizmoColor = Color.cyan;

    [Header("Minimap Marker")]
    [SerializeField] private GameObject escapeMarkerPrefab;

    private Transform player;
    private PlayerController playerController;
    private float stayTimer = 0f;
    private bool playerInZone = false;
    private GameObject minimapMarker;

    private void Start()
    {
        // Find player
        playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }

        // Add minimap marker
        MinimapController minimap = FindAnyObjectByType<MinimapController>();
        if (minimap != null && escapeMarkerPrefab != null)
        {
            minimapMarker = minimap.AddMissionMarker(transform.position, escapeMarkerPrefab);
            Debug.Log("Escape zone marker added to minimap");
        }

        // Show notification to player
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("All missions complete! Head to the escape zone to escape!", 8f);
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        // Check if player is in escape zone
        float distance = Vector3.Distance(player.position, transform.position);
        playerInZone = distance <= escapeRadius;

        if (playerInZone)
        {
            stayTimer += Time.deltaTime;

            // Show timer UI (both notification and interaction prompt)
            if (UIManager.Instance != null)
            {
                int remainingSeconds = Mathf.CeilToInt(requiredStayTime - stayTimer);
                float progress = stayTimer / requiredStayTime;

                // Show large notification with timer
                UIManager.Instance.ShowNotification($"탈출 중... {remainingSeconds}초", 0.2f);

                // Also show interaction prompt
                UIManager.Instance.ShowInteractionPrompt(true, $"탈출 지역에서 대기 중 ({remainingSeconds}초)");
            }

            // Check if player stayed long enough
            if (stayTimer >= requiredStayTime)
            {
                TriggerEscape();
            }
        }
        else
        {
            // Reset timer if player leaves
            if (stayTimer > 0)
            {
                stayTimer = 0f;

                // Hide UI
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.HideNotification();
                    UIManager.Instance.ShowInteractionPrompt(false, "");
                }
            }
        }
    }

    private void TriggerEscape()
    {
        Debug.Log("Player escaped!");

        // Hide UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideNotification();
            UIManager.Instance.ShowInteractionPrompt(false, "");
        }

        // Disable player controls
        if (playerController != null)
        {
            playerController.SetControlsEnabled(false);
        }

        // Show game result UI
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnPlayerEscaped();
        }
        else
        {
            Debug.LogWarning("GameManager not found! Cannot show game result.");
        }
    }

    private void OnDrawGizmos()
    {
        // Draw escape zone
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, escapeRadius);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw filled sphere when selected
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, escapeRadius);
    }
}
