using System.Collections.Generic;
using System.IO;
using UnityEngine;

// A recorded run: the ball's position sampled every "step" seconds.
[System.Serializable]
public class GhostData
{
    public float step;
    public List<Vector2> points = new List<Vector2>();
}

// Saves best times in PlayerPrefs and ghost runs as small JSON files.
public static class SaveData
{
    // Bump this when the course maps change so old ghosts don't replay on new layouts
    const string Version = "v2_";

    public static float GetBest(string levelId)
    {
        return PlayerPrefs.GetFloat(Version + "best_" + levelId, 0f);
    }

    public static void SetBest(string levelId, float time)
    {
        PlayerPrefs.SetFloat(Version + "best_" + levelId, time);
        PlayerPrefs.Save();
    }

    static string GhostPath(string levelId)
    {
        return Path.Combine(Application.persistentDataPath, Version + "ghost_" + levelId + ".json");
    }

    public static GhostData LoadGhost(string levelId)
    {
        string path = GhostPath(levelId);
        if (!File.Exists(path)) return null;

        try
        {
            return JsonUtility.FromJson<GhostData>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    public static void SaveGhost(string levelId, GhostData ghost)
    {
        File.WriteAllText(GhostPath(levelId), JsonUtility.ToJson(ghost));
    }

    // Forget the best time and ghost for one level
    public static void Clear(string levelId)
    {
        PlayerPrefs.DeleteKey(Version + "best_" + levelId);
        string path = GhostPath(levelId);
        if (File.Exists(path)) File.Delete(path);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        foreach (LevelInfo level in Levels.All)
        {
            PlayerPrefs.DeleteKey(Version + "best_" + level.id);
            string path = GhostPath(level.id);
            if (File.Exists(path)) File.Delete(path);
        }
        PlayerPrefs.Save();
    }
}
