using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -5);
    [SerializeField] private float cameraAngle = 60f;
    [SerializeField] private bool smoothFollow = true;
    [SerializeField] private float smoothSpeed = 10f;

    private void Start()
    {
        // Set initial rotation
        transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        FollowTarget();
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition = target.position + offset;

        if (smoothFollow)
        {
            // Smooth movement
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                smoothSpeed * Time.deltaTime
            );
        }
        else
        {
            // Instant movement
            transform.position = desiredPosition;
        }

        // Keep rotation fixed (no rotation with player)
        transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public Transform GetTarget()
    {
        return target;
    }

    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
}
