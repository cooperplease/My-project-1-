using UnityEngine;

public class Marinesnow : MonoBehaviour
{

    public float sinkSpeed = 0.5f;
    public float floorY = -5f;
    public float lifetimeOnFloor = 20f;

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y > floorY)
        {
            transform.position += Vector3.down * Time.deltaTime * sinkSpeed;
        }else Destroy(gameObject, lifetimeOnFloor);
    }
}
