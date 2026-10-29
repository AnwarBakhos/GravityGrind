using System.Collections.Generic;
using UnityEngine;

// Everything the game needs to know about a level after it is built.
public class BuiltLevel
{
    public GameObject root;
    public Vector2 start;
    public Rect bounds;
    public int keyCount;
    public List<LockBlock> locks = new List<LockBlock>();
}

// Reads the text maps in Levels.cs and builds the tiles, colliders, pickups and hazards.
public static class LevelBuilder
{
    static string[] map;
    static int width, height;

    public static BuiltLevel Build(LevelInfo info)
    {
        map = info.map;
        height = map.Length;
        width = 0;
        foreach (string row in map) width = Mathf.Max(width, row.Length);

        BuiltLevel level = new BuiltLevel();
        level.root = new GameObject("Level - " + info.name);
        level.bounds = new Rect(0, 0, width, height);

        if (info.mode == LevelMode.Tabletop) BuildTabletop(level);
        else BuildSideView(level);
        return level;
    }

    // Character at a map cell. Row 0 is the top line of the map.
    static char At(int x, int y)
    {
        if (x < 0 || y < 0 || y >= height) return ' ';
        string row = map[y];
        return x < row.Length ? row[x] : ' ';
    }

    // World position of the middle of a map cell (y is flipped so the map reads top to bottom).
    static Vector2 Cell(int x, int y)
    {
        return new Vector2(x + 0.5f, height - 1 - y + 0.5f);
    }

    // ------------------------------------------------------------------ Tabletop

