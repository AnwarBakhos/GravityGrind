using UnityEngine;

// Speed pad: launches the ball in the arrow's direction.
public class BoostPad : MonoBehaviour
{
    public Vector2 direction = Vector2.right;
    public float speed = 15f;
    float readyTime;

    void OnTriggerEnter2D(Collider2D other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball == null || ball.Busy || Time.time < readyTime) return;

        readyTime = Time.time + 0.3f;
        ball.Boost(direction, speed);
        Sfx.Play("boost", 0.6f);
        FX.Sparks(transform.position, 8);
    }
}
