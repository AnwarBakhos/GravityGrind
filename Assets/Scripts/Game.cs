using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState { Menu, Editor, Countdown, Playing, Paused, Finished }

// Runs the whole game: the menu, loading courses (built-in or your own), the countdown and timer,
// checkpoints and respawning, finishing, saving best times and ghosts, and opening the level editor.
public class Game : MonoBehaviour
{
    public static Game I { get; private set; }

    public GameState State { get; private set; } = GameState.Menu;
    public CameraRig Rig { get; private set; }
    public UIManager UI { get; private set; }
    public LevelEditor Editor { get; private set; }
    public BallController Ball => ball;

    int levelIndex = -1;          // -1 means a custom level
    bool testingFromEditor;       // true while play-testing a level from the editor
    LevelInfo current;            // the level being played (kept for restarting)
    LevelInfo level;
    BuiltLevel built;
    BallController ball;
    GhostRunner ghost;

    Vector2 checkpoint;
    float elapsed;
    float bonus;
    int keysFound;

    // The Game object in the scene has these components added in the Inspector:
    // Game, Sfx, UIManager and LevelEditor. The Main Camera has the CameraRig component.
    void Awake()
    {
        I = this;
        Application.targetFrameRate = 120;
        Time.fixedDeltaTime = 1f / 60f;

        Rig = Camera.main.GetComponent<CameraRig>();
        UI = GetComponent<UIManager>();
        Editor = GetComponent<LevelEditor>();
    }

    void Start()
    {
        CustomLevels.MoveOldLevels();
        ShowMenu();
    }

    float CurrentTime => Mathf.Max(0f, elapsed - bonus);

    void Update()
    {
        Keyboard kb = Keyboard.current;

        switch (State)
        {
            case GameState.Menu:
                // Slowly rock the board behind the menu
                Rig.SetTilt(new Vector2(Mathf.Sin(Time.time * 0.5f), Mathf.Cos(Time.time * 0.37f)) * 0.6f);
                if (kb != null)
                {
                    for (int i = 0; i < Levels.All.Count; i++)
                        if (kb[Key.Digit1 + i].wasPressedThisFrame) StartLevel(i);
                }
                break;

            case GameState.Countdown:
                if (kb != null && kb.rKey.wasPressedThisFrame) Restart();
                else if (kb != null && kb.escapeKey.wasPressedThisFrame) LeaveLevel();
                break;

            case GameState.Playing:
                elapsed += Time.deltaTime;
                ghost.Tick(elapsed, ball.transform.position);
                UI.SetTime(CurrentTime);
                if (kb != null && kb.escapeKey.wasPressedThisFrame) Pause();
                else if (kb != null && kb.rKey.wasPressedThisFrame) Restart();
                break;

            case GameState.Paused:
                if (kb != null && kb.escapeKey.wasPressedThisFrame) Resume();
                else if (kb != null && kb.rKey.wasPressedThisFrame) Restart();
                break;

            case GameState.Finished:
                if (kb == null) break;
                if (kb.rKey.wasPressedThisFrame) Restart();
                else if (kb.enterKey.wasPressedThisFrame || kb.nKey.wasPressedThisFrame) NextLevel();
                else if (kb.escapeKey.wasPressedThisFrame) LeaveLevel();
                break;
        }

        if (ball != null && level != null && level.mode == LevelMode.Tabletop) Rig.SetTilt(ball.Tilt);
    }

    // ------------------------------------------------------------------ Menu and levels

    public void ShowMenu()
    {
        Editor.Close();
        testingFromEditor = false;
        ClearLevel();
        Time.timeScale = 1f;
        State = GameState.Menu;

        // The first board sits behind the menu as a backdrop
        built = LevelBuilder.Build(Levels.All[0]);
        Rig.SetupTabletop(built.bounds);
        UI.ShowMenu();
    }

    public void StartLevel(int index)
    {
        testingFromEditor = false;
        Play(Levels.All[index], index);
    }

    // Plays one of your saved levels from the level editor menu
    public void PlayCustom(LevelMode mode, int slot)
    {
        string[] map = CustomLevels.Load(mode, slot);
        if (map == null) return;
        testingFromEditor = false;
        Play(CustomLevels.MakeInfo(mode, slot, map), -1);
    }

    // Play-test straight from the editor; finishing or quitting goes back to the editor
    public void TestFromEditor(LevelInfo info)
    {
        testingFromEditor = true;
        Play(info, -1);
    }

    public void OpenEditor(LevelMode mode, int slot)
    {
        ClearLevel();
        Time.timeScale = 1f;
        State = GameState.Editor;
        UI.HideAllScreens();
        Editor.Open(mode, slot);
    }

