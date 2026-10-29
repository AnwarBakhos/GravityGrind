using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// In-game level editor for both course types.
// Pick a piece from the bar at the bottom, paint it on the grid with the mouse, then play-test or save.
// Levels are stored in the same text-map format as the built-in courses (see Levels.cs).
public class LevelEditor : MonoBehaviour
{
    // One piece you can place: its map character, a name, and how to draw its icon
    class Tool
    {
        public char c;
        public string name;
        public string icon;
        public float angle;
        public Vector2 scale = Vector2.one;
        public Color tint = Color.white;

        public Tool(char c, string name, string icon, float angle = 0f)
        {
            this.c = c;
            this.name = name;
            this.icon = icon;
            this.angle = angle;
        }
    }

    static readonly Color Pink = new Color(1f, 0.55f, 0.85f);
    static readonly Color Orange = new Color(1f, 0.65f, 0.3f);
    static readonly Color Green = new Color(0.6f, 1f, 0.5f);
    static readonly Color Moving = new Color(0.8f, 0.9f, 1f);

    static readonly Tool[] TabletopTools =
    {
        new Tool('#', "Wall", "Tabletop/block_small"),
        new Tool('1', "Corner Wedge", "Tabletop/block_corner", 0f),
        new Tool('2', "Corner Wedge", "Tabletop/block_corner", 90f),
        new Tool('3', "Corner Wedge", "Tabletop/block_corner", 180f),
        new Tool('4', "Corner Wedge", "Tabletop/block_corner", 270f),
        new Tool('O', "Hole", "Tabletop/hole"),
        new Tool('T', "Trapdoor (opens and closes)", "Tabletop/trapdoor"),
        new Tool('S', "Start", "Tabletop/hole_start"),
        new Tool('F', "Finish", "Tabletop/hole_large_end"),
        new Tool('C', "Checkpoint", "Tabletop/background_blue"),
        new Tool('*', "Star (-1 second)", "Tabletop/star"),
        new Tool('K', "Key", "Tabletop/key"),
        new Tool('k', "Shy Key (runs away!)", "Tabletop/key") { tint = Pink },
        new Tool('L', "Lock (opens with the key)", "Tabletop/block_locked_small"),
        new Tool('R', "Spinning Bar", "Tabletop/block_rotate_large"),
        new Tool('U', "Bumper", "Tabletop/bumper"),
        new Tool('>', "Boost Right", "Tabletop/boost", 0f),
        new Tool('<', "Boost Left", "Tabletop/boost", 180f),
        new Tool('^', "Boost Up", "Tabletop/boost", 90f),
        new Tool('v', "Boost Down", "Tabletop/boost", 270f),
        new Tool('P', "Portal A", "Tabletop/hole_large_end_alt"),
        new Tool('Q', "Portal A exit", "Tabletop/hole_large_end_alt") { tint = Orange },
        new Tool('p', "Portal B", "Tabletop/hole_large_end_alt") { tint = Green },
        new Tool('q', "Portal B exit", "Tabletop/hole_large_end_alt") { tint = Pink },
        new Tool('.', "Eraser", null),
    };

    static readonly Tool[] SideTools =
    {
        new Tool('#', "Ground", "SideView/tile_0022"),
        new Tool('I', "Ice", "SideView/ice_top"),
        new Tool('/', "Steep Slope Up", "SideView/slope_up"),
        new Tool('\\', "Steep Slope Down", "SideView/slope_down"),
        new Tool('a', "Gentle Slope Up (part 1)", "SideView/slope_gentle_up_a"),
        new Tool('b', "Gentle Slope Up (part 2)", "SideView/slope_gentle_up_b"),
        new Tool('c', "Gentle Slope Down (part 1)", "SideView/slope_gentle_down_a"),
        new Tool('d', "Gentle Slope Down (part 2)", "SideView/slope_gentle_down_b"),
        new Tool('=', "One-way Platform", "SideView/tile_0002"),
        new Tool('M', "Moving Platform", "SideView/tile_0002") { tint = Moving },
        new Tool('N', "Elevator", "SideView/tile_0002") { tint = Moving, scale = new Vector2(0.7f, 1f) },
        new Tool('X', "Crumbling Block", "SideView/tile_0004"),
        new Tool('B', "Crate", "SideView/tile_0026"),
        new Tool('^', "Spikes", "SideView/tile_0068"),
        new Tool('v', "Ceiling Spikes", "SideView/tile_0068") { scale = new Vector2(1f, -1f) },
        new Tool('J', "Spring", "SideView/tile_0107"),
        new Tool('>', "Boost Right", "SideView/boost_pad"),
        new Tool('<', "Boost Left", "SideView/boost_pad") { scale = new Vector2(-1f, 1f) },
        new Tool('G', "Gravity Flip Up", "SideView/gravity_up"),
        new Tool('g', "Gravity Flip Down", "SideView/gravity_down"),
        new Tool('S', "Start", "SideView/ball_red"),
        new Tool('F', "Finish", "SideView/tile_0112"),
        new Tool('C', "Checkpoint", "SideView/tile_0111"),
        new Tool('*', "Coin (-1 second)", "SideView/tile_0151"),
        new Tool('K', "Key", "SideView/tile_0027"),
        new Tool('k', "Shy Key (runs away!)", "SideView/tile_0027") { tint = Pink },
        new Tool('L', "Lock (opens with the key)", "SideView/tile_0028"),
        new Tool(' ', "Eraser", null),
    };

