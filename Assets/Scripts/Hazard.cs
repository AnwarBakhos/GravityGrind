using UnityEngine;

// Spikes: touching them sends the ball back to the last checkpoint.
public class Hazard : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball != null) ball.Die(true);
    }
}
