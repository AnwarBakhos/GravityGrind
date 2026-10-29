using System.IO;
using UnityEngine;

// Saves and loads the levels you build in the level editor.
// Each level is a small text file (the same map format as Levels.cs) on this computer.
public static class CustomLevels
{
    public const int TabletopWidth = 28;
    public const int TabletopHeight = 16;
    public const int SideWidth = 160;
    public const int SideHeight = 16;

    public static string Id(LevelMode mode, int slot)
    {
        return "custom_" + (mode == LevelMode.Tabletop ? "t" : "s") + slot;
    }

    // In the Unity editor, levels are saved in a "CustomLevels" folder inside the project
    // so they get backed up with the project. In a built game they go in the normal save folder.
    static string Folder
    {
        get
        {
            string folder = Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "CustomLevels"))
                : Application.persistentDataPath;
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    static string FilePath(LevelMode mode, int slot)
    {
        return Path.Combine(Folder, Id(mode, slot) + ".txt");
    }

    // Where levels were saved before they moved into the project folder
    static string OldFilePath(LevelMode mode, int slot)
    {
        return Path.Combine(Application.persistentDataPath, Id(mode, slot) + ".txt");
    }

    // Copies any levels from the old save location into the project folder (keeps the old copy too)
    public static void MoveOldLevels()
    {
        for (int slot = 1; slot <= 3; slot++)
        {
            foreach (LevelMode mode in new[] { LevelMode.Tabletop, LevelMode.SideView })
            {
                string oldPath = OldFilePath(mode, slot);
                string newPath = FilePath(mode, slot);
                if (oldPath != newPath && File.Exists(oldPath) && !File.Exists(newPath)) File.Copy(oldPath, newPath);
            }
        }
    }

    public static bool Exists(LevelMode mode, int slot)
    {
        return File.Exists(FilePath(mode, slot));
    }

    public static string[] Load(LevelMode mode, int slot)
    {
        string path = FilePath(mode, slot);
        if (!File.Exists(path)) return null;
        return File.ReadAllText(path).Replace("\r", "").Split('\n');
    }

    public static void Save(LevelMode mode, int slot, string[] map)
    {
        string path = FilePath(mode, slot);
        string text = string.Join("\n", map);
        bool changed = !File.Exists(path) || File.ReadAllText(path).Replace("\r", "") != text;
        File.WriteAllText(path, text);

        // Only if the layout really changed: the old best time and ghost don't count anymore
        if (changed) SaveData.Clear(Id(mode, slot));
    }

    public static LevelInfo MakeInfo(LevelMode mode, int slot, string[] map)
    {
        return new LevelInfo
        {
            id = Id(mode, slot),
            name = "My " + (mode == LevelMode.Tabletop ? "Tabletop " : "Side View ") + slot,
            hint = "Your level - good luck!",
            mode = mode,
            map = map
        };
    }

    // A starting layout for a new level: an empty board with a start and a finish
    public static string[] Template(LevelMode mode)
    {
        if (mode == LevelMode.Tabletop)
        {
            string[] rows = new string[TabletopHeight];
            for (int y = 0; y < TabletopHeight; y++)
            {
                char[] row = new char[TabletopWidth];
                for (int x = 0; x < TabletopWidth; x++)
                {
                    bool edge = x == 0 || y == 0 || x == TabletopWidth - 1 || y == TabletopHeight - 1;
                    row[x] = edge ? '#' : '.';
                }
                rows[y] = new string(row);
            }
            rows[1] = "#S" + rows[1].Substring(2);
            rows[TabletopHeight - 2] = rows[TabletopHeight - 2].Substring(0, TabletopWidth - 2) + "F#";
            return rows;
        }
        else
        {
            string[] rows = new string[SideHeight];
            for (int y = 0; y < SideHeight; y++)
                rows[y] = new string(y >= SideHeight - 3 ? '#' : ' ', SideWidth);

            char[] startRow = rows[SideHeight - 4].ToCharArray();
            startRow[2] = 'S';
            startRow[SideWidth - 5] = 'F';
            rows[SideHeight - 4] = new string(startRow);
            return rows;
        }
    }
}
