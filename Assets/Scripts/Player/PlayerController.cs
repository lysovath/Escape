using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Physics-based third-person character controller supporting camera-relative movement,
/// sprinting, jumping, ground detection, and procedural limb animation.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement")]
    [Tooltip("Standard walking speed in units per second.")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("Sprinting speed when holding Sprint input.")]
    [Min(0f)]
    [SerializeField] private float sprintSpeed = 10f;

    [Tooltip("How quickly horizontal velocity accelerates/decelerates.")]
    [Min(0.1f)]
    [SerializeField] private float acceleration = 25f;

    [Tooltip("Multiplier applied to acceleration while airborne (0 = no air control, 1 = full control).")]
    [Range(0f, 1f)]
    [SerializeField] private float airControlMultiplier = 0.5f;

    [Tooltip("How quickly the character rotates toward the movement direction.")]
    [Min(0.1f)]
    [SerializeField] private float rotationSpeed = 12f;

    [Tooltip("Yaw offset (in degrees) so the character model's front faces the movement direction. Default is 90 because the Player prefab eyes face -X.")]
    [SerializeField] private float modelForwardYawOffset = 90f;

    [Header("Jumping & Gravity")]
    [Tooltip("Upward impulse force applied when jumping.")]
    [Min(0f)]
    [SerializeField] private float jumpForce = 7.5f;

    [Tooltip("Extra gravity multiplier when falling for responsive platforming.")]
    [Min(1f)]
    [SerializeField] private float fallGravityMultiplier = 1.8f;

    [Tooltip("Grace time (in seconds) after leaving a ledge where the player can still jump.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("Grace time (in seconds) before landing where a jump input is buffered.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Detection")]
    [Tooltip("Extra distance below the player's lowest collider bound to check for ground.")]
    [Min(0.01f)]
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Tooltip("Maximum surface slope angle (in degrees) considered walkable ground.")]
    [Range(15f, 80f)]
    [SerializeField] private float maxGroundAngle = 55f;

    [Tooltip("Layers recognized as walkable ground.")]
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("References")]
    [Tooltip("Reference to the camera Transform used for camera-relative movement. Auto-detected from Camera.main if empty.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Procedural Limb Animation")]
    [Tooltip("Animates LeftLeg, RightLeg, LeftArm, and RightArm child transforms while moving.")]
    [SerializeField] private bool animateLimbs = true;

    [Min(0f)]
    [SerializeField] private float limbSwingSpeed = 10f;

    [Range(0f, 80f)]
    [SerializeField] private float limbSwingAngle = 35f;

    #endregion

    #region Private State

    private Rigidbody rb;
    private Collider[] playerColliders;

    private Vector2 moveInput;
    private bool sprintHeld;
    private bool movementEnabled = true;

    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;
    private float lastGroundedTime = -10f;
    private float lastJumpPressedTime = -10f;
    private float lastJumpExecutedTime = -10f;

    // Cached limb transforms for procedural walking animation
    private Transform leftLeg;
    private Transform rightLeg;
    private Transform leftArm;
    private Transform rightArm;
    private float walkCycleTime;

    #endregion

    #region Public Properties

    /// <summary>True when the player is standing on valid ground.</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>True when movement input is active and movement is enabled.</summary>
    public bool IsMoving => movementEnabled && moveInput.sqrMagnitude > 0.01f;

    /// <summary>True when the player is actively moving and holding sprint.</summary>
    public bool IsSprinting => IsMoving && sprintHeld;

    /// <summary>Current normalized 2D movement input (X = right/left, Y = forward/backward).</summary>
    public Vector2 MoveInput => moveInput;

    /// <summary>Current world-space velocity of the player's Rigidbody.</summary>
    public Vector3 CurrentVelocity => rb != null ? rb.linearVelocity : Vector3.zero;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializePhysics();
        CacheLimbsAndSanitizeChildPhysics();
        ResolveCameraReference();
    }

    private void Update()
    {
        if (movementEnabled)
        {
            ReadInput();
        }
        else
        {
            moveInput = Vector2.zero;
            sprintHeld = false;
        }

        CheckGrounded();
        UpdateLimbAnimation();
    }

    private void FixedUpdate()
    {
        if (!movementEnabled)
        {
            return;
        }

        HandleMovement();
        HandleJump();
        ApplyExtraGravity();
    }

    private void OnValidate()
    {
        if (sprintSpeed < moveSpeed)
        {
            sprintSpeed = moveSpeed;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Enables or disables player input and movement (useful for UI, cutscenes, or death states).
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!enabled && rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            moveInput = Vector2.zero;
            sprintHeld = false;
        }
    }

    /// <summary>
    /// Instantly teleports the player to a target position and rotation and clears velocity (useful for respawning).
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(position, rotation);
    }

    #endregion

    #region Initialization

    private void InitializePhysics()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        playerColliders = GetComponentsInChildren<Collider>();
    }

    private void CacheLimbsAndSanitizeChildPhysics()
    {
        leftLeg = transform.Find("LeftLeg");
        rightLeg = transform.Find("RightLeg");
        leftArm = transform.Find("LeftArm");
        rightArm = transform.Find("RightArm");

        // Remove unconnected HingeJoints and make child Rigidbodies kinematic so limbs don't detach at runtime
        HingeJoint[] childJoints = GetComponentsInChildren<HingeJoint>();
        foreach (HingeJoint joint in childJoints)
        {
            if (joint.gameObject != gameObject && joint.connectedBody == null)
            {
                Destroy(joint);
            }
        }

        Rigidbody[] childBodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody childRb in childBodies)
        {
            if (childRb != rb)
            {
                childRb.isKinematic = true;
                childRb.useGravity = false;
            }
        }
    }

    private void ResolveCameraReference()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    #endregion

    #region Input Handling

    private void ReadInput()
    {
        Vector2 rawMove = Vector2.zero;
        sprintHeld = false;
        bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) rawMove.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) rawMove.x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) rawMove.y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) rawMove.y += 1f;

            if (Keyboard.current.leftShiftKey.isPressed)
            {
                sprintHeld = true;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpPressed = true;
            }
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > rawMove.sqrMagnitude)
            {
                rawMove = stick;
            }

            if (Gamepad.current.leftStickButton.isPressed)
            {
                sprintHeld = true;
            }

            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                jumpPressed = true;
            }
        }
