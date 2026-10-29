using UnityEngine;

// Teleporter: rolling into one sends the ball out of its partner, keeping its speed.
public class Portal : MonoBehaviour
{
    public Portal partner;
    public Transform art;

    void Update()
    {
        if (art != null) art.Rotate(0f, 0f, -200f * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball == null || partner == null || ball.Busy || !ball.CanTeleport) return;

        FX.Burst(transform.position, new Color(0.5f, 0.85f, 1f), 10);
        ball.Teleport(partner.transform.position);
        FX.Burst(partner.transform.position, new Color(1f, 0.6f, 0.25f), 10);
        Sfx.Play("portal", 0.7f);
    }
}
