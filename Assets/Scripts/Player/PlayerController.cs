using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 10f;
    public float jumpForce = 7.5f;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Audio Clips")]
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip footstepSound;
    public AudioClip runFootstepSound;
    public AudioClip lavaDeathSound;
    public AudioClip buttonPressSound;

    [Header("Footstep Timing & Volume")]
    public float walkStepInterval = 0.4f;
    public float runStepInterval = 0.65f;
    [Range(0f, 1f)] public float footstepVolume = 0.25f;

    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector2 moveInput;
    private bool isSprinting;
    private bool isGrounded;
    private bool wasGrounded = true;
    private float stepTimer;

    [SerializeField] private GameManager bm;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Prevent the player capsule from tipping over

        // Get or add an AudioSource on the Player to play sound effects
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Give every player collider zero friction so the player slides off walls and edges instead of sticking
        PhysicsMaterial noFriction = new PhysicsMaterial("NoFriction");
        noFriction.dynamicFriction = 0f;
        noFriction.staticFriction = 0f;
        noFriction.frictionCombine = PhysicsMaterialCombine.Minimum;

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.material = noFriction;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        ReadInput();
        HandleFootstepAudio();

        // Jump when pressing Space while on the ground
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            PlaySound(jumpSound);
        }
    }

    private void FixedUpdate()
    {
        // Remember if we were grounded last physics step, then reset before OnCollisionStay runs
        wasGrounded = isGrounded;
        isGrounded = false;

        MovePlayer();
    }

    private void ReadInput()
    {
        moveInput = Vector2.zero;
        isSprinting = false;

        if (Keyboard.current == null) return;

        // Forward / Backward (W, S or Up, Down arrows)
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1f;

        // Left / Right (A, D or Left, Right arrows)
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1f;

        moveInput = moveInput.normalized;
        isSprinting = Keyboard.current.leftShiftKey.isPressed;
    }

    private void MovePlayer()
    {
        // Get camera forward and right directions on a flat horizontal plane
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction and speed
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // Move the Rigidbody horizontally while keeping vertical gravity velocity
        rb.linearVelocity = new Vector3(
            moveDirection.x * currentSpeed,
            rb.linearVelocity.y,
            moveDirection.z * currentSpeed
        );

        // Rotate the player to face the movement direction (+90 degrees because the model's eyes face -X)
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection) * Quaternion.Euler(0f, 90f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void HandleFootstepAudio()
    {
        if (isGrounded && moveInput != Vector2.zero)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                if (isSprinting && runFootstepSound != null)
                {
                    PlaySound(runFootstepSound, footstepVolume);
                    stepTimer = runStepInterval;
                }
                else
                {
                    PlaySound(footstepSound, footstepVolume);
                    stepTimer = walkStepInterval;
                }
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Lava"))
        {
            Die();
            return;
        }

        if (collision.gameObject.CompareTag("Button"))
        {
            PlaySound(buttonPressSound);
            if (bm != null)
            {
                bm.HitButton(collision.collider.GetComponent<Button>());
            }
        }
    }

    // Also works if the lava collider is set to "Is Trigger"
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Lava"))
        {
            Die();
        }
    }

    // Kill the player when touching lava
    private void Die()
    {
        Debug.Log("Game Over! Player touched lava.");

        // PlayClipAtPoint plays the sound in the world so it doesn't get cut off when the Player is destroyed
        if (lavaDeathSound != null)
        {
            AudioSource.PlayClipAtPoint(lavaDeathSound, transform.position);
        }

        Destroy(gameObject);
    }

    // Check when the player is touching the ground
    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // If the surface normal points upward, the player is standing on ground
            if (contact.normal.y > 0.5f)
            {
                if (!wasGrounded && !isGrounded)
                {
                    PlaySound(landSound);
                }
                isGrounded = true;
                return;
            }
        }
    }
}
