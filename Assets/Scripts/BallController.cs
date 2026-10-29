using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// The player's ball.
// Tabletop: the mouse (or WASD) tilts the board and the ball rolls downhill.
// Side view: A/D roll the ball and Space jumps.
// Also handles falling into holes, dying on spikes, and the rolling / bump sounds.
public class BallController : MonoBehaviour
{
    [Header("Tabletop")]
    public float tiltStrength = 24f;
    public float tabletopMaxSpeed = 9f;

    [Header("Side view")]
    public float groundAccel = 24f;
    public float airAccel = 10f;
    public float maxRollSpeed = 10f;
    public float jumpSpeed = 10.5f;
    public float springSpeed = 17f;

    public LevelMode Mode { get; private set; }
    public Vector2 Tilt { get; private set; }
    public bool Busy => frozen || falling;
    public bool ControlsOn { get; set; } = true;
    public float GravityDir { get; private set; } = 1f;   // 1 = normal, -1 = upside down
    public bool CanTeleport => Time.time > teleportReadyTime;

    const float SideGravity = 2.2f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    SpriteRenderer shadow;
    bool frozen = true;
    bool falling;
    float killY;
    float killTop;
    float boostUntil;
    float teleportReadyTime;

    float moveInput;
    float lastGroundedTime = -10f;
    float jumpPressedTime = -10f;
    bool jumpHeld;
    bool cutJumpShort;
    Vector2 groundNormal = Vector2.up;
    float dustTimer;

    public static BallController Create(LevelMode mode, Vector2 start, Rect bounds, Transform parent)
    {
        GameObject go = new GameObject("Ball");
        go.transform.SetParent(parent, false);
        go.transform.position = start;
        BallController ball = go.AddComponent<BallController>();
        ball.Init(mode, bounds);
        return ball;
    }

    void Init(LevelMode mode, Rect bounds)
    {
        Mode = mode;
        killY = bounds.yMin - 3f;
        killTop = bounds.yMax + 3f;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.mass = 1f;

        if (mode == LevelMode.Tabletop)
        {
            // Seen from above: no gravity, the tilt pushes the ball instead
            sr.sprite = Art.Load("Tabletop/ball_red_large", 0.7f);
            circle.radius = 0.35f;
            circle.sharedMaterial = new PhysicsMaterial2D("Ball") { friction = 0f, bounciness = 0.3f };
            rb.gravityScale = 0f;
            rb.linearDamping = 0.6f;
            rb.freezeRotation = true;

            shadow = Art.Place(transform, sr.sprite, new Vector2(0.1f, -0.1f), 9, "Shadow");
            shadow.color = new Color(0f, 0f, 0f, 0.3f);
        }
        else
        {
            sr.sprite = Art.Load("SideView/ball_red", 0.8f);
            circle.radius = 0.4f;
            circle.sharedMaterial = new PhysicsMaterial2D("Ball") { friction = 0.9f, bounciness = 0.05f };
            rb.gravityScale = SideGravity;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.3f;
        }

        SetFrozen(true);
    }

    public void SetFrozen(bool value)
    {
        frozen = value;
        if (rb != null) rb.simulated = !frozen && !falling;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (frozen || falling || !ControlsOn)
        {
            moveInput = 0f;
            Tilt = Vector2.MoveTowards(Tilt, Vector2.zero, 3f * Time.deltaTime);
            if (frozen || falling) Sfx.SetRoll(0f, 1f);
            else UpdateSoundAndDust();
            return;
        }

        if (Mode == LevelMode.Tabletop) ReadTilt();
        else ReadRollAndJump();

        UpdateSoundAndDust();

        if (rb.position.y < killY || rb.position.y > killTop) Die(false);
    }

