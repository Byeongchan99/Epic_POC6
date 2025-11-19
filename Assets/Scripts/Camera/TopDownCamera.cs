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

    [Header("Rotation Settings")]
    [SerializeField] private bool followTargetRotation = false;
    [Tooltip("When enabled, camera rotates with target (useful for vehicles)")]
    [SerializeField] private float rotationSmoothSpeed = 5f;

    [Header("Vehicle Mode Settings")]
    [SerializeField] private Vector3 vehicleOffset = new Vector3(0, 20, -10);
    [SerializeField] private float vehicleCameraAngle = 45f;
    [SerializeField] private bool vehicleFollowRotation = true;

    private bool isFollowingVehicle = false;
    private Vector3 currentOffset;
    private float currentCameraAngle;
    private bool currentFollowRotation;

    private void Start()
    {
        // Set initial rotation
        transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);

        // Initialize current settings
        currentOffset = offset;
        currentCameraAngle = cameraAngle;
        currentFollowRotation = followTargetRotation;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        FollowTarget();
    }

    private void FollowTarget()
    {
        // Calculate desired position based on target's rotation if following rotation
        Vector3 desiredPosition;

        if (currentFollowRotation && target != null)
        {
            // Rotate offset by target's Y rotation
            Vector3 rotatedOffset = Quaternion.Euler(0, target.eulerAngles.y, 0) * currentOffset;
            desiredPosition = target.position + rotatedOffset;
        }
        else
        {
            // Fixed offset (world space)
            desiredPosition = target.position + currentOffset;
        }

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

        // Handle rotation
        if (currentFollowRotation && target != null)
        {
            // Follow target's Y rotation
            Quaternion targetRotation = Quaternion.Euler(currentCameraAngle, target.eulerAngles.y, 0);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                rotationSmoothSpeed * Time.deltaTime
            );
        }
        else
        {
            // Keep rotation fixed (no rotation with target)
            transform.rotation = Quaternion.Euler(currentCameraAngle, 0, 0);
        }
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
        if (!isFollowingVehicle)
            currentOffset = newOffset;
    }

    /// <summary>
    /// Switches camera to vehicle mode with vehicle-specific settings
    /// </summary>
    public void EnableVehicleMode(bool enable)
    {
        isFollowingVehicle = enable;

        if (enable)
        {
            // Use vehicle settings
            currentOffset = vehicleOffset;
            currentCameraAngle = vehicleCameraAngle;
            currentFollowRotation = vehicleFollowRotation;
            Debug.Log($"Camera switched to vehicle mode - Offset: {vehicleOffset}, Angle: {vehicleCameraAngle}, FollowRotation: {vehicleFollowRotation}");
        }
        else
        {
            // Use player settings
            currentOffset = offset;
            currentCameraAngle = cameraAngle;
            currentFollowRotation = followTargetRotation;
            Debug.Log($"Camera switched to player mode - Offset: {offset}, Angle: {cameraAngle}");
        }
    }

    /// <summary>
    /// Sets whether camera should follow target's rotation
    /// </summary>
    public void SetFollowRotation(bool follow)
    {
        currentFollowRotation = follow;
    }
}