#else
        rawMove = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
        jumpPressed = Input.GetButtonDown("Jump");
#endif

        moveInput = Vector2.ClampMagnitude(rawMove, 1f);

        if (jumpPressed)
        {
            lastJumpPressedTime = Time.time;
        }
    }

    #endregion

    #region Movement & Jumping

    private void HandleMovement()
    {
        ResolveCameraReference();

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
        }

        Vector3 moveDirection = Vector3.ClampMagnitude(forward * moveInput.y + right * moveInput.x, 1f);
        float targetSpeed = sprintHeld ? sprintSpeed : moveSpeed;
        Vector3 targetHorizontalVelocity = moveDirection * targetSpeed;

        // Project movement onto slope when grounded
        if (isGrounded && moveDirection.sqrMagnitude > 0.001f)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal).normalized;
            targetHorizontalVelocity = slopeDirection * targetSpeed;
        }

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 desiredHorizontal = new Vector3(targetHorizontalVelocity.x, 0f, targetHorizontalVelocity.z);

        float currentAccel = isGrounded ? acceleration : acceleration * airControlMultiplier;
        Vector3 newHorizontal = Vector3.MoveTowards(
            currentHorizontal,
            desiredHorizontal,
            currentAccel * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(newHorizontal.x, currentVelocity.y, newHorizontal.z);

        // Smoothly rotate character toward movement direction
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up)
                                        * Quaternion.Euler(0f, modelForwardYawOffset, 0f);
            Quaternion nextRotation = Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );
            rb.MoveRotation(nextRotation);
        }
    }

    private void HandleJump()
    {
        bool hasBufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;
        bool withinCoyoteTime = (Time.time - lastGroundedTime) <= coyoteTime;
        bool jumpCooldownElapsed = (Time.time - lastJumpExecutedTime) > 0.15f;

        if (hasBufferedJump && withinCoyoteTime && jumpCooldownElapsed)
        {
            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
            lastJumpExecutedTime = Time.time;
            isGrounded = false;

            Vector3 velocity = rb.linearVelocity;
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }
            rb.linearVelocity = velocity;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void ApplyExtraGravity()
    {
        if (!isGrounded && rb.linearVelocity.y < 0f && fallGravityMultiplier > 1f)
        {
            Vector3 extraGravity = Physics.gravity * (fallGravityMultiplier - 1f);
            rb.AddForce(extraGravity, ForceMode.Acceleration);
        }
    }

    #endregion

    #region Ground Detection

    private void CheckGrounded()
    {
        // Prevent immediately re-grounding on the exact frame a jump starts
        if ((Time.time - lastJumpExecutedTime) < 0.08f)
        {
            isGrounded = false;
            groundNormal = Vector3.up;
            return;
        }

        GetGroundCastParameters(out Vector3 origin, out float radius, out float castDistance);

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            radius,
            Vector3.down,
            castDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        isGrounded = false;
        groundNormal = Vector3.up;
        float minAllowedNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

        foreach (RaycastHit hit in hits)
        {
            if (IsSelfCollider(hit.collider))
            {
                continue;
            }

            if (hit.normal.y >= minAllowedNormalY)
            {
                isGrounded = true;
                groundNormal = hit.normal;
                lastGroundedTime = Time.time;
                break;
            }
        }
    }

    private void GetGroundCastParameters(out Vector3 origin, out float radius, out float castDistance)
    {
        Bounds combinedBounds = GetCombinedBounds();
        radius = Mathf.Max(0.15f, Mathf.Min(combinedBounds.extents.x, combinedBounds.extents.z) * 0.55f);
        float startOffset = radius + 0.05f;
        origin = new Vector3(
            combinedBounds.center.x,
            combinedBounds.min.y + startOffset,
            combinedBounds.center.z
        );
        castDistance = groundCheckDistance + 0.05f;
    }

    private Bounds GetCombinedBounds()
    {
        if (playerColliders == null || playerColliders.Length == 0)
        {
            playerColliders = GetComponentsInChildren<Collider>();
        }

        bool initialized = false;
        Bounds bounds = new Bounds(transform.position, new Vector3(1f, 3.6f, 1f));

        if (playerColliders != null)
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider col = playerColliders[i];
                if (col != null && col.enabled && !col.isTrigger)
                {
                    if (!initialized)
                    {
                        bounds = col.bounds;
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(col.bounds);
                    }
                }
            }
        }

        return bounds;
    }

    private bool IsSelfCollider(Collider col)
    {
        if (col == null) return false;
        return col.transform == transform || col.transform.IsChildOf(transform);
    }

    #endregion

    #region Procedural Animation & Gizmos

    private void UpdateLimbAnimation()
    {
        if (!animateLimbs) return;

        if (IsMoving && isGrounded)
        {
            float speedMultiplier = sprintHeld ? 1.4f : 1f;
            walkCycleTime += Time.deltaTime * limbSwingSpeed * speedMultiplier;
            float swing = Mathf.Sin(walkCycleTime) * limbSwingAngle;

            // Player model faces -X locally, so forward/backward limb swing rotates around local Z axis
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0f, 0f, swing);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0f, 0f, -swing);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0f, 0f, -swing);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0f, 0f, swing);
        }
        else
        {
            walkCycleTime = 0f;
            float returnSpeed = 10f * Time.deltaTime;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.identity, returnSpeed);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, returnSpeed);
            if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.identity, returnSpeed);
            if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.identity, returnSpeed);
        }
    }

    private void OnDrawGizmosSelected()
    {
        GetGroundCastParameters(out Vector3 origin, out float radius, out float castDistance);
        Vector3 endCenter = origin + Vector3.down * castDistance;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(endCenter, radius);
        Gizmos.DrawLine(origin, endCenter);
    }

    #endregion
}

