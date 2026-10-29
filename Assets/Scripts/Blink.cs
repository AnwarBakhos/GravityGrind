using UnityEngine;
using UnityEngine.UI;

// Makes a UI text pulse, used for "NEW BEST!".
public class Blink : MonoBehaviour
{
    Text text;

    void Awake()
    {
        text = GetComponent<Text>();
    }

    void Update()
    {
        float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 8f) * 0.15f;
        transform.localScale = Vector3.one * pulse;
        Color c = text.color;
        text.color = new Color(c.r, c.g, c.b, 0.6f + pulse * 0.4f);
    }
}
