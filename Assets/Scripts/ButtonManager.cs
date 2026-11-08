using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private List<Button> buttons = new List<Button>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach(Button button in buttons)
        {
            button.SetState(ButtonState.UNCLICKABLE);
        }
        buttons[0].SetState(ButtonState.CLICKABLE);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HitButton(Button button)
    {
        if(buttons.Count == 0) return;

        if(button == buttons[0])
        {
            button.SetState(ButtonState.CLICKED);

            foreach(ButtonAction btn in button.GetActions())
            {
                Debug.Log("Hit");
                btn.Execute();
            }

            buttons.RemoveAt(0);

            if(buttons.Count == 0) return;

            buttons[0].SetState(ButtonState.CLICKABLE);
        }
    }
}
