using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Third-person camera controller that smoothly follows a target, supports orbiting,
/// obstacle collision avoidance, and runtime/Inspector adjustments.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    #region Inspector Fields

    [Header("Target")]
    [Tooltip("The player Transform to follow. Auto-detected at runtime if left empty.")]
    [SerializeField] private Transform target;

    [Tooltip("Local offset from the target's position that the camera looks at (e.g., upper body/head).")]
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Position & Angles (Adjustable)")]
    [Tooltip("Desired distance behind the target.")]
    [Min(0.5f)]
    [SerializeField] private float distance = 7f;

    [Tooltip("Vertical camera angle in degrees (positive = looking down from above).")]
    [Range(-40f, 85f)]
    [SerializeField] private float pitch = 20f;

    [Tooltip("Horizontal camera angle in degrees around the target.")]
    [SerializeField] private float yaw = 0f;

    [Tooltip("Time (in seconds) for the camera position to smoothly catch up to the target. Set to 0 for instant follow.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float followSmoothTime = 0.08f;

    [Header("Orbit & Look Controls")]
    [Tooltip("Enable mouse/gamepad camera rotation around the player.")]
    [SerializeField] private bool enableOrbitControl = true;

    [Tooltip("If true, moving the mouse orbits the camera automatically. If false, hold Right Mouse Button to orbit.")]
    [SerializeField] private bool orbitWithoutHoldingRightClick = true;

    [Tooltip("Locks and hides the cursor on Play when free orbit is enabled (press Escape to unlock, Left Click to re-lock).")]
    [SerializeField] private bool lockCursorOnStart = true;

    [Tooltip("Look sensitivity for horizontal (X) and vertical (Y) orbit.")]
    [SerializeField] private Vector2 lookSensitivity = new Vector2(0.15f, 0.12f);

    [Tooltip("Minimum vertical pitch angle in degrees.")]
    [Range(-60f, 0f)]
    [SerializeField] private float minPitch = -15f;

    [Tooltip("Maximum vertical pitch angle in degrees.")]
    [Range(10f, 89f)]
    [SerializeField] private float maxPitch = 65f;

    [Header("Obstacle Avoidance")]
    [Tooltip("Pulls the camera closer when walls or obstacles block line-of-sight to the player.")]
    [SerializeField] private bool avoidObstacles = true;

    [Tooltip("Sphere radius used when checking for camera-blocking obstacles.")]
    [Min(0.05f)]
    [SerializeField] private float collisionRadius = 0.25f;

    [Tooltip("Minimum distance allowed when the camera is pushed in by an obstacle.")]
    [Min(0.2f)]
    [SerializeField] private float minDistance = 1.2f;

    [Tooltip("Layers treated as camera-blocking obstacles.")]
    [SerializeField] private LayerMask obstacleLayers = ~0;

    #endregion

    #region Private State

    private Vector3 smoothVelocity;
    private Vector3 currentFocusPoint;
    private bool focusInitialized;

    #endregion

    #region Public Properties

    /// <summary>The Transform currently followed by the camera.</summary>
    public Transform Target => target;

    /// <summary>Desired distance from the focus point.</summary>
    public float Distance
    {
        get => distance;
        set => distance = Mathf.Max(minDistance, value);
    }

    /// <summary>Current vertical camera angle in degrees.</summary>
    public float Pitch
    {
        get => pitch;
        set => pitch = Mathf.Clamp(value, minPitch, maxPitch);
    }

    /// <summary>Current horizontal camera angle in degrees.</summary>
    public float Yaw
    {
        get => yaw;
        set => yaw = value;
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ResolveTargetIfNeeded();

        if (target != null)
        {
            yaw = transform.eulerAngles.y;
            SnapToTarget();
        }

        if (enableOrbitControl && orbitWithoutHoldingRightClick && lockCursorOnStart)
        {
            SetCursorLocked(true);
        }
    }

    private void LateUpdate()
    {
        ResolveTargetIfNeeded();
        if (target == null)
        {
            return;
        }

        HandleOrbitInput();
        UpdateCameraTransform();
    }

    private void OnValidate()
    {
        if (maxPitch < minPitch)
        {
            maxPitch = minPitch;
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        if (distance < minDistance)
        {
            distance = minDistance;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Assigns a new target for the camera to follow.
    /// </summary>
    public void SetTarget(Transform newTarget, bool snapImmediately = false)
    {
        target = newTarget;
        if (snapImmediately && target != null)
        {
            SnapToTarget();
        }
    }

    /// <summary>
    /// Adjusts camera distance, yaw, and pitch at runtime.
    /// </summary>
    public void ConfigureView(float newDistance, float newYaw, float newPitch)
    {
        Distance = newDistance;
        Yaw = newYaw;
        Pitch = newPitch;
    }

    /// <summary>
    /// Immediately snaps the camera to its desired position behind the target without smoothing lag.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        currentFocusPoint = target.position + focusOffset;
        focusInitialized = true;
        smoothVelocity = Vector3.zero;

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredDirection = orbitRotation * Vector3.back;
        float resolvedDistance = ResolveObstacleDistance(currentFocusPoint, desiredDirection, distance);

        transform.SetPositionAndRotation(
            currentFocusPoint + desiredDirection * resolvedDistance,
            orbitRotation
        );
    }

    #endregion

    #region Internal Logic

    private void ResolveTargetIfNeeded()
    {
        if (target != null) return;

        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
            return;
        }

        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
        {
            target = playerObject.transform;
        }
    }

    private void HandleOrbitInput()
    {
        if (!enableOrbitControl) return;

        Vector2 lookDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
        }

        if (Mouse.current != null)
        {
            if (orbitWithoutHoldingRightClick && lockCursorOnStart &&
                Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }

            bool canOrbitWithMouse = orbitWithoutHoldingRightClick
                ? (Cursor.lockState == CursorLockMode.Locked || !lockCursorOnStart)
                : Mouse.current.rightButton.isPressed;

            if (canOrbitWithMouse)
            {
                lookDelta += Mouse.current.delta.ReadValue();
            }
        }

        if (Gamepad.current != null)
        {
            Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
            if (rightStick.sqrMagnitude > 0.01f)
            {
                lookDelta += rightStick * (600f * Time.deltaTime);
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }

        bool canOrbitWithMouse = orbitWithoutHoldingRightClick || Input.GetMouseButton(1);
        if (canOrbitWithMouse)
        {
            lookDelta = new Vector2(Input.GetAxis("Mouse X") * 10f, Input.GetAxis("Mouse Y") * 10f);
        }
#endif

        yaw += lookDelta.x * lookSensitivity.x;
        pitch = Mathf.Clamp(pitch - lookDelta.y * lookSensitivity.y, minPitch, maxPitch);
    }

    private void UpdateCameraTransform()
    {
        Vector3 desiredFocusPoint = target.position + focusOffset;

        if (!focusInitialized || followSmoothTime <= 0f)
        {
            currentFocusPoint = desiredFocusPoint;
            focusInitialized = true;
        }
        else
        {
            currentFocusPoint = Vector3.SmoothDamp(
                currentFocusPoint,
                desiredFocusPoint,
                ref smoothVelocity,
                followSmoothTime
            );
        }

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredDirection = orbitRotation * Vector3.back;
        float resolvedDistance = ResolveObstacleDistance(currentFocusPoint, desiredDirection, distance);

        Vector3 finalPosition = currentFocusPoint + desiredDirection * resolvedDistance;
        transform.SetPositionAndRotation(finalPosition, orbitRotation);
    }

    private float ResolveObstacleDistance(Vector3 focusPoint, Vector3 direction, float maxDesiredDistance)
    {
        if (!avoidObstacles)
        {
            return maxDesiredDistance;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            focusPoint,
            collisionRadius,
            direction,
            maxDesiredDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestHit = maxDesiredDistance;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == target || hit.collider.transform.IsChildOf(target)) continue;

            if (hit.distance < closestHit)
            {
                closestHit = hit.distance;
            }
        }

        return Mathf.Clamp(closestHit, minDistance, maxDesiredDistance);
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 focus = target.position + focusOffset;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(focus, 0.15f);
        Gizmos.DrawLine(focus, transform.position);

        if (avoidObstacles)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
        }
    }

    #endregion
}

