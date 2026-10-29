using System.Collections;
using UnityEngine;

// Launches the ball high into the air.
public class Spring : MonoBehaviour
{
    public SpriteRenderer art;
    float readyTime;

    void OnTriggerEnter2D(Collider2D other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball == null || Time.time < readyTime) return;

        readyTime = Time.time + 0.2f;
        ball.Bounce();
        Sfx.Play("spring", 0.8f);
        if (art != null) StartCoroutine(Boing());
    }

    // squash and stretch the spring
    IEnumerator Boing()
    {
        Transform t = art.transform;
        float time = 0f;
        while (time < 0.3f)
        {
            time += Time.deltaTime;
            float s = 1f + Mathf.Sin(time * 30f) * 0.3f * (1f - time / 0.3f);
            t.localScale = new Vector3(2f - s, s, 1f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}
