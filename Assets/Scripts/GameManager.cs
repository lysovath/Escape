using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public struct LavaSpeedKeyFrame
{
    public float time;
    public float riseSpeed;
}
public class GameManager : MonoBehaviour
{
    [SerializeField] private List<Button> buttons = new List<Button>();

    [SerializeField] private LavaRise lava;

    [SerializeField] private List<LavaSpeedKeyFrame> lavaSchedule;

    private float timeElapse = 0f;
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
        timeElapse += Time.deltaTime;
        Debug.Log(timeElapse);
        if(lavaSchedule.Count > 0 && timeElapse > lavaSchedule[0].time)
        {
            Debug.Log(lavaSchedule[0].riseSpeed);
            lava.riseSpeed = lavaSchedule[0].riseSpeed;
            lavaSchedule.RemoveAt(0);
        }
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
