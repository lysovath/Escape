using UnityEngine;


public enum ButtonState : int {
    CLICKED,
    CLICKABLE,
    UNCLICKABLE
};
public class Button : MonoBehaviour
{

    [SerializeField] ButtonState btnState = ButtonState.UNCLICKABLE;

    [SerializeField] Renderer rend;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {

        // ResolveBtnState();
    }

    void ResolveBtnState()
    {
        Material mat = Resources.Load<Material>("Materials/Buttons/Mat_ButtonClicked");
        switch (btnState)
        {
            case ButtonState.CLICKED:
                mat = Resources.Load<Material>("Materials/Buttons/Mat_ButtonClicked");
                break;
            case ButtonState.CLICKABLE:
                mat = Resources.Load<Material>("Materials/Buttons/Mat_ButtonClickable");
                break;
            case ButtonState.UNCLICKABLE:
                mat = Resources.Load<Material>("Materials/Buttons/Mat_ButtonUnClickable");
                break;

        }
        rend.material = mat;
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetState(ButtonState newState)
    {
        btnState = newState;
        ResolveBtnState();
    }
}
