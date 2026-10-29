using UnityEngine;

// A solid platform that slides back and forth. The ball rides along on top of it.
public class MovingPlatform : MonoBehaviour
{
    public Vector2 travel = new Vector2(4f, 0f);
    public float period = 4f;

    Rigidbody2D body;
    Vector2 start;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        start = body.position;
    }

    void FixedUpdate()
    {
        // 0 -> 1 -> 0 smoothly over one period
        float t = (1f - Mathf.Cos(Time.fixedTime * Mathf.PI * 2f / period)) * 0.5f;
        body.MovePosition(start + travel * t);
    }
}