    LevelMode mode;
    int slot;
    char[,] grid;
    int width, height;
    Tool[] tools;
    int selected;
    bool isOpen;

    BuiltLevel preview;
    bool needsRebuild;
    float rebuildTime;
    float clearConfirmTime = -10f;

    GameObject ui;
    Text statusText, toolText;
    readonly List<Image> toolButtons = new List<Image>();
    SpriteRenderer cursor, cursorIcon, gridLines;

    char Empty => mode == LevelMode.Tabletop ? '.' : ' ';

    // ------------------------------------------------------------------ Opening and closing

    public void Open(LevelMode levelMode, int levelSlot)
    {
        mode = levelMode;
        slot = levelSlot;
        LoadGrid(CustomLevels.Load(mode, slot) ?? CustomLevels.Template(mode));
        selected = 0;
        Show();
    }

    // Coming back after a play-test: keep the same grid
    public void Reopen()
    {
        Show();
    }

    void Show()
    {
        isOpen = true;
        tools = mode == LevelMode.Tabletop ? TabletopTools : SideTools;
        BuildUI();
        BuildCursor();
        Rebuild();

        Rect area = new Rect(0, 0, width, height);
        if (mode == LevelMode.Tabletop) Game.I.Rig.SetupTabletop(area, 5f, -1.1f);
        else Game.I.Rig.SetupEditor(area, 11f, 7f);

        SetStatus("Left click: place     Right click: erase     Q / E: change piece" +
                  (mode == LevelMode.SideView ? "     A / D or mouse wheel: scroll" : ""));
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (preview != null) Destroy(preview.root);
        if (ui != null) Destroy(ui);
        if (cursor != null) Destroy(cursor.gameObject);
        if (gridLines != null) Destroy(gridLines.gameObject);
        preview = null;
    }

    void LoadGrid(string[] map)
    {
        width = mode == LevelMode.Tabletop ? CustomLevels.TabletopWidth : CustomLevels.SideWidth;
        height = mode == LevelMode.Tabletop ? CustomLevels.TabletopHeight : CustomLevels.SideHeight;
        grid = new char[width, height];

        for (int y = 0; y < height; y++)
        {
            string row = y < map.Length ? map[y] : "";
            for (int x = 0; x < width; x++)
                grid[x, y] = x < row.Length ? row[x] : (mode == LevelMode.Tabletop ? '.' : ' ');
        }
    }

    string[] ToMap()
    {
        string[] rows = new string[height];
        for (int y = 0; y < height; y++)
        {
            char[] row = new char[width];
            for (int x = 0; x < width; x++) row[x] = grid[x, y];
            rows[y] = new string(row);
        }
        return rows;
    }

    // ------------------------------------------------------------------ Every frame

    void Update()
    {
        if (!isOpen || Game.I.State != GameState.Editor) return;

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (kb != null)
        {
            if (kb.qKey.wasPressedThisFrame) Select(selected - 1);
            if (kb.eKey.wasPressedThisFrame) Select(selected + 1);
            if (kb.escapeKey.wasPressedThisFrame)
            {
                Back();
                return;
            }

            if (mode == LevelMode.SideView)
            {
                float pan = 0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) pan -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) pan += 1f;
                if (pan != 0f) Game.I.Rig.Pan(pan * 20f * Time.deltaTime);
            }
        }

        if (mouse != null)
        {
            if (mode == LevelMode.SideView)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0f) Game.I.Rig.Pan(Mathf.Sign(scroll) * 2f);
            }
            HandleMouse(mouse);
        }

        if (needsRebuild && Time.unscaledTime >= rebuildTime) Rebuild();
    }

    void HandleMouse(Mouse mouse)
    {
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (overUI || !MouseToCell(mouse, out int x, out int y))
        {
            cursor.gameObject.SetActive(false);
            return;
        }

        cursor.gameObject.SetActive(true);
        cursor.transform.position = Cell(x, y);

        // (wasPressedThisFrame also catches very quick clicks)
        if (mouse.leftButton.isPressed || mouse.leftButton.wasPressedThisFrame) Paint(x, y, tools[selected].c);
        else if (mouse.rightButton.isPressed || mouse.rightButton.wasPressedThisFrame) Paint(x, y, Empty);

        // Finish the stroke right away when the mouse is let go
        if ((mouse.leftButton.wasReleasedThisFrame || mouse.rightButton.wasReleasedThisFrame) && needsRebuild) Rebuild();
    }

    // Turns the mouse position into a grid cell by finding where it points on the level (z = 0)
    bool MouseToCell(Mouse mouse, out int x, out int y)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        float distance = -ray.origin.z / ray.direction.z;
        Vector3 point = ray.origin + ray.direction * distance;

        x = Mathf.FloorToInt(point.x);
        y = height - 1 - Mathf.FloorToInt(point.y);
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    Vector2 Cell(int x, int y)
    {
        return new Vector2(x + 0.5f, height - 1 - y + 0.5f);
    }

    void Paint(int x, int y, char c)
    {
        // The outer walls of a tabletop board always stay
        if (mode == LevelMode.Tabletop && (x == 0 || y == 0 || x == width - 1 || y == height - 1)) return;
        if (grid[x, y] == c) return;

        // Only one start, one finish and one of each portal
        if ("SFPQpq".IndexOf(c) >= 0)
            for (int yy = 0; yy < height; yy++)
                for (int xx = 0; xx < width; xx++)
                    if (grid[xx, yy] == c) grid[xx, yy] = Empty;

        grid[x, y] = c;
        needsRebuild = true;
        rebuildTime = Time.unscaledTime + 0.15f;
    }

    // Rebuild the level from the grid so you see exactly what you'll play
    void Rebuild()
    {
        needsRebuild = false;
        if (preview != null) Destroy(preview.root);

        preview = LevelBuilder.Build(CustomLevels.MakeInfo(mode, slot, ToMap()));
        FX.Parent = preview.root.transform;

        // Side-view starts have no tile of their own, so show the ball there
        if (mode == LevelMode.SideView)
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (grid[x, y] == 'S')
                        Art.Place(preview.root.transform, Art.Load("SideView/ball_red", 0.8f), Cell(x, y), 10, "Start Marker");
    }

    // ------------------------------------------------------------------ Buttons

    void PlayTest()
    {
        if (!Check()) return;
        CustomLevels.Save(mode, slot, ToMap());
        Game.I.TestFromEditor(CustomLevels.MakeInfo(mode, slot, ToMap()));
    }

    void Save()
    {
        CustomLevels.Save(mode, slot, ToMap());
        Sfx.Play("checkpoint", 0.6f);
        SetStatus(Check(false) ? "Saved!" : "Saved - but it still needs a Start and a Finish to be playable.");
    }

    void ClearAll()
    {
        // Ask twice so one click can't wipe your work
        if (Time.unscaledTime - clearConfirmTime > 3f)
        {
            clearConfirmTime = Time.unscaledTime;
            SetStatus("Click Clear again to erase the whole level.");
            return;
        }
        LoadGrid(CustomLevels.Template(mode));
        Rebuild();
        SetStatus("Level cleared.");
    }

    void Back()
    {
        CustomLevels.Save(mode, slot, ToMap());
        Close();
        Game.I.ShowMenu();
        Game.I.UI.ShowCustomMenu();
    }

    // A level needs exactly one start and a finish
    bool Check(bool showMessage = true)
    {
        int starts = 0, finishes = 0;
        foreach (char c in grid)
        {
            if (c == 'S') starts++;
            if (c == 'F') finishes++;
        }
        bool ok = starts == 1 && finishes >= 1;
        if (!ok && showMessage)
        {
            SetStatus("Your level needs a Start (S) and a Finish (F) before you can play it.");
            Sfx.Play("ui_back", 0.7f);
        }
        return ok;
    }

    void Select(int index)
    {
        selected = (index + tools.Length) % tools.Length;
        RefreshTools();
    }

    void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // ------------------------------------------------------------------ Building the editor screen

    void BuildUI()
    {
        if (ui != null) Destroy(ui);
        toolButtons.Clear();

        UIManager u = Game.I.UI;
        ui = u.MakeLayer("Editor UI");
        Transform t = ui.transform;

        // Top bar
        u.MakePanel(t, new Color(0f, 0f, 0f, 0.6f), new Vector2(0, 495), new Vector2(4000, 90));
        string title = "Editing: " + (mode == LevelMode.Tabletop ? "Tabletop" : "Side View") + " Slot " + slot;
        u.MakeText(t, title, 40, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(330, -45), new Vector2(620, 60), Color.white);
        u.MakeButton(t, "Play Test", new Vector2(230, 495), new Vector2(230, 70), PlayTest, 32);
        u.MakeButton(t, "Save", new Vector2(450, 495), new Vector2(180, 70), Save, 32);
        u.MakeButton(t, "Clear", new Vector2(650, 495), new Vector2(180, 70), ClearAll, 32);
        u.MakeButton(t, "Back", new Vector2(850, 495), new Vector2(180, 70), Back, 32);
        u.MakePanel(t, new Color(0f, 0f, 0f, 0.45f), new Vector2(0, 420), new Vector2(1900, 50));
        statusText = u.MakeText(t, "", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1800, 50), Color.white);

        // Piece bar along the bottom, in two rows
        u.MakePanel(t, new Color(0f, 0f, 0f, 0.6f), new Vector2(0, -450), new Vector2(4000, 200));
        toolText = u.MakeText(t, "", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(1200, 50), new Color(1f, 0.82f, 0.25f));

        int perRow = Mathf.CeilToInt(tools.Length / 2f);
        for (int i = 0; i < tools.Length; i++)
        {
            int index = i;
            Tool tool = tools[i];
            int row = i / perRow;
            int column = i % perRow;
            Vector2 position = new Vector2((column - (perRow - 1) / 2f) * 86f, -405f - row * 86f);

            Button button = u.MakeButton(t, tool.icon == null ? "Erase" : "", position, new Vector2(78, 78), () => Select(index), 20);
            if (tool.icon != null)
            {
                Image icon = u.MakeImage(button.transform, Art.Load(tool.icon), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56));
                icon.color = tool.tint;
                icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tool.angle);
                icon.rectTransform.localScale = new Vector3(tool.scale.x, tool.scale.y, 1f);
            }
            toolButtons.Add(button.GetComponent<Image>());
        }
        RefreshTools();
    }

    void RefreshTools()
    {
        for (int i = 0; i < toolButtons.Count; i++)
            toolButtons[i].color = i == selected ? Color.white : new Color(0.5f, 0.5f, 0.55f);

        Tool tool = tools[selected];
        if (toolText != null) toolText.text = tool.name + "   (Q / E to change)";

        if (cursorIcon != null)
        {
            cursorIcon.sprite = tool.icon != null ? Art.Load(tool.icon) : null;
            cursorIcon.color = new Color(tool.tint.r, tool.tint.g, tool.tint.b, 0.6f);
            cursorIcon.transform.localRotation = Quaternion.Euler(0f, 0f, tool.angle);
            cursorIcon.transform.localScale = new Vector3(tool.scale.x, tool.scale.y, 1f);
        }
    }

    // The highlighted square under the mouse, a preview of the piece, and faint grid lines
    void BuildCursor()
    {
        if (cursor != null) Destroy(cursor.gameObject);
        if (gridLines != null) Destroy(gridLines.gameObject);

        cursor = Art.Place(null, Art.White, Vector2.zero, 40, "Editor Cursor");
        cursor.color = new Color(1f, 1f, 1f, 0.25f);
        cursorIcon = Art.Place(cursor.transform, null, Vector2.zero, 41, "Cursor Piece");

        gridLines = Art.Place(null, GridSprite(), new Vector2(width / 2f, height / 2f), 35, "Editor Grid");
        gridLines.drawMode = SpriteDrawMode.Tiled;
        gridLines.size = new Vector2(width, height);
        RefreshTools();
    }

    static Sprite gridSprite;

    static Sprite GridSprite()
    {
        if (gridSprite != null) return gridSprite;

        Texture2D tex = new Texture2D(32, 32);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color line = new Color(1f, 1f, 1f, 0.18f);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                tex.SetPixel(x, y, x == 0 || y == 0 ? line : clear);
        tex.Apply();

        gridSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
        return gridSprite;
    }
}
