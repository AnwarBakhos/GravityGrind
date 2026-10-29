using System.Collections;
using UnityEngine;

// A shy key. When the ball gets close it hops away and teases you.
// It can only run so far from where it started, and after a few dodges it gets tired and lets you grab it.
public class FleeingKey : MonoBehaviour
{
    public float range = 3.5f;       // how far from its starting spot it can go
    public float scareDistance = 2.2f;
    public int dodges = 4;

    static readonly string[] taunts = { "Nope!", "Too slow!", "Catch me!", "Hehe!", "Over here!" };

    Pickup pickup;
    Vector3 home;
    bool moving;

    void Start()
    {
        pickup = GetComponent<Pickup>();
        home = pickup.Anchor;
    }

    void Update()
    {
        if (moving || dodges <= 0 || Game.I.State != GameState.Playing) return;

        BallController ball = Game.I.Ball;
        if (ball == null || ball.Busy) return;

        Vector2 away = transform.position - ball.transform.position;
        if (away.magnitude < scareDistance) StartCoroutine(Dodge(away, ball));
    }

    IEnumerator Dodge(Vector2 away, BallController ball)
    {
        moving = true;
        dodges--;

        Vector3 from = pickup.Anchor;
        Vector3 to = PickSpot(away, ball);
        bool sideView = ball.Mode == LevelMode.SideView;

        Game.I.UI.Popup(transform.position, dodges > 0 ? taunts[Random.Range(0, taunts.Length)] : "Okay, okay...", new Color(1f, 0.6f, 0.9f));
        Sfx.Play("jump", 0.45f, 1.7f);

        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            Vector3 position = Vector3.Lerp(from, to, t);
            if (sideView) position.y += Mathf.Sin(t * Mathf.PI) * 1.6f;     // little hop
            pickup.Anchor = position;
            yield return null;
        }
        pickup.Anchor = to;

        // Tired out: stop running and droop a little
        if (dodges <= 0) transform.localScale = Vector3.one * 0.85f;
        moving = false;
    }

    Vector3 PickSpot(Vector2 away, BallController ball)
    {
        Vector3 current = pickup.Anchor;

        if (ball.Mode == LevelMode.SideView)
        {
            float direction = away.x >= 0f ? 1f : -1f;
            float x = current.x + direction * 3f;

            // Cornered at the end of its range? Jump right over the ball to the other side
            if (Mathf.Abs(x - home.x) > range) x = ball.transform.position.x - direction * 2.5f;

            x = Mathf.Clamp(x, home.x - range, home.x + range);
            return new Vector3(x, home.y, 0f);
        }

        // Tabletop: try directions away from the ball and take the first open spot
        for (int i = 0; i < 8; i++)
        {
            float angle = (i % 2 == 0 ? 1f : -1f) * (i / 2) * 45f;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * away.normalized;
            Vector3 spot = current + (Vector3)(direction * 2.5f);
            if (Vector2.Distance(spot, home) <= range && IsOpen(spot)) return spot;
        }
        return home;
    }

    // True if no wall or solid object is at this spot
    static bool IsOpen(Vector2 spot)
    {
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(spot, 0.35f))
            if (!hit.isTrigger) return false;
        return true;
    }
}
