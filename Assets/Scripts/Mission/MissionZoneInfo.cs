using UnityEngine;

/// <summary>
/// Helper component to define mission zone size for procedural map generation
/// Attach this to mission zone prefabs
/// </summary>
public class MissionZoneInfo : MonoBehaviour
{
    [Header("Zone Size")]
    [Tooltip("Size of the mission zone area in tiles (X, Y)")]
    public Vector2Int size = new Vector2Int(15, 15);

    [Header("Visualization")]
    [SerializeField] private Color gizmoColor = Color.yellow;

    private void OnDrawGizmos()
    {
        // Draw the zone bounds in editor
        Gizmos.color = gizmoColor;

        Vector3 center = transform.position;
        Vector3 sizeVec = new Vector3(size.x, 0.1f, size.y);

        Gizmos.DrawWireCube(center, sizeVec);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw filled box when selected
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);

        Vector3 center = transform.position;
        Vector3 sizeVec = new Vector3(size.x, 0.1f, size.y);

        Gizmos.DrawCube(center, sizeVec);
    }
}
