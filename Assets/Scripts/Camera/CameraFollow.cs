using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.2f, 0f);

    [Header("Camera Settings")]
    public float distance = 7f;
    public float mouseSensitivity = 0.15f;
    public float smoothSpeed = 12f;
    public float minPitch = -15f;
    public float maxPitch = 60f;

    private float yaw = 0f;
    private float pitch = 20f;

    private void Start()
    {
        // Find the Player automatically if not assigned in the Inspector
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        // Lock and hide the mouse cursor when the game starts
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Read mouse movement to rotate the camera
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Press Escape to unlock the cursor
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 2. Calculate camera rotation and position behind the player
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + offset;
        Vector3 desiredPosition = focusPoint - (rotation * Vector3.forward * distance);

        // 3. Smoothly move and rotate the camera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = rotation;
    }
}
