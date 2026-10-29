using UnityEngine;

// One particle: flies, falls, shrinks and fades, then removes itself.
public class Particle : MonoBehaviour
{
    public Vector2 velocity;
    public float gravity;
    public float life = 0.5f;

    SpriteRenderer sr;
    Color startColor;
    Vector3 startScale;
    float age;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = sr.color;
        startScale = transform.localScale;
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= life)
        {
            Destroy(gameObject);
            return;
        }

        velocity.y -= gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, velocity.x * 60f * Time.deltaTime);

        float left = 1f - age / life;
        sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * left);
        transform.localScale = startScale * (0.4f + 0.6f * left);
    }
}
