using UnityEngine;

// A bar that keeps spinning and knocks the ball around.
public class Spinner : MonoBehaviour
{
    public float speed = 100f;
    Rigidbody2D body;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        body.MoveRotation(body.rotation + speed * Time.fixedDeltaTime);
    }
}
