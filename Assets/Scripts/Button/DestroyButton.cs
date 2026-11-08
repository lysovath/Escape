

using UnityEngine;

public class DestroyButton : ButtonAction
{
    [SerializeField] private GameObject target;
    public override void Execute()
    {
        Destroy(target);
    }
}