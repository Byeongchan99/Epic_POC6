using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 10f;
    [SerializeField] private float rollDuration = 0.5f;
    [SerializeField] private float rollStaminaCost = 20f;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera mainCamera;

    private CharacterController controller;
    private PlayerStats stats;
    private Gun gun;

    // Movement state
    private Vector3 moveDirection;
    private bool isRolling;
    private float rollTimer;
    private Vector3 rollDirection;

    // Invincibility
    private bool isInvincible;

    // Vehicle
    private bool isInVehicle;
    private Vehicle currentVehicle;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
        gun = GetComponent<Gun>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        // Move player to valid spawn position on land
        MapGenerator mapGen = FindAnyObjectByType<MapGenerator>();
        if (mapGen != null)
        {
            Vector3 spawnPos = mapGen.GetPlayerSpawnPosition();

            // Disable CharacterController temporarily to teleport
            controller.enabled = false;
            transform.position = spawnPos;
            controller.enabled = true;

            Debug.Log($"Player spawned at valid land position: {spawnPos}");
        }
        else
        {
            Debug.LogWarning("MapGenerator not found! Player may spawn in water.");
        }
    }

    private void Update()
    {
        if (isInVehicle)
            return;

        if (!isRolling)
        {
            HandleMovement();
            HandleRoll();
            HandleRotation();
            HandleShooting();
            HandleInteraction();
        }
        else
        {
            UpdateRoll();
        }
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0, v).normalized;

        // Sprint
        float currentSpeed = baseSpeed;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && stats.GetStamina() > 0;

        if (isSprinting && move.magnitude > 0)
        {
            currentSpeed *= sprintMultiplier;
            stats.ConsumeStamina(stats.sprintStaminaCost * Time.deltaTime);
        }

        // Movement
        Vector3 motion = move * currentSpeed * Time.deltaTime;

        // Apply gravity
        motion.y = gravity * Time.deltaTime;

        controller.Move(motion);
    }

    private void HandleRoll()
    {
        if (Input.GetKeyDown(KeyCode.Space) && stats.GetStamina() >= rollStaminaCost)
        {
            StartRoll();
        }
    }

    private void StartRoll()
    {
        isRolling = true;
        rollTimer = rollDuration;
        isInvincible = true;

        // Determine roll direction
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        rollDirection = new Vector3(h, 0, v).normalized;

        // If no input, roll forward
        if (rollDirection.magnitude < 0.1f)
            rollDirection = transform.forward;

        stats.ConsumeStamina(rollStaminaCost);

        Debug.Log("Rolling!");
    }

    private void UpdateRoll()
    {
        // Move during roll
        Vector3 motion = rollDirection * rollSpeed * Time.deltaTime;
        motion.y = gravity * Time.deltaTime;
        controller.Move(motion);

        // Update timer
        rollTimer -= Time.deltaTime;

        if (rollTimer <= 0)
        {
            EndRoll();
        }
    }

    private void EndRoll()
    {
        isRolling = false;
        isInvincible = false;
        Debug.Log("Roll ended");
    }

    private void HandleRotation()
    {
        // Rotate towards mouse position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0;

            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = targetRotation;
            }
        }
    }

    private void HandleShooting()
    {
        if (gun == null)
            return;

        // Fire
        if (Input.GetMouseButton(0))
        {
            Vector3 aimDirection = GetAimDirection();
            gun.Fire(aimDirection);
        }

        // Reload
        if (Input.GetKeyDown(KeyCode.R))
        {
            gun.Reload();
        }
    }

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        // Check for nearby interactable objects
        float interactionRadius = 3f;
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRadius);

        Debug.Log($"Checking for interactions... Found {colliders.Length} colliders in {interactionRadius}m radius");

        foreach (Collider col in colliders)
        {
            // Skip self
            if (col.gameObject == gameObject)
                continue;

            // Try to find IInteractable on the object or its parents
            IInteractable interactable = col.GetComponent<IInteractable>();

            // If not found, search in parent hierarchy
            if (interactable == null)
            {
                interactable = col.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                Debug.Log($"Found interactable: {col.gameObject.name}, Distance: {Vector3.Distance(transform.position, col.transform.position):F2}m");
                interactable.Interact(this);
                return;
            }
        }

        Debug.Log("No interactable objects found nearby");
    }

    public bool IsInvincible()
    {
        return isInvincible;
    }

    public void EnterVehicle(Vehicle vehicle)
    {
        isInVehicle = true;
        currentVehicle = vehicle;
        gameObject.SetActive(false);

        Debug.Log("Entered vehicle");
    }

    public void ExitVehicle(Vector3 exitPosition)
    {
        isInVehicle = false;
        currentVehicle = null;
        transform.position = exitPosition;
        gameObject.SetActive(true);

        Debug.Log("Exited vehicle");
    }

    public bool IsInVehicle()
    {
        return isInVehicle;
    }

    public Vehicle GetCurrentVehicle()
    {
        return currentVehicle;
    }

    public Vector3 GetAimDirection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 direction = targetPoint - firePoint.position;
            direction.y = 0;
            return direction.normalized;
        }

        return transform.forward;
    }
}
