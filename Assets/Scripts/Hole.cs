using UnityEngine;

// A hole in the tabletop board. If the middle of the ball rolls over it, the ball drops in.
// Trapdoors are holes with a cover that opens and closes on a timer.
public class Hole : MonoBehaviour
{
    public float catchRadius = 0.3f;
    public bool trapdoor;
    public SpriteRenderer cover;
    public float period = 2.6f;
    public float offset;

    bool wasOpen = true;

    public bool IsOpen => !trapdoor || Mathf.Repeat(Time.time + offset, period) < period * 0.5f;

    void Update()
    {
        if (!trapdoor || cover == null) return;

        bool open = IsOpen;
        float target = open ? 0f : 1f;
        float scale = Mathf.MoveTowards(cover.transform.localScale.x, target, Time.deltaTime * 6f);
        cover.transform.localScale = Vector3.one * scale;

        if (open && !wasOpen && Game.I.BallNear(transform.position, 5f)) Sfx.Play("trapdoor", 0.3f);
        wasOpen = open;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsOpen) return;

        BallController ball = other.GetComponent<BallController>();
        if (ball == null || ball.Busy) return;

        if (Vector2.Distance(ball.transform.position, transform.position) < catchRadius)
            ball.FallIntoHole(transform.position);
    }
}
