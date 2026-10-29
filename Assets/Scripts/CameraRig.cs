using UnityEngine;

// Controls the camera.
// Tabletop: shows the whole board and leans with the tilt so the board looks tipped.
// Side view: follows the ball, looking ahead in the direction it's rolling.
// Level editor: a free camera you can scroll sideways.
// Both: shakes on big hits.
public class CameraRig : MonoBehaviour
{
    public float tiltLean = 7f;

    Camera cam;
    Transform target;
    Rigidbody2D targetBody;
    BallController targetBall;
    Rect bounds;
    bool following;
    bool free;
    Vector3 basePosition;
    Vector3 followVelocity;
    Vector2 tilt;
    float shake;
    Vector3 boardCenter;
    float boardDistance;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void SetupTabletop(Rect board, float margin = 1.8f, float lookOffsetY = 0f)
    {
        following = false;
        free = false;
        target = null;
        bounds = board;
        tilt = Vector2.zero;

        // Perspective camera so tilting it makes the board look like it's leaning
        cam.orthographic = false;
        cam.fieldOfView = 40f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.13f, 0.17f);

        float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float halfFovWide = Mathf.Atan(Mathf.Tan(halfFov) * cam.aspect);
        float fitHeight = (board.height / 2f + margin) / Mathf.Tan(halfFov);
        float fitWidth = (board.width / 2f + 1.2f) / Mathf.Tan(halfFovWide);
        boardDistance = Mathf.Max(fitHeight, fitWidth);
        boardCenter = new Vector3(board.center.x, board.center.y + lookOffsetY, 0f);
    }

    public void SetupSideView(Rect level, Transform follow)
    {
        following = true;
        free = false;
        target = follow;
        targetBody = follow.GetComponent<Rigidbody2D>();
        targetBall = follow.GetComponent<BallController>();
        bounds = level;
        tilt = Vector2.zero;

        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(223, 246, 245, 255);
        transform.rotation = Quaternion.identity;

        basePosition = ClampToLevel(follow.position);
        followVelocity = Vector3.zero;
        transform.position = basePosition;
    }

    // Side-view editor camera: no following, you scroll it with Pan()
    public void SetupEditor(Rect level, float size, float centerY)
    {
        following = false;
        free = true;
        target = null;
        bounds = level;
        tilt = Vector2.zero;

        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(223, 246, 245, 255);
        transform.rotation = Quaternion.identity;
        transform.position = new Vector3(0f, centerY, -10f);
        Pan(0f);
    }

    public void Pan(float amount)
    {
        float halfWidth = cam.orthographicSize * cam.aspect;
        float min = bounds.xMin + halfWidth - 2f;
        float max = Mathf.Max(min, bounds.xMax - halfWidth + 2f);
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x + amount, min, max);
        transform.position = pos;
    }

    public void SetTilt(Vector2 value)
    {
        tilt = value;
    }

    public void Shake(float amount)
    {
        shake = Mathf.Max(shake, amount);
    }

    void LateUpdate()
    {
        if (free) return;

        Vector3 shakeOffset = shake > 0f ? (Vector3)(Random.insideUnitCircle * shake) : Vector3.zero;
        shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 0.8f);

        if (following && target != null)
        {
            float lookAhead = targetBody != null ? Mathf.Clamp(targetBody.linearVelocity.x * 0.35f, -3f, 3f) : 0f;
            float above = targetBall != null ? 1.5f * targetBall.GravityDir : 1.5f;
            Vector3 goal = ClampToLevel(target.position + new Vector3(lookAhead, above, 0f));
            basePosition = Vector3.SmoothDamp(basePosition, goal, ref followVelocity, 0.15f);
            transform.position = basePosition + shakeOffset;
        }
        else if (!following)
        {
            // Lean the camera: tilting right makes the right side of the board look farther away
            Quaternion lean = Quaternion.Euler(-tilt.y * tiltLean, tilt.x * tiltLean, 0f);
            transform.rotation = lean;
            transform.position = boardCenter + lean * new Vector3(0f, 0f, -boardDistance) + shakeOffset;
        }
    }

    Vector3 ClampToLevel(Vector3 point)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float x = Mathf.Clamp(point.x, bounds.xMin + halfWidth, Mathf.Max(bounds.xMin + halfWidth, bounds.xMax - halfWidth));
        float y = Mathf.Clamp(point.y, bounds.yMin + halfHeight, Mathf.Max(bounds.yMin + halfHeight, bounds.yMax + 2f - halfHeight));
        return new Vector3(x, y, -10f);
    }
}