    static void BuildTabletop(BuiltLevel level)
    {
        Transform root = level.root.transform;

        // Wooden board with a soft shadow under it
        SpriteRenderer shadow = Art.Place(root, Art.White, new Vector2(width / 2f + 0.3f, height / 2f - 0.3f), -21, "Board Shadow");
        shadow.transform.localScale = new Vector3(width + 0.4f, height + 0.4f, 1f);
        shadow.color = new Color(0f, 0f, 0f, 0.45f);

        SpriteRenderer floor = Art.Place(root, Art.Load("Tabletop/background_brown", 2f, true), new Vector2(width / 2f, height / 2f), -20, "Floor");
        floor.drawMode = SpriteDrawMode.Tiled;
        floor.size = new Vector2(width, height);

        GameObject walls = MakeSolidGroup(root, "Walls", MakeMaterial("Wood", 0f, 0.35f));
        Dictionary<char, Portal> portals = new Dictionary<char, Portal>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = At(x, y);
                Vector2 p = Cell(x, y);

                switch (c)
                {
                    case '#':
                        AddBox(walls, p, Vector2.one);
                        PlaceWithShadow(root, Art.Load("Tabletop/block_small"), p, 0f);
                        break;

                    case '1': case '2': case '3': case '4':
                        AddWedge(root, walls, p, (c - '1') * 90f);
                        break;

                    case 'O':
                        Art.Place(root, Art.Load("Tabletop/hole", 0.95f), p, -5, "Hole");
                        AddTrigger<Hole>(root, "Hole Trigger", p, 0.45f);
                        break;

                    case 'S':
                        level.start = p;
                        Art.Place(root, Art.Load("Tabletop/hole_start", 0.85f), p, -5, "Start");
                        break;

                    case 'F':
                        Art.Place(root, Art.Load("Tabletop/hole_large_end", 1.3f), p, -5, "Finish");
                        AddTrigger<FinishZone>(root, "Finish Trigger", p, 0.6f).sinkInto = true;
                        break;

                    case 'C':
                        SpriteRenderer pad = Art.Place(root, Art.Load("Tabletop/background_blue", 0.9f), p, -6, "Checkpoint");
                        AddTrigger<Checkpoint>(root, "Checkpoint Trigger", p, 0.45f).art = pad;
                        break;

                    case '*':
                        AddPickup(root, p, Art.Load("Tabletop/star", 0.6f), PickupType.TimeBonus);
                        break;

                    case 'K': case 'k':
                        Pickup tableKey = AddPickup(root, p, Art.Load("Tabletop/key", 0.7f), PickupType.Key);
                        if (c == 'k') MakeShy(tableKey);
                        level.keyCount++;
                        break;

                    case 'L':
                        level.locks.Add(AddLock(root, p, Art.Load("Tabletop/block_locked_small")));
                        break;

                    case 'R':
                        AddSpinner(root, p, (x + y) % 2 == 0 ? 100f : -100f);
                        break;

                    case '>': case '<': case '^': case 'v':
                        AddTabletopBoost(root, p, c);
                        break;

                    case 'P': case 'Q': case 'p': case 'q':
                        portals[c] = AddPortal(root, p, c);
                        break;

                    case 'U':
                        AddBumper(root, p);
                        break;

                    case 'T':
                        AddTrapdoor(root, p, x * 0.37f + y * 0.21f);
                        break;
                }
            }
        }

        // Link each portal to its partner: P <-> Q and p <-> q
        LinkPortals(portals, 'P', 'Q');
        LinkPortals(portals, 'p', 'q');

        walls.GetComponent<CompositeCollider2D>().GenerateGeometry();
    }

    static void AddTabletopBoost(Transform root, Vector2 p, char c)
    {
        Vector2 direction = c == '>' ? Vector2.right : c == '<' ? Vector2.left : c == '^' ? Vector2.up : Vector2.down;
        SpriteRenderer pad = Art.Place(root, Art.Load("Tabletop/boost", 0.9f), p, -6, "Boost Pad");
        pad.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        BoostPad boost = AddTrigger<BoostPad>(root, "Boost Trigger", p, 0.4f);
        boost.direction = direction;
        boost.speed = 13f;
    }

    static Portal AddPortal(Transform root, Vector2 p, char c)
    {
        bool second = c == 'Q' || c == 'q';
        bool otherPair = c == 'p' || c == 'q';
        SpriteRenderer art = Art.Place(root, Art.Load("Tabletop/hole_large_end_alt", 1.1f), p, -5, "Portal");
        art.color = otherPair ? (second ? new Color(1f, 0.5f, 0.9f) : new Color(0.6f, 1f, 0.5f))
                              : (second ? new Color(1f, 0.65f, 0.3f) : Color.white);
        Portal portal = AddTrigger<Portal>(root, "Portal Trigger", p, 0.35f);
        portal.art = art.transform;
        return portal;
    }

    static void LinkPortals(Dictionary<char, Portal> portals, char a, char b)
    {
        if (portals.TryGetValue(a, out Portal first) && portals.TryGetValue(b, out Portal second))
        {
            first.partner = second;
            second.partner = first;
        }
    }

    static void AddBumper(Transform root, Vector2 p)
    {
        SpriteRenderer sr = Art.Place(root, Art.Load("Tabletop/bumper", 0.9f), p, 1, "Bumper");
        CircleCollider2D circle = sr.gameObject.AddComponent<CircleCollider2D>();
        circle.radius = 0.45f;
        sr.gameObject.AddComponent<Bumper>();
    }

    static void AddTrapdoor(Transform root, Vector2 p, float offset)
    {
        Art.Place(root, Art.Load("Tabletop/hole", 0.95f), p, -5, "Trapdoor Hole");
        SpriteRenderer cover = Art.Place(root, Art.Load("Tabletop/trapdoor", 0.95f), p, -4, "Trapdoor Cover");
        Hole hole = AddTrigger<Hole>(root, "Trapdoor Trigger", p, 0.45f);
        hole.trapdoor = true;
        hole.cover = cover;
        hole.offset = offset;
    }

    static void PlaceWithShadow(Transform root, Sprite sprite, Vector2 p, float angle)
    {
        SpriteRenderer shadow = Art.Place(root, sprite, p + new Vector2(0.08f, -0.08f), -2, "Shadow");
        shadow.color = new Color(0f, 0f, 0f, 0.3f);
        shadow.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        SpriteRenderer sr = Art.Place(root, sprite, p, 0, "Block");
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // A triangle block in a corner. With no rotation the square corner is at the bottom-left.
    static void AddWedge(Transform root, GameObject walls, Vector2 p, float angle)
    {
        Quaternion turn = Quaternion.Euler(0f, 0f, angle);
        Vector2 a = turn * new Vector2(-0.5f, -0.5f);
        Vector2 b = turn * new Vector2(0.5f, -0.5f);
        Vector2 c = turn * new Vector2(-0.5f, 0.5f);
        AddPoly(walls, p + a, p + b, p + c);
        PlaceWithShadow(root, Art.Load("Tabletop/block_corner"), p, angle);
    }

    static void AddSpinner(Transform root, Vector2 p, float speed)
    {
        GameObject go = new GameObject("Spinner");
        go.transform.SetParent(root, false);
        go.transform.localPosition = p;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Art.Load("Tabletop/block_rotate_large", 1f);
        sr.sortingOrder = 1;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 1.9f);
        box.sharedMaterial = MakeMaterial("Spinner", 0f, 0.5f);

        go.AddComponent<Spinner>().speed = speed;
    }

    // ------------------------------------------------------------------ Side view

    static bool IsSolid(char c)
    {
        return c == '#' || c == 'I' || c == '/' || c == '\\' || c == 'a' || c == 'b' || c == 'c' || c == 'd';
    }

    static void BuildSideView(BuiltLevel level)
    {
        Transform root = level.root.transform;
        BuildBackground(root);

        GameObject ground = MakeSolidGroup(root, "Ground", MakeMaterial("Grass", 0.9f, 0f));
        GameObject ice = MakeSolidGroup(root, "Ice", MakeMaterial("Ice", 0.001f, 0f));

        // Invisible walls at both ends so the ball can't roll off the sides
        AddBox(ground, new Vector2(-0.5f, height), new Vector2(1f, height * 3f));
        AddBox(ground, new Vector2(width + 0.5f, height), new Vector2(1f, height * 3f));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = At(x, y);
                Vector2 p = Cell(x, y);
                Vector2 bl = p + new Vector2(-0.5f, -0.5f);

                switch (c)
                {
                    case '#':
                        AddBox(ground, p, Vector2.one);
                        SpriteRenderer tile = Art.Place(root, Art.Load("SideView/" + GroundTile(x, y)), p, 0, "Ground");
                        // Ceilings: grass on the underside so upside-down sections look right
                        tile.flipY = y < height - 1 && IsSolid(At(x, y - 1)) && !IsSolid(At(x, y + 1)) && At(x, y + 1) != '=';
                        if (tile.flipY) tile.sprite = Art.Load("SideView/tile_0022");
                        break;

                    case 'I':
                        AddBox(ice, p, Vector2.one);
                        string iceTile = IsSolid(At(x, y - 1)) ? "tile_0122" : "ice_top";
                        Art.Place(root, Art.Load("SideView/" + iceTile), p, 0, "Ice");
                        break;

                    case '/':
                        AddPoly(ground, bl, bl + new Vector2(1, 0), bl + new Vector2(1, 1));
                        Art.Place(root, Art.Load("SideView/slope_up"), p, 0, "Slope");
                        break;

                    case '\\':
                        AddPoly(ground, bl, bl + new Vector2(1, 0), bl + new Vector2(0, 1));
                        Art.Place(root, Art.Load("SideView/slope_down"), p, 0, "Slope");
                        break;

                    case 'a':
                        AddPoly(ground, bl, bl + new Vector2(1, 0), bl + new Vector2(1, 0.5f));
                        Art.Place(root, Art.Load("SideView/slope_gentle_up_a"), p, 0, "Slope");
                        break;

                    case 'b':
                        AddPoly(ground, bl, bl + new Vector2(1, 0), bl + new Vector2(1, 1), bl + new Vector2(0, 0.5f));
                        Art.Place(root, Art.Load("SideView/slope_gentle_up_b"), p, 0, "Slope");
                        break;

                    case 'c':
                        AddPoly(ground, bl, bl + new Vector2(1, 0), bl + new Vector2(1, 0.5f), bl + new Vector2(0, 1));
                        Art.Place(root, Art.Load("SideView/slope_gentle_down_a"), p, 0, "Slope");
                        break;

                    case 'd':
                        AddPoly(ground, bl, bl + new Vector2(1, 0), bl + new Vector2(0, 0.5f));
                        Art.Place(root, Art.Load("SideView/slope_gentle_down_b"), p, 0, "Slope");
                        break;

                    case '=':
                        if (At(x - 1, y) != '=') AddPlatform(root, x, y);
                        break;

                    case '^':
                        Art.Place(root, Art.Load("SideView/tile_0068"), p, 2, "Spikes");
                        AddBoxTrigger<Hazard>(root, "Spike Trigger", p + new Vector2(0f, -0.25f), new Vector2(0.75f, 0.4f));
                        break;

                    case 'v':
                        SpriteRenderer ceilingSpikes = Art.Place(root, Art.Load("SideView/tile_0068"), p, 2, "Ceiling Spikes");
                        ceilingSpikes.flipY = true;
                        AddBoxTrigger<Hazard>(root, "Spike Trigger", p + new Vector2(0f, 0.25f), new Vector2(0.75f, 0.4f));
                        break;

                    case '>': case '<':
                        SpriteRenderer boostArt = Art.Place(root, Art.Load("SideView/boost_pad"), p, 2, "Boost Pad");
                        boostArt.flipX = c == '<';
                        BoostPad boost = AddBoxTrigger<BoostPad>(root, "Boost Trigger", p + new Vector2(0f, -0.2f), new Vector2(0.9f, 0.7f));
                        boost.direction = c == '>' ? Vector2.right : Vector2.left;
                        boost.speed = 16f;
                        break;

                    case 'M': case 'N':
                        if (At(x - 1, y) != c) AddMovingPlatform(root, x, y, c);
                        break;

                    case 'X':
                        SpriteRenderer crumble = Art.Place(root, Art.Load("SideView/tile_0004"), p, 1, "Crumble Block");
                        crumble.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
                        crumble.gameObject.GetComponent<BoxCollider2D>().sharedMaterial = MakeMaterial("Grass", 0.9f, 0f);
                        crumble.gameObject.AddComponent<CrumbleBlock>();
                        break;

                    case 'G': case 'g':
                        SpriteRenderer field = Art.Place(root, Art.Load(c == 'G' ? "SideView/gravity_up" : "SideView/gravity_down"), p, -1, "Gravity Field");
                        GravityZone zone = AddBoxTrigger<GravityZone>(root, "Gravity Trigger", p, new Vector2(0.6f, 1f));
                        zone.direction = c == 'G' ? -1f : 1f;
                        zone.art = field;
                        break;

                    case 'J':
                        SpriteRenderer springArt = Art.Place(root, Art.Load("SideView/tile_0107"), p, 2, "Spring");
                        AddBoxTrigger<Spring>(root, "Spring Trigger", p + new Vector2(0f, -0.2f), new Vector2(0.9f, 0.5f)).art = springArt;
                        break;

                    case 'C':
                        SpriteRenderer flag = Art.Place(root, Art.Load("SideView/tile_0111"), p, 2, "Checkpoint");
                        AddBoxTrigger<Checkpoint>(root, "Checkpoint Trigger", p + new Vector2(0f, 1f), new Vector2(1f, 3f)).art = flag;
                        break;

                    case 'F':
                        SpriteRenderer finish = Art.Place(root, Art.Load("SideView/tile_0112", 1.6f), p + new Vector2(0f, 0.3f), 2, "Finish");
                        finish.color = new Color(1f, 0.85f, 0.3f);
                        AddBoxTrigger<FinishZone>(root, "Finish Trigger", p + new Vector2(0f, 2.5f), new Vector2(1.5f, 6f));
                        break;

                    case '*':
                        AddPickup(root, p, Art.Load("SideView/tile_0151", 0.9f), PickupType.TimeBonus);
                        break;

                    case 'K': case 'k':
                        Pickup sideKey = AddPickup(root, p, Art.Load("SideView/tile_0027"), PickupType.Key);
                        if (c == 'k') MakeShy(sideKey);
                        level.keyCount++;
                        break;

                    case 'L':
                        level.locks.Add(AddLock(root, p, Art.Load("SideView/tile_0028")));
                        break;

                    case 'B':
                        AddCrate(root, p);
                        break;

                    case 'S':
                        level.start = p;
                        break;
                }
            }
        }

        ground.GetComponent<CompositeCollider2D>().GenerateGeometry();
        ice.GetComponent<CompositeCollider2D>().GenerateGeometry();
    }

    // Picks the right grass tile: grass on top, plain dirt underneath, edge tiles at the ends.
    static string GroundTile(int x, int y)
    {
        if (IsSolid(At(x, y - 1))) return "tile_0122";
        bool left = IsSolid(At(x - 1, y));
        bool right = IsSolid(At(x + 1, y));
        if (left && right) return "tile_0022";
        if (right) return "tile_0021";
        if (left) return "tile_0023";
        return "tile_0020";
    }

    // A one-way platform you can jump up through and land on.
    static void AddPlatform(Transform root, int startX, int y)
    {
        int length = 0;
        while (At(startX + length, y) == '=') length++;

        GameObject go = new GameObject("Platform");
        go.transform.SetParent(root, false);
        Vector2 left = Cell(startX, y);
        go.transform.localPosition = new Vector2(left.x - 0.5f + length / 2f, left.y);

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(length, 0.3f);
        box.offset = new Vector2(0f, 0.35f);
        box.usedByEffector = true;
        box.sharedMaterial = MakeMaterial("Grass", 0.9f, 0f);

        PlatformEffector2D effector = go.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 160f;

        for (int i = 0; i < length; i++)
        {
            string tile = length == 1 ? "tile_0000" : i == 0 ? "tile_0001" : i == length - 1 ? "tile_0003" : "tile_0002";
            Art.Place(root, Art.Load("SideView/" + tile), Cell(startX + i, y), 0, "Platform Tile");
        }
    }

    // A solid platform that slides: M moves right and back, N moves up and back.
    static void AddMovingPlatform(Transform root, int startX, int y, char c)
    {
        int length = 0;
        while (At(startX + length, y) == c) length++;

        GameObject go = new GameObject("Moving Platform");
        go.transform.SetParent(root, false);
        Vector2 left = Cell(startX, y);
        go.transform.localPosition = new Vector2(left.x - 0.5f + length / 2f, left.y);

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(length, 0.6f);
        box.offset = new Vector2(0f, 0.2f);
        box.sharedMaterial = MakeMaterial("Grass", 0.9f, 0f);

        MovingPlatform mover = go.AddComponent<MovingPlatform>();
        mover.travel = c == 'M' ? new Vector2(5f, 0f) : new Vector2(0f, 4f);
        mover.period = c == 'M' ? 4f : 3.5f;

        for (int i = 0; i < length; i++)
        {
            string tileName = length == 1 ? "tile_0000" : i == 0 ? "tile_0001" : i == length - 1 ? "tile_0003" : "tile_0002";
            SpriteRenderer sr = Art.Place(go.transform, Art.Load("SideView/" + tileName), new Vector2(i - (length - 1) / 2f, 0f), 1, "Platform Tile");
            sr.color = new Color(0.8f, 0.9f, 1f);
        }
    }

    static void AddCrate(Transform root, Vector2 p)
    {
        SpriteRenderer sr = Art.Place(root, Art.Load("SideView/tile_0026"), p, 3, "Crate");
        BoxCollider2D box = sr.gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.95f, 0.95f);
        box.sharedMaterial = MakeMaterial("Crate", 0.6f, 0f);
        Rigidbody2D body = sr.gameObject.AddComponent<Rigidbody2D>();
        body.mass = 0.6f;
        body.gravityScale = 2f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // Sky color, then two layers of hills/trees that scroll slower than the camera.
    static void BuildBackground(Transform root)
    {
        float wide = width + 60f;

        SpriteRenderer lowFill = Art.Place(root, Art.White, new Vector2(width / 2f, -6f), -50, "Background Fill");
        lowFill.transform.localScale = new Vector3(wide, 20f, 1f);
        lowFill.color = new Color32(194, 227, 232, 255);

        SpriteRenderer far = Art.Place(root, Art.Load("SideView/bg_0008", 6f, true), new Vector2(width / 2f, 7f), -49, "Far Hills");
        far.drawMode = SpriteDrawMode.Tiled;
        far.size = new Vector2(wide, 6f);
        far.gameObject.AddComponent<Parallax>().factor = 0.7f;

        SpriteRenderer near = Art.Place(root, Art.Load("SideView/bg_0009", 4f, true), new Vector2(width / 2f, 5f), -48, "Near Trees");
        near.drawMode = SpriteDrawMode.Tiled;
        near.size = new Vector2(wide, 4f);
        near.gameObject.AddComponent<Parallax>().factor = 0.45f;
    }

    // ------------------------------------------------------------------ Shared helpers

    static PhysicsMaterial2D MakeMaterial(string name, float friction, float bounce)
    {
        return new PhysicsMaterial2D(name) { friction = friction, bounciness = bounce };
    }

    // One object holding all the solid tiles. A CompositeCollider2D merges them into one smooth shape
    // so the ball doesn't bump on the seams between tiles.
    static GameObject MakeSolidGroup(Transform parent, string name, PhysicsMaterial2D material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composite = go.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.sharedMaterial = material;
        return go;
    }

    static void AddBox(GameObject group, Vector2 center, Vector2 size)
    {
        BoxCollider2D box = group.AddComponent<BoxCollider2D>();
        box.offset = center;
        box.size = size;
        box.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    static void AddPoly(GameObject group, params Vector2[] points)
    {
        PolygonCollider2D poly = group.AddComponent<PolygonCollider2D>();
        poly.points = points;
        poly.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    static T AddTrigger<T>(Transform root, string name, Vector2 p, float radius) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = p;
        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = radius;
        return go.AddComponent<T>();
    }

    static T AddBoxTrigger<T>(Transform root, string name, Vector2 p, Vector2 size) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = p;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;
        return go.AddComponent<T>();
    }

    static Pickup AddPickup(Transform root, Vector2 p, Sprite sprite, PickupType type)
    {
        SpriteRenderer sr = Art.Place(root, sprite, p, 4, type == PickupType.Key ? "Key" : "Bonus");
        CircleCollider2D circle = sr.gameObject.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.4f;
        Pickup pickup = sr.gameObject.AddComponent<Pickup>();
        pickup.type = type;
        return pickup;
    }

    // A key that runs away from the ball a few times before it lets you grab it
    static void MakeShy(Pickup key)
    {
        key.GetComponent<SpriteRenderer>().color = new Color(1f, 0.75f, 0.9f);
        key.gameObject.AddComponent<FleeingKey>();
    }

    static LockBlock AddLock(Transform root, Vector2 p, Sprite sprite)
    {
        SpriteRenderer sr = Art.Place(root, sprite, p, 1, "Lock");
        BoxCollider2D box = sr.gameObject.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        return sr.gameObject.AddComponent<LockBlock>();
    }
}
