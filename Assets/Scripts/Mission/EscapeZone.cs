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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

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
            if (enableDebugLogs) Debug.Log($"[EscapeZone] Player found at {player.position}");
        }
        else
        {
            Debug.LogError("[EscapeZone] PlayerController not found!");
        }

        // Add minimap marker
        MinimapController minimap = FindAnyObjectByType<MinimapController>();
        if (minimap != null && escapeMarkerPrefab != null)
        {
            minimapMarker = minimap.AddMissionMarker(transform.position, escapeMarkerPrefab);
            if (enableDebugLogs) Debug.Log("[EscapeZone] Escape zone marker added to minimap");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[EscapeZone] Minimap or marker prefab missing. Minimap: {minimap != null}, Prefab: {escapeMarkerPrefab != null}");
        }

        // Show notification to player
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("모든 임무 완료! 탈출 지역으로 이동하세요!", 8f);
        }

        if (enableDebugLogs) Debug.Log($"[EscapeZone] Initialized at {transform.position}, radius: {escapeRadius}");
    }

    private void Update()
    {
        if (player == null)
        {
            if (enableDebugLogs && Time.frameCount % 60 == 0) // Log every 60 frames
                Debug.LogWarning("[EscapeZone] Player reference is null!");
            return;
        }

        // Check if player is in escape zone
        float distance = Vector3.Distance(player.position, transform.position);
        bool wasInZone = playerInZone;
        playerInZone = distance <= escapeRadius;

        // Log zone entry/exit
        if (playerInZone && !wasInZone && enableDebugLogs)
        {
            Debug.Log($"[EscapeZone] Player ENTERED escape zone! Distance: {distance:F2}m");
        }
        else if (!playerInZone && wasInZone && enableDebugLogs)
        {
            Debug.Log($"[EscapeZone] Player LEFT escape zone! Distance: {distance:F2}m");
        }

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

            // Log progress every second
            if (enableDebugLogs && Mathf.FloorToInt(stayTimer) > Mathf.FloorToInt(stayTimer - Time.deltaTime))
            {
                Debug.Log($"[EscapeZone] Escape timer: {stayTimer:F1}s / {requiredStayTime}s");
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

                if (enableDebugLogs) Debug.Log("[EscapeZone] Timer reset - player left zone");
            }
        }
    }

    private void TriggerEscape()
    {
        if (enableDebugLogs) Debug.Log("[EscapeZone] ESCAPE TRIGGERED!");

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
            Debug.LogWarning("[EscapeZone] GameManager not found! Cannot show game result.");
        }

        // Disable this script to prevent multiple triggers
        enabled = false;
    }

    private void OnDrawGizmos()
    {
        // Draw escape zone
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, escapeRadius);

        // Draw distance line to player (in editor)
        if (player != null && Application.isPlaying)
        {
            float distance = Vector3.Distance(player.position, transform.position);
            Gizmos.color = playerInZone ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);

            // Draw player position marker
            Gizmos.DrawWireSphere(player.position, 0.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw filled sphere when selected
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, escapeRadius);
    }
}
