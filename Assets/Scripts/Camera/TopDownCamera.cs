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
    [Tooltip("Camera offset when in vehicle (adjustable in Play Mode)")]
    [SerializeField] private Vector3 vehicleOffset = new Vector3(0, 20, -10);
    [Tooltip("Camera angle when in vehicle (adjustable in Play Mode)")]
    [SerializeField] private float vehicleCameraAngle = 45f;
    [Tooltip("Whether camera follows vehicle rotation (adjustable in Play Mode)")]
    [SerializeField] private bool vehicleFollowRotation = true;
    [Tooltip("Enable to see real-time updates in Play Mode")]
    [SerializeField] private bool enableRuntimeAdjustment = true;

    [Header("Mouse Follow Settings")]
    [Tooltip("Enable camera offset based on mouse position")]
    [SerializeField] private bool enableMouseFollow = true;
    [Tooltip("Maximum distance camera can move towards mouse (in world units)")]
    [SerializeField] private float maxMouseOffset = 5f;
    [Tooltip("How fast camera follows mouse position")]
    [SerializeField] private float mouseFollowSpeed = 3f;
    [Tooltip("Dead zone in center where mouse doesn't affect camera (0-1, where 0.5 = half screen)")]
    [SerializeField] private float mouseDeadZone = 0.1f;

    private bool isFollowingVehicle = false;
    private Vector3 currentOffset;
    private float currentCameraAngle;
    private bool currentFollowRotation;

    // Cache previous values to detect changes in Inspector
    private Vector3 previousVehicleOffset;
    private float previousVehicleCameraAngle;
    private bool previousVehicleFollowRotation;

    // Mouse follow
    private Vector3 currentMouseOffset = Vector3.zero;

    private void Start()
    {
        // Set initial rotation
        transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);

        // Initialize current settings
        currentOffset = offset;
        currentCameraAngle = cameraAngle;
        currentFollowRotation = followTargetRotation;

        // Initialize cache for runtime adjustment
        previousVehicleOffset = vehicleOffset;
        previousVehicleCameraAngle = vehicleCameraAngle;
        previousVehicleFollowRotation = vehicleFollowRotation;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Check for runtime adjustments in Play Mode (for vehicle settings)
        if (enableRuntimeAdjustment && isFollowingVehicle)
        {
            CheckForVehicleSettingsChanges();
        }

        // Update mouse offset
        if (enableMouseFollow)
        {
            UpdateMouseOffset();
        }

        FollowTarget();
    }

    private void CheckForVehicleSettingsChanges()
    {
        // Detect if vehicle settings changed in Inspector during Play Mode
        bool offsetChanged = previousVehicleOffset != vehicleOffset;
        bool angleChanged = !Mathf.Approximately(previousVehicleCameraAngle, vehicleCameraAngle);
        bool rotationChanged = previousVehicleFollowRotation != vehicleFollowRotation;

        if (offsetChanged || angleChanged || rotationChanged)
        {
            // Apply new settings immediately
            currentOffset = vehicleOffset;
            currentCameraAngle = vehicleCameraAngle;
            currentFollowRotation = vehicleFollowRotation;

            // Update cache
            previousVehicleOffset = vehicleOffset;
            previousVehicleCameraAngle = vehicleCameraAngle;
            previousVehicleFollowRotation = vehicleFollowRotation;

            Debug.Log($"[Runtime Adjustment] Vehicle camera updated - Offset: {vehicleOffset}, Angle: {vehicleCameraAngle}, FollowRotation: {vehicleFollowRotation}");
        }
    }

    private void UpdateMouseOffset()
    {
        // Get mouse position in viewport space (0-1)
        Vector2 mouseViewportPos = new Vector2(
            Input.mousePosition.x / Screen.width,
            Input.mousePosition.y / Screen.height
        );

        // Convert to -1 to 1 range (centered at 0)
        Vector2 mouseNormalized = new Vector2(
            (mouseViewportPos.x - 0.5f) * 2f,
            (mouseViewportPos.y - 0.5f) * 2f
        );

        // Apply dead zone
        if (mouseNormalized.magnitude < mouseDeadZone)
        {
            mouseNormalized = Vector2.zero;
        }
        else
        {
            // Smooth dead zone transition
            float magnitude = mouseNormalized.magnitude;
            float adjustedMagnitude = (magnitude - mouseDeadZone) / (1f - mouseDeadZone);
            mouseNormalized = mouseNormalized.normalized * adjustedMagnitude;
        }

        // Clamp to prevent values greater than 1
        mouseNormalized = Vector2.ClampMagnitude(mouseNormalized, 1f);

        // Calculate target offset (world space, on XZ plane)
        Vector3 targetMouseOffset = new Vector3(
            mouseNormalized.x * maxMouseOffset,
            0f,
            mouseNormalized.y * maxMouseOffset
        );

        // Smoothly interpolate current offset
        currentMouseOffset = Vector3.Lerp(
            currentMouseOffset,
            targetMouseOffset,
            mouseFollowSpeed * Time.deltaTime
        );
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

        // Add mouse offset to desired position
        if (enableMouseFollow)
        {
            desiredPosition += currentMouseOffset;
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

    // Context menu for quick adjustments in Play Mode
    [ContextMenu("Apply Vehicle Settings Now")]
    private void ApplyVehicleSettingsNow()
    {
        if (isFollowingVehicle)
        {
            currentOffset = vehicleOffset;
            currentCameraAngle = vehicleCameraAngle;
            currentFollowRotation = vehicleFollowRotation;
            Debug.Log($"[Manual Apply] Vehicle camera updated - Offset: {vehicleOffset}, Angle: {vehicleCameraAngle}");
        }
        else
        {
            Debug.LogWarning("Not in vehicle mode. Enter a vehicle first.");
        }
    }

    [ContextMenu("Reset to Default Vehicle Settings")]
    private void ResetToDefaultVehicleSettings()
    {
        vehicleOffset = new Vector3(0, 20, -10);
        vehicleCameraAngle = 45f;
        vehicleFollowRotation = true;

        if (isFollowingVehicle)
        {
            ApplyVehicleSettingsNow();
        }

        Debug.Log("Vehicle camera settings reset to defaults");
    }
}
