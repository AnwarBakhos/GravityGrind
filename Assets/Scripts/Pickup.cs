using UnityEngine;

public enum PickupType { TimeBonus, Key }

// Stars and coins take a second off your time. Keys open every lock in the level.
public class Pickup : MonoBehaviour
{
    public PickupType type;

    // Where the pickup floats around. A shy key moves this when it runs away.
    public Vector3 Anchor { get; set; }

    float offset;

    void Awake()
    {
        Anchor = transform.localPosition;
        offset = Random.value * 6f;
    }

    void Update()
    {
        // gentle bob so pickups stand out
        transform.localPosition = Anchor + Vector3.up * Mathf.Sin(Time.time * 3f + offset) * 0.08f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<BallController>() == null) return;

        Game.I.Collect(this);
        Destroy(gameObject);
    }
}
