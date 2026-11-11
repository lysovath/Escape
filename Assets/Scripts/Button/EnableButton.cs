

using UnityEngine;

public class EnableButton : ButtonAction
{
    [SerializeField] private GameObject target;
    public override void Execute()
    {
        target.SetActive(true);
    }
}