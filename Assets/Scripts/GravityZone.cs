using UnityEngine;

// Rolling through this flips which way gravity pulls the ball (up to the ceiling, or back down).
public class GravityZone : MonoBehaviour
{
    public float direction = -1f;
    public SpriteRenderer art;

    void Update()
    {
        if (art != null) art.color = new Color(1f, 1f, 1f, 0.6f + Mathf.Sin(Time.time * 5f) * 0.3f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball != null && !ball.Busy && ball.GravityDir != direction) ball.SetGravity(direction);
    }
}
