using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class LobbyMapSelection : MonoBehaviour
{
    [SerializeField] GameObject popup;
    [SerializeField] PlayerController player;
    [SerializeField] CameraFollow followCamera;
    [SerializeField] string mapScene = "Assets/Scenes/Map1.unity";

    int inside;
    bool open;
    Rigidbody body;
    bool playerWasOn, cameraWasOn, cursorWasOn;
    RigidbodyConstraints oldFreeze;
    Vector3 oldMove, oldSpin;
    CursorLockMode oldLock;

    private void Awake()
    {
        body = player.GetComponent<Rigidbody>();
        popup.SetActive(false);
    }

    void Update()
    {
        if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (inside++ == 0) Open();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other) || inside == 0) return;
        if (--inside == 0) Close();
    }

    bool IsPlayer(Collider other)
    {
        return other.GetComponentInParent<PlayerController>() == player;
    }

    void Open()
    {
        if (open) return;
        playerWasOn = player.enabled;
        cameraWasOn = followCamera.enabled;
        oldFreeze = body.constraints;
        oldMove = body.linearVelocity;
        oldSpin = body.angularVelocity;
        oldLock = Cursor.lockState;
        cursorWasOn = Cursor.visible;

        player.enabled = false;
        followCamera.enabled = false;
        // Freeze the body. Do not switch isKinematic or the popup opens again.
        body.constraints = RigidbodyConstraints.FreezeAll;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        open = true;
        popup.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!open) return;
        open = false;
        if (popup) popup.SetActive(false);
        if (body)
        {
            body.constraints = oldFreeze;
            if (!body.isKinematic)
            {
                body.linearVelocity = oldMove;
                body.angularVelocity = oldSpin;
            }
        }
        if (player) player.enabled = playerWasOn;
        if (followCamera) followCamera.enabled = cameraWasOn;
        Cursor.lockState = oldLock;
        Cursor.visible = cursorWasOn;
    }

    public void LoadMap1()
    {
        if (!open) return;
        if (!Application.CanStreamedLevelBeLoaded(mapScene))
        {
            Debug.LogError("Map1 is not in Build Settings.", this);
            return;
        }
        Close();
        SceneManager.LoadScene(mapScene);
    }

    void OnDisable()
    {
        Close();
        inside = 0;
    }
}
