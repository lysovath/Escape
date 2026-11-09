using UnityEngine;

public class LavaRise : MonoBehaviour
{
    [Min(0f)]
    public float riseSpeed = 1f;

    public float minY = 0f;

    public float maxY = 10f;

    private void Start()
    {
        //starting height
        maxY = Mathf.Max(minY, maxY);

        Vector3 position = transform.position;
        position.y = minY;
        transform.position = position;
    }

    private void Update()
    {
        Vector3 position = transform.position;

        //speed consistent
        position.y = Mathf.MoveTowards(
            position.y, maxY, Mathf.Max(0f, riseSpeed) * Time.deltaTime);

        transform.position = position;
    }
}