    void ReadTilt()
    {
        Vector2 target = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) target.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) target.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) target.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) target.y += 1f;
        }

        if (target != Vector2.zero)
        {
            target = target.normalized;
        }
        else if (Mouse.current != null && Camera.main != null)
        {
            // The board tilts toward the mouse. The farther the mouse is from the ball, the steeper the tilt.
            Vector2 mouse = Mouse.current.position.ReadValue();
            Vector2 ballOnScreen = Camera.main.WorldToScreenPoint(transform.position);
            Vector2 offset = (mouse - ballOnScreen) / (Screen.height * 0.25f);
            target = offset.magnitude < 0.08f ? Vector2.zero : Vector2.ClampMagnitude(offset, 1f);
        }

        Tilt = Vector2.MoveTowards(Tilt, target, 5f * Time.deltaTime);
    }

    void ReadRollAndJump()
    {
        moveInput = 0f;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput += 1f;

        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            jumpPressedTime = Time.time;

        jumpHeld = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;
    }

    void FixedUpdate()
    {
        if (frozen || falling) return;

        if (Mode == LevelMode.Tabletop)
        {
            rb.AddForce(Tilt * tiltStrength * rb.mass);
            // speed limit, except right after a boost pad or bumper
            if (Time.time > boostUntil && rb.linearVelocity.magnitude > tabletopMaxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * tabletopMaxSpeed;
            return;
        }

        bool grounded = IsGrounded();
        float accel = grounded ? groundAccel : airAccel;

        // Push toward the input direction, but stop adding speed past the max (slopes can still go faster)
        if (moveInput != 0f && rb.linearVelocity.x * moveInput < maxRollSpeed)
            rb.AddForce(new Vector2(moveInput * accel * rb.mass, 0f));

        // Jump buffering: a jump pressed just before landing still counts
        if (grounded && Time.time - jumpPressedTime < 0.12f) Jump();

        // Letting go of jump early gives a shorter hop
        float upSpeed = rb.linearVelocity.y * GravityDir;
        if (cutJumpShort && !jumpHeld && upSpeed > 2f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.55f);
            cutJumpShort = false;
        }
        if (upSpeed <= 0f) cutJumpShort = false;
    }

    bool IsGrounded()
    {
        // "Coyote time": still counts as grounded for a moment after rolling off an edge
        return Time.time - lastGroundedTime < 0.1f;
    }

    void Jump()
    {
        Vector2 up = new Vector2(0f, GravityDir);
        Vector2 direction = (groundNormal + up).normalized;
        Vector2 velocity = rb.linearVelocity;
        if (velocity.y * GravityDir < 0f) velocity.y = 0f;
        rb.linearVelocity = velocity + direction * jumpSpeed;

        jumpPressedTime = -10f;
        lastGroundedTime = -10f;
        cutJumpShort = true;
        Sfx.Play("jump", 0.5f);
        FX.Dust(rb.position - up * 0.4f, 6);
    }

    public void Bounce()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, springSpeed * GravityDir);
        lastGroundedTime = -10f;
        cutJumpShort = false;
        FX.Dust(rb.position - new Vector2(0f, 0.4f * GravityDir), 8);
    }

    // Boost pads: make sure we're going at least "speed" in that direction
    public void Boost(Vector2 direction, float speed)
    {
        Vector2 velocity = rb.linearVelocity;
        float along = Vector2.Dot(velocity, direction);
        if (along < speed) velocity += direction * (speed - along);
        rb.linearVelocity = velocity;
        boostUntil = Time.time + 0.8f;
    }

    // Bumpers: replace the velocity with a hard push
    public void Kick(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        boostUntil = Time.time + 0.4f;
    }

    public void Teleport(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        teleportReadyTime = Time.time + 0.5f;
    }

    // Flip gravity: 1 pulls down (normal), -1 pulls up to the ceiling
    public void SetGravity(float direction, bool quiet = false)
    {
        GravityDir = direction;
        if (Mode == LevelMode.SideView) rb.gravityScale = SideGravity * direction;
        lastGroundedTime = -10f;
        if (!quiet)
        {
            Sfx.Play(direction < 0f ? "gravity_up" : "gravity_down", 0.7f);
            FX.Burst(transform.position, new Color(0.75f, 0.5f, 1f), 10);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        CheckGround(collision);

        float hit = collision.relativeVelocity.magnitude;
        if (hit > 2f)
        {
            string sound = Mode == LevelMode.Tabletop ? "wood" : "soft";
            Sfx.Play(sound, Mathf.Clamp01(hit / 9f), Mathf.Lerp(1.15f, 0.85f, hit / 12f));
        }
        if (hit > (Mode == LevelMode.Tabletop ? 6f : 11f))
        {
            Game.I.Rig.Shake(0.12f);
            FX.Sparks(collision.GetContact(0).point, 6);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        CheckGround(collision);
    }

    void CheckGround(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;
            if (normal.y * GravityDir > 0.5f)
            {
                lastGroundedTime = Time.time;
                groundNormal = normal;
            }
        }
    }

    void UpdateSoundAndDust()
    {
        float speed = rb.linearVelocity.magnitude;
        bool touching = Mode == LevelMode.Tabletop || IsGrounded();

        float volume = touching ? Mathf.Clamp01(speed / 10f) * 0.6f : 0f;
        Sfx.SetRoll(volume, 0.6f + Mathf.Clamp01(speed / 12f));

        if (Mode == LevelMode.SideView && touching && speed > 5f)
        {
            dustTimer -= Time.deltaTime;
            if (dustTimer <= 0f)
            {
                dustTimer = 0.06f;
                FX.Dust(rb.position + new Vector2(-Mathf.Sign(rb.linearVelocity.x) * 0.3f, -0.35f * GravityDir), 1);
            }
        }

        // The shadow slides opposite the tilt so the board looks tipped
        if (shadow != null) shadow.transform.localPosition = new Vector2(0.1f, -0.1f) - Tilt * 0.08f;
    }

    // ------------------------------------------------------------------ Losing and finishing

    public void FallIntoHole(Vector2 holeCenter)
    {
        if (Busy) return;
        Sfx.Play("fall_hole", 0.8f);
        StartCoroutine(Sink(holeCenter, false));
    }

    public void SinkIntoFinish(Vector2 holeCenter)
    {
        if (Busy) return;
        StartCoroutine(Sink(holeCenter, true));
    }

    IEnumerator Sink(Vector2 center, bool finished)
    {
        falling = true;
        rb.simulated = false;
        Vector3 from = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            transform.position = Vector3.Lerp(from, center, t);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, t);
            yield return null;
        }

        sr.enabled = false;
        if (shadow != null) shadow.enabled = false;

        if (finished) Game.I.ReachFinish();
        else Game.I.BallLost();
    }

    // Spikes or falling off the bottom of the level
    public void Die(bool popped)
    {
        if (Busy) return;
        falling = true;
        rb.simulated = false;
        sr.enabled = false;

        if (popped)
        {
            Sfx.Play("spike", 0.8f);
            FX.Burst(transform.position, new Color(0.9f, 0.25f, 0.3f), 16);
            Game.I.Rig.Shake(0.2f);
        }
        else
        {
            Sfx.Play("fall_hole", 0.8f);
        }
        Game.I.BallLost();
    }

    public void Respawn(Vector2 position)
    {
        StopAllCoroutines();
        transform.position = position;
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
        rb.position = position;
        rb.rotation = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        Tilt = Vector2.zero;
        SetGravity(1f, true);

        sr.enabled = true;
        if (shadow != null) shadow.enabled = true;
        falling = false;
        rb.simulated = !frozen;

        Sfx.Play("respawn", 0.4f);
        FX.Burst(position, Color.white, 10);
    }
}
