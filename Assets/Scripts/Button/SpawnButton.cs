
using UnityEngine;

public class SpawnButton : ButtonAction
{

    [SerializeField] private GameObject prefeb;

    [SerializeField] private Vector3 position;

    [SerializeField] private Vector3 rotation;

    [SerializeField] private Vector3 scale = new Vector3(1f, 1f, 1f);
    public override void Execute()
    {
            
        GameObject newObj = Instantiate(prefeb, position, Quaternion.Euler(rotation));
        newObj.transform.localScale = scale;
    }

}