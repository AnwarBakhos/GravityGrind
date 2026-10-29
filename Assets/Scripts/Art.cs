using System.Collections.Generic;
using UnityEngine;

// Loads pictures from Resources/Art and turns them into sprites of a chosen size.
public static class Art
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static Sprite white;

    // Loads a sprite so it is "unitsWide" world units wide.
    // Tiled sprites use a full rectangle mesh so they can repeat across a big area.
    public static Sprite Load(string path, float unitsWide = 1f, bool tiled = false)
    {
        string key = path + "|" + unitsWide + "|" + tiled;
        if (cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;

        Texture2D tex = Resources.Load<Texture2D>("Art/" + path);
        if (tex == null)
        {
            Debug.LogWarning("Missing art: " + path);
            return White;
        }

        float pixelsPerUnit = tex.width / unitsWide;
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                               pixelsPerUnit, 0, tiled ? SpriteMeshType.FullRect : SpriteMeshType.Tight);
        cache[key] = sprite;
        return sprite;
    }

    // A sprite for UI buttons that stretches without stretching its rounded corners.
    public static Sprite LoadSliced(string path, float border)
    {
        string key = path + "|sliced";
        if (cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;

        Texture2D tex = Resources.Load<Texture2D>("Art/" + path);
        if (tex == null) return White;

        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                               100f, 0, SpriteMeshType.FullRect, Vector4.one * border);
        cache[key] = sprite;
        return sprite;
    }

    // A plain white square (Resources/Art/white.png). Used for shadows, panels and particles.
    public static Sprite White
    {
        get
        {
            if (white == null) white = Load("white", 1f, true);
            return white;
        }
    }

    // Creates a GameObject with a SpriteRenderer at a position.
    public static SpriteRenderer Place(Transform parent, Sprite sprite, Vector2 position, int order, string name = "Sprite")
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }
}
