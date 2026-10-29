using UnityEngine;

// Background layer that follows the camera part of the way, so it looks far away.
public class Parallax : MonoBehaviour
{
    public float factor = 0.5f;
    float startX;

    void Start()
    {
        startX = transform.position.x;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 pos = transform.position;
        pos.x = startX + (cam.transform.position.x - startX) * factor;
        transform.position = pos;
    }
}