    void Play(LevelInfo info, int index)
    {
        Editor.Close();
        ClearLevel();
        Time.timeScale = 1f;

        levelIndex = index;
        current = info;
        level = info;
        built = LevelBuilder.Build(level);
        FX.Parent = built.root.transform;

        checkpoint = built.start;
        elapsed = 0f;
        bonus = 0f;
        keysFound = 0;

        ghost = GhostRunner.Create(level, built.root.transform);
        ball = BallController.Create(level.mode, built.start, built.bounds, built.root.transform);

        if (level.mode == LevelMode.Tabletop) Rig.SetupTabletop(built.bounds);
        else Rig.SetupSideView(built.bounds, ball.transform);

        UI.ShowHud(level, SaveData.GetBest(level.id), ghost.HasGhost, built.keyCount);
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        State = GameState.Countdown;
        ball.SetFrozen(true);

        for (int i = 3; i > 0; i--)
        {
            UI.ShowCountdown(i.ToString());
            Sfx.Play("count_tick", 0.8f);
            yield return new WaitForSeconds(0.6f);
        }

        UI.ShowCountdown("GO!");
        Sfx.Play("count_go", 0.8f);
        State = GameState.Playing;
        ball.SetFrozen(false);

        yield return new WaitForSeconds(0.5f);
        UI.ShowCountdown("");
    }

    public void Restart()
    {
        Play(current, levelIndex);
    }

    public void NextLevel()
    {
        if (testingFromEditor) BackToEditor();
        else if (levelIndex >= 0 && levelIndex + 1 < Levels.All.Count) StartLevel(levelIndex + 1);
        else ShowMenu();
    }

    // Esc / "Menu" button while in a level
    public void LeaveLevel()
    {
        if (testingFromEditor) BackToEditor();
        else ShowMenu();
    }

    void BackToEditor()
    {
        ClearLevel();
        Time.timeScale = 1f;
        State = GameState.Editor;
        UI.HideAllScreens();
        Editor.Reopen();
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
        Sfx.SetRoll(0f, 1f);
        UI.ShowPause(true);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        UI.ShowPause(false);
    }

    void ClearLevel()
    {
        StopAllCoroutines();
        if (built != null) Destroy(built.root);
        built = null;
        ball = null;
        ghost = null;
        level = null;
        Sfx.SetRoll(0f, 1f);
    }

    // ------------------------------------------------------------------ Things that happen during a run

    public void SetCheckpoint(Vector2 position)
    {
        checkpoint = position;
        UI.Popup(position, "Checkpoint!", new Color(0.5f, 0.85f, 1f));
    }

    public void Collect(Pickup pickup)
    {
        Vector2 position = pickup.transform.position;

        if (pickup.type == PickupType.TimeBonus)
        {
            bonus += 1f;
            Sfx.Play("coin", 0.7f);
            FX.Burst(position, new Color(1f, 0.85f, 0.3f), 8);
            UI.Popup(position, "-1.0s", new Color(1f, 0.85f, 0.3f));
        }
        else
        {
            keysFound++;
            Sfx.Play("key", 0.8f);
            UI.SetKeys(keysFound, built.keyCount);
            UI.Popup(position, "Key!", new Color(1f, 0.85f, 0.3f));
            if (keysFound >= built.keyCount)
            {
                foreach (LockBlock block in built.locks)
                    if (block != null) block.Open();
                StartCoroutine(PlayLater("unlock", 0.3f));
            }
        }
    }

    public bool BallNear(Vector2 position, float distance)
    {
        return ball != null && Vector2.Distance(ball.transform.position, position) < distance;
    }

    public void BallLost()
    {
        StartCoroutine(RespawnSoon());
    }

    IEnumerator RespawnSoon()
    {
        yield return new WaitForSeconds(0.6f);
        if (State == GameState.Playing && ball != null) ball.Respawn(checkpoint);
    }

    public void ReachFinish()
    {
        if (State != GameState.Playing) return;
        State = GameState.Finished;

        if (level.mode == LevelMode.Tabletop) ball.SetFrozen(true);
        else ball.ControlsOn = false;

        float time = CurrentTime;
        float best = SaveData.GetBest(level.id);
        bool newBest = best <= 0f || time < best;
        if (newBest)
        {
            SaveData.SetBest(level.id, time);
            ghost.SaveRun(level.id);
            best = time;
        }

        Sfx.Play("finish", 0.9f);
        if (newBest) StartCoroutine(PlayLater("new_best", 0.7f));
        FX.Confetti(ball.transform.position, 50);

        string nextLabel = testingFromEditor ? "Edit"
                         : levelIndex >= 0 && levelIndex + 1 < Levels.All.Count ? "Next" : "Menu";
        UI.ShowResults(level, time, best, newBest, level.MedalFor(time), nextLabel);
    }

    IEnumerator PlayLater(string sound, float delay)
    {
        yield return new WaitForSeconds(delay);
        Sfx.Play(sound, 0.8f);
    }
}
