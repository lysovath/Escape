
using UnityEngine;

public class SpawnButton : ButtonAction
{

    [SerializeField] private GameObject prefeb;

    [SerializeField] private Vector3 position;

    [SerializeField] private Vector3 rotation;
    public override void Execute()
    {
        Instantiate(prefeb, position, Quaternion.Euler(rotation));
    }

}