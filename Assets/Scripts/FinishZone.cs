using UnityEngine;

// The goal. On tabletop levels the ball has to roll into the finish hole,
// in side view you just have to reach the flag.
public class FinishZone : MonoBehaviour
{
    public bool sinkInto;
    public float catchRadius = 0.45f;

    void OnTriggerStay2D(Collider2D other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball == null || ball.Busy) return;

        if (!sinkInto)
        {
            Game.I.ReachFinish();
        }
        else if (Vector2.Distance(ball.transform.position, transform.position) < catchRadius)
        {
            ball.SinkIntoFinish(transform.position);
        }
    }
}
