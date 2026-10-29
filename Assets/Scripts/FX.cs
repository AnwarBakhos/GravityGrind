using UnityEngine;

// Small particle effects built from tiny square sprites: dust, sparks, bursts and confetti.
public static class FX
{
    // Particles are parented to the current level so they get cleaned up with it
    public static Transform Parent;

    static readonly Color[] confettiColors =
    {
        new Color(1f, 0.35f, 0.35f), new Color(1f, 0.82f, 0.25f), new Color(0.35f, 0.85f, 0.45f),
        new Color(0.3f, 0.7f, 1f), new Color(0.85f, 0.45f, 1f)
    };

    public static void Dust(Vector2 position, int count)
    {
        Spawn(position, count, new Color(0.9f, 0.8f, 0.65f, 0.8f), 1.2f, 0.14f, 0.35f, -1f, Vector2.up * 0.6f);
    }

    public static void Sparks(Vector2 position, int count)
    {
        Spawn(position, count, new Color(1f, 0.92f, 0.55f), 4f, 0.09f, 0.3f, 0f, Vector2.zero);
    }

    public static void Burst(Vector2 position, Color color, int count)
    {
        Spawn(position, count, color, 5f, 0.16f, 0.6f, 6f, Vector2.up * 2f);
    }

    public static void Confetti(Vector2 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Color color = confettiColors[Random.Range(0, confettiColors.Length)];
            Spawn(position, 1, color, 6f, 0.18f, 1.6f, 9f, Vector2.up * 7f);
        }
    }

    static void Spawn(Vector2 position, int count, Color color, float speed, float size, float life, float gravity, Vector2 push)
    {
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer sr = Art.Place(Parent, Art.White, position, 20, "Particle");
            sr.color = color;
            sr.transform.position = position;
            sr.transform.localScale = Vector3.one * size * Random.Range(0.7f, 1.3f);

            Particle particle = sr.gameObject.AddComponent<Particle>();
            particle.velocity = Random.insideUnitCircle * speed + push;
            particle.gravity = gravity;
            particle.life = life * Random.Range(0.7f, 1.2f);
        }
    }
}
