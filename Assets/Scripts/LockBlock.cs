using System.Collections;
using UnityEngine;

// A gold block that disappears once the key has been picked up.
public class LockBlock : MonoBehaviour
{
    public void Open()
    {
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(Vanish());
    }

    IEnumerator Vanish()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            transform.localScale = Vector3.one * (1f + t * 0.4f);
            sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        Destroy(gameObject);
    }
}
