using UnityEngine;

// Touching a checkpoint makes it the new respawn point.
public class Checkpoint : MonoBehaviour
{
    public SpriteRenderer art;
    bool reached;

    void Start()
    {
        if (art != null) art.color = new Color(0.55f, 0.55f, 0.6f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (reached || other.GetComponent<BallController>() == null) return;

        reached = true;
        if (art != null) art.color = Color.white;
        Game.I.SetCheckpoint(art != null ? (Vector2)art.transform.position : (Vector2)transform.position);
        Sfx.Play("checkpoint", 0.8f);
        FX.Burst(transform.position, new Color(0.4f, 0.8f, 1f), 12);
    }
}
