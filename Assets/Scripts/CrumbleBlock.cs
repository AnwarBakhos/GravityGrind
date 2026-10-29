using System.Collections;
using UnityEngine;

// Shakes when the ball touches it, falls away, then grows back a few seconds later.
public class CrumbleBlock : MonoBehaviour
{
    BoxCollider2D box;
    SpriteRenderer sr;
    Vector3 home;
    bool crumbling;

    void Start()
    {
        box = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        home = transform.localPosition;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!crumbling && collision.gameObject.GetComponent<BallController>() != null)
            StartCoroutine(Crumble());
    }

    IEnumerator Crumble()
    {
        crumbling = true;
        Sfx.Play("crumble", 0.5f);

        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            transform.localPosition = home + (Vector3)(Random.insideUnitCircle * 0.06f);
            yield return null;
        }

        box.enabled = false;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
        {
            transform.localPosition = home + Vector3.down * t * 1.5f;
            sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);
        transform.localPosition = home;
        sr.color = Color.white;
        box.enabled = true;
        crumbling = false;
    }
}
