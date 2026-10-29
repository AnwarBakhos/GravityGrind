using System.Collections.Generic;

public enum LevelMode { Tabletop, SideView }

// One course: its name, how it plays, the map, and the medal times.
public class LevelInfo
{
    public string id;
    public string name;
    public string hint;
    public LevelMode mode;
    public string[] map;
    public float gold, silver, bronze;

    // 3 = gold, 2 = silver, 1 = bronze, 0 = no medal
    public int MedalFor(float time)
    {
        if (gold <= 0f) return 0;   // custom levels have no medal times
        if (time <= gold) return 3;
        if (time <= silver) return 2;
        if (time <= bronze) return 1;
        return 0;
    }
}

// All six courses. Each map is drawn with text, one character per tile.
//
// Tabletop:  # wall   O hole   T trapdoor (opens and closes)   S start   F finish hole   C checkpoint
//            * star (-1 sec)   K key   L lock   R spinning bar   U bumper   > < ^ v boost pad
//            P/Q and p/q portal pairs   1 2 3 4 corner wedge (bottom-left, bottom-right, top-right, top-left)
//
// Side view: # ground   I ice   / \\ steep slopes   a b gentle slope up   c d gentle slope down
//            = one-way platform   M moving platform (sideways)   N elevator (up and down)   X crumbling block
//            ^ spikes   v ceiling spikes   J spring   > < boost pad   G flip gravity up   g flip gravity down
//            C checkpoint   F finish   * coin (-1 sec)   K key   L lock   B crate   S start
public static class Levels
{
    public static readonly List<LevelInfo> All = new List<LevelInfo>
    {
        new LevelInfo
        {
            id = "t1", name = "Wooden Warmup", mode = LevelMode.Tabletop,
            hint = "Move the mouse to tilt the board. Boost pads (>>) launch you!",
            gold = 32f, silver = 41f, bronze = 53f,
            map = new[]
            {
                "############################",
                "#S..........O.........O....#",
                "#.....O.........*..........#",
                "#..........O........O......#",
                "#...*...........O..........#",
                "######################.....#",
                "#....O.......O.......O.C...#",
                "#..O.....O......<....O.....#",
                "#.......*....O.........O...#",
                "#.....O..........O.........#",
                "#.....######################",
                "#.C.....O....O....>.....O..#",
                "#...O.....O......O....O....#",
                "#......O.....*.......O...O.#",
                "#1.O.......O....O........OF#",
                "############################",
            }
        },
        new LevelInfo
        {
            id = "t2", name = "Portal Lock", mode = LevelMode.Tabletop,
            hint = "Portals teleport you. Get the key, and watch the trapdoors!",
            gold = 13f, silver = 17f, bronze = 22f,
            map = new[]
            {
                "############################",
                "#S......O....#.......O.....#",
                "#..*.........#..O.......*..#",
                "#......O.....#Q...O....T...#",
                "#............#......K..T..p#",
                "#..P...q.....#..O...T......#",
                "#.......O....#.......O.....#",
                "#####L######################",
                "#..........#...............#",
                "#.TT.TT.TT.#..U....U....U..#",
                "#..........#.......C.......#",
                "#.TT.TT.TT.#..U....U....U..#",
                "#..........#...............#",
                "#.....*....>...O.....O..T..#",
                "#1....C....#..........T...F#",
                "############################",
            }
        },
        new LevelInfo
        {
            id = "t3", name = "Pinball Panic", mode = LevelMode.Tabletop,
            hint = "Bumpers kick you around. Stay on the narrow bridge!",
            gold = 18f, silver = 23f, bronze = 30f,
            map = new[]
            {
                "############################",
                "#S.....U.......U.......U...#",
                "#...U.....R.......R.....U..#",
                "#.......U.......U.....*....#",
                "#..U......U..R.......U...U.#",
                "#..........................#",
                "##########################.#",
                "#.OOOOOOOOOOOOOOOOOOOOOOO..#",
                "#C.......*.......*......<..#",
                "#.OOOOOOOOOOOOOOOOOOOOOOOOO#",
                "#.##########################",
                "#...........U.........T....#",
                "#..T...R......T.....R....U.#",
                "#......*...U.....T.........#",
                "#2..U.......T......U.....F.#",
                "############################",
            }
        },
        new LevelInfo
        {
            id = "s1", name = "Rolling Hills", mode = LevelMode.SideView,
            hint = "A / D to roll, Space to jump. Ride the hills and boost pads.",
            gold = 22f, silver = 29f, bronze = 38f,
            map = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                                                                                                           ***",
                "                                                     * *                                                              ***",
                "            ***                **                   *   *                           * *                                                  ab###",
                "                                                                   ab##                          /#\\    /#\\                            ab#####",
                "          ab###cd             /##\\                               ab####                         /###\\  /###\\                         ab#######    ###\\",
                "  S     ab#######cd          /####\\            >           C   ab######                    C   /#####\\/#####\\     >         C  ^^  ab#########    ####\\     F",
                "######################   ############    ##########       #############   #### MMM        ###########################      ###################    ###############",
                "######################   ############    ##########       #############   ####            ###########################      ###################    ###############",
                "######################   ############    ##########       #############   ####            ###########################      ###################    ###############",
            }
        },
        new LevelInfo
        {
            id = "s2", name = "Crumble Canyon", mode = LevelMode.SideView,
            hint = "Brown blocks crumble - keep moving! Springs launch you up.",
            gold = 24f, silver = 31f, bronze = 40f,
            map = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "            *                                              *",
                "                      *    *                                    C                                             ***",
                "                                                              ########cd            *  *",
                "              ######XXXXXXXXXX####\\                           ##########cd",
                "              ######          #####\\                          ############cd                C   ^^            ^^^",
                "              ######          #########\\                      ################## MM       ############XXXX############cd",
                "              ######          ##########\\                     ##################          ############    ##############cd",
                "  S    ^^   J ######          ###########\\ C            C NNN ##################          ############    ################cd      F",
                "####################          ##################XXXXXXXX########################          ############    ############################",
                "####################          ##################        ########################          ############    ############################",
                "####################          ##################        ########################          ############    ############################",
            }
        },
        new LevelInfo
        {
            id = "s3", name = "Flip Side", mode = LevelMode.SideView,
            hint = "Purple fields flip gravity. The key is shy - corner it on the hill!",
            gold = 28f, silver = 36f, bronze = 46f,
            map = new[]
            {
                "                                                ##########   ###########    ###############",
                "                                                ##########   ###########    ###############",
                "                                                ##########   ###########    ###############",
                "                                                  G    *         vv *           vv  *   g",
                "                                                  G                                     g",
                "                                                  G                                     g",
                "                                                  G                                     g",
                "  S             *                                 G                                     g",
                "########IIII\\                                     G                                     g           k",
                "#############III\\       *       **                G                                     g                             ***       L",
                "#################III\\                             G                                     g       /########\\                      L",
                "#####################III\\                 C       G ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ g  C   /##########\\         >           L       F",
                "#########################IIIIII    IIIIII######################################################################   IIIIIIIIIIII##################",
                "###############################    ############################################################################   ##############################",
                "###############################    ############################################################################   ##############################",
                "###############################    ############################################################################   ##############################",
            }
        },
    };
}
