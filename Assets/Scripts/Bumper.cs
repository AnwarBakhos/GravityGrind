using System.Collections;
using UnityEngine;

// Pinball bumper: knocks the ball away hard when it hits.
public class Bumper : MonoBehaviour
{
    public float power = 11f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        BallController ball = collision.gameObject.GetComponent<BallController>();
        if (ball == null) return;

        Vector2 away = ((Vector2)ball.transform.position - (Vector2)transform.position).normalized;
        ball.Kick(away * power);
        Sfx.Play("bumper", 0.5f);
        Game.I.Rig.Shake(0.06f);
        StopAllCoroutines();
        StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.2f)
        {
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.25f);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
