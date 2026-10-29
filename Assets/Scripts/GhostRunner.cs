using UnityEngine;

// Records the ball's path during a run and replays your best run as a see-through ghost ball.
public class GhostRunner : MonoBehaviour
{
    const float Step = 1f / 30f;

    GhostData best;
    readonly GhostData recording = new GhostData { step = Step };
    float nextSample;
    SpriteRenderer sr;
    LevelMode mode;

    public bool HasGhost => best != null && best.points.Count > 1;

    public static GhostRunner Create(LevelInfo level, Transform parent)
    {
        GameObject go = new GameObject("Ghost");
        go.transform.SetParent(parent, false);
        GhostRunner ghost = go.AddComponent<GhostRunner>();
        ghost.Init(level);
        return ghost;
    }

    void Init(LevelInfo level)
    {
        mode = level.mode;
        best = SaveData.LoadGhost(level.id);

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = mode == LevelMode.Tabletop ? Art.Load("Tabletop/ball_blue_large", 0.7f) : Art.Load("SideView/ball_blue", 0.8f);
        sr.color = new Color(1f, 1f, 1f, 0.45f);
        sr.sortingOrder = 9;
        sr.enabled = HasGhost;
        if (HasGhost) transform.position = best.points[0];
    }

    // Called every frame while the run is going, with the time since "GO!".
    public void Tick(float time, Vector2 ballPosition)
    {
        while (time >= nextSample)
        {
            recording.points.Add(ballPosition);
            nextSample += Step;
        }

        if (!HasGhost) return;

        float exact = time / best.step;
        int index = Mathf.FloorToInt(exact);
        int last = best.points.Count - 1;

        if (index >= last)
        {
            // The ghost finished: leave it at the goal and fade it out
            transform.position = best.points[last];
            sr.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(sr.color.a, 0f, Time.deltaTime));
            return;
        }

        Vector2 position = Vector2.Lerp(best.points[index], best.points[index + 1], exact - index);

        // Spin the side-view ghost as it rolls (skip big jumps like respawns)
        float dx = position.x - transform.position.x;
        if (mode == LevelMode.SideView && Mathf.Abs(dx) < 2f)
            transform.Rotate(0f, 0f, -dx / 0.4f * Mathf.Rad2Deg);

        transform.position = position;
    }

    public void SaveRun(string levelId)
    {
        SaveData.SaveGhost(levelId, recording);
    }
}
