using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Builds and updates all the on-screen UI: main menu, level editor menu, HUD, countdown, pause and results.
public class UIManager : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
    static readonly Color GhostBlue = new Color(0.45f, 0.85f, 1f);
    static readonly Color PanelColor = new Color(0.1f, 0.08f, 0.13f, 0.88f);

    Font font;
    RectTransform root;
    GameObject menu, customMenu, hud, pause, results;

    public RectTransform Root => root;

    Text levelText, timerText, bestText, ghostText, keyText, hintText, countdownText;
    Text resultTitle, resultTime, resultBest, newBestText, medalTargets;
    Image[] resultStars = new Image[3];
    Button nextButton;

    Text[] menuBestTexts;
    Image[][] menuStars;
    Text[] customInfoTexts = new Text[6];
    Button[] customPlayButtons = new Button[6];
    Coroutine hintRoutine;

    void Awake()
    {
        font = Resources.Load<Font>("Fonts/LilitaOne");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildCanvas();
        BuildHud();
        BuildMenu();
        BuildCustomMenu();
        BuildPause();
        BuildResults();
    }

    // ------------------------------------------------------------------ Public calls from Game

    public void ShowMenu()
    {
        HideAllScreens();
        menu.SetActive(true);
        RefreshMenu();
    }

    public void ShowCustomMenu()
    {
        HideAllScreens();
        customMenu.SetActive(true);
        RefreshCustomMenu();
    }

    public void HideAllScreens()
    {
        menu.SetActive(false);
        customMenu.SetActive(false);
        hud.SetActive(false);
        pause.SetActive(false);
        results.SetActive(false);
    }

    public void ShowHud(LevelInfo level, float best, bool hasGhost, int keyCount)
    {
        HideAllScreens();
        hud.SetActive(true);

        levelText.text = level.name;
        timerText.text = FormatTime(0f);
        bestText.text = best > 0f ? "BEST " + FormatTime(best) : "BEST --:--";
        ghostText.text = hasGhost ? "Race your ghost!" : "";
        keyText.gameObject.SetActive(keyCount > 0);
        SetKeys(0, keyCount);
        countdownText.text = "";

        if (hintRoutine != null) StopCoroutine(hintRoutine);
        hintRoutine = StartCoroutine(ShowHint(level.hint));
    }

    public void SetTime(float time)
    {
        timerText.text = FormatTime(time);
    }

    public void SetKeys(int found, int total)
    {
        keyText.text = found >= total ? "Key found! Locks open" : "Key: " + found + " / " + total;
        keyText.color = found >= total ? Gold : Color.white;
    }

    public void ShowCountdown(string text)
    {
        countdownText.text = text;
        if (text.Length > 0) StartCoroutine(Pop(countdownText.transform));
    }

    public void ShowPause(bool show)
    {
        pause.SetActive(show);
        if (show) countdownText.text = "";
    }

    public void ShowResults(LevelInfo level, float time, float best, bool newBest, int medal, string nextLabel)
    {
        results.SetActive(true);
        resultTitle.text = level.name + " - Finished!";
        resultTime.text = FormatTime(time);
        resultBest.text = "Best " + FormatTime(best);
        newBestText.gameObject.SetActive(newBest);
        medalTargets.text = level.gold > 0f
            ? "Gold " + FormatTime(level.gold) + "     Silver " + FormatTime(level.silver) + "     Bronze " + FormatTime(level.bronze)
            : "Custom level - beat your own best time!";
        nextButton.GetComponentInChildren<Text>().text = nextLabel;

        for (int i = 0; i < 3; i++)
        {
            resultStars[i].sprite = Art.Load(i < medal ? "Tabletop/star" : "Tabletop/star_outline");
            resultStars[i].transform.localScale = Vector3.zero;
            StartCoroutine(PopIn(resultStars[i].transform, 0.3f + i * 0.2f));
        }
    }

    // Floating text that rises from a spot in the world and fades out
    public void Popup(Vector2 worldPosition, string message, Color color)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 screen = cam.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
        Text text = MakeText(root, message, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), local, new Vector2(500, 70), color);
        StartCoroutine(FloatAway(text));
    }

    // ------------------------------------------------------------------ Building the screens

    void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root = (RectTransform)canvasObject.transform;
    }

    void BuildHud()
    {
        hud = MakeLayer("HUD");
        Transform t = hud.transform;

        levelText = MakeText(t, "", 46, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(300, -50), new Vector2(560, 70), Color.white);
        keyText = MakeText(t, "", 34, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(300, -110), new Vector2(560, 50), Color.white);
        timerText = MakeText(t, "0:00.00", 86, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -65), new Vector2(600, 110), Color.white);
        bestText = MakeText(t, "", 42, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-300, -50), new Vector2(560, 60), Gold);
        ghostText = MakeText(t, "", 32, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-300, -105), new Vector2(560, 50), GhostBlue);
        hintText = MakeText(t, "", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(1600, 60), Color.white);
        countdownText = MakeText(t, "", 200, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(800, 260), Gold);
    }

    void BuildMenu()
    {
        menu = MakeLayer("Menu");
        Transform t = menu.transform;
        MakePanel(t, new Color(0.05f, 0.04f, 0.08f, 0.55f), Vector2.zero, new Vector2(4000, 4000));

        MakeText(t, "GRAVITY GRIND", 150, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1600, 180), Gold);
        MakeText(t, "tilt it.  roll it.  beat your ghost.", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -255), new Vector2(1400, 60), Color.white);

        MakeText(t, "TABLETOP", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(-380, 190), new Vector2(620, 70), GhostBlue);
        MakeText(t, "SIDE VIEW", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(380, 190), new Vector2(620, 70), GhostBlue);

        int count = Levels.All.Count;
        menuBestTexts = new Text[count];
        menuStars = new Image[count][];

        for (int i = 0; i < count; i++)
        {
            int index = i;
            LevelInfo level = Levels.All[i];
            float x = level.mode == LevelMode.Tabletop ? -380 : 380;
            float y = 80 - (i % 3) * 140;

            Button button = MakeButton(t, (i + 1) + ".  " + level.name, new Vector2(x, y), new Vector2(620, 120), () => Game.I.StartLevel(index), 42);
            Text title = button.GetComponentInChildren<Text>();
            title.alignment = TextAnchor.UpperLeft;
            title.rectTransform.offsetMin = new Vector2(30, 0);
            title.rectTransform.offsetMax = new Vector2(0, -14);

            menuBestTexts[i] = MakeText(button.transform, "", 30, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(230, 38), new Vector2(400, 50), new Color(0.3f, 0.2f, 0.1f));
            menuBestTexts[i].GetComponent<Shadow>().enabled = false;

            menuStars[i] = new Image[3];
            for (int s = 0; s < 3; s++)
                menuStars[i][s] = MakeImage(button.transform, Art.Load("Tabletop/star_outline"), new Vector2(1, 0), new Vector2(-150 + s * 48, 38), new Vector2(42, 42));
        }

        MakeText(t, "Tabletop: move the mouse to tilt the board      Side view: A / D to roll, Space to jump      R: restart      Esc: pause",
                 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(1800, 50), Color.white);
        MakeText(t, "Press 1 - 6 or click a course", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1000, 50), new Color(1f, 1f, 1f, 0.7f));

        MakeButton(t, "Reset times", new Vector2(760, -470), new Vector2(260, 70), () => { SaveData.ResetAll(); RefreshMenu(); }, 28);
        MakeButton(t, "Level Editor", new Vector2(-760, -470), new Vector2(300, 80), () => ShowCustomMenu(), 34);
    }

    // Lists the 3 tabletop and 3 side-view slots for your own levels
    void BuildCustomMenu()
    {
        customMenu = MakeLayer("Level Editor Menu");
        Transform t = customMenu.transform;
        MakePanel(t, new Color(0.05f, 0.04f, 0.08f, 0.75f), Vector2.zero, new Vector2(4000, 4000));

        MakeText(t, "LEVEL EDITOR", 120, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1600, 150), Gold);
        MakeText(t, "Build your own courses with every piece from the game. They save on this computer.", 36,
                 TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(1700, 60), Color.white);

        MakeText(t, "TABLETOP", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(-420, 170), new Vector2(700, 70), GhostBlue);
        MakeText(t, "SIDE VIEW", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(420, 170), new Vector2(700, 70), GhostBlue);

        for (int i = 0; i < 6; i++)
        {
            LevelMode mode = i < 3 ? LevelMode.Tabletop : LevelMode.SideView;
            int slot = i % 3 + 1;
            float x = i < 3 ? -420 : 420;
            float y = 60 - (i % 3) * 140;

            MakePanel(t, PanelColor, new Vector2(x, y), new Vector2(700, 120));
            MakeText(t, "Slot " + slot, 44, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(x - 100, y + 18), new Vector2(460, 60), Color.white);
            customInfoTexts[i] = MakeText(t, "", 28, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(x - 100, y - 28), new Vector2(460, 40), new Color(1f, 1f, 1f, 0.7f));
            MakeButton(t, "Edit", new Vector2(x + 110, y), new Vector2(170, 90), () => Game.I.OpenEditor(mode, slot), 36);
            customPlayButtons[i] = MakeButton(t, "Play", new Vector2(x + 270, y), new Vector2(150, 90), () => Game.I.PlayCustom(mode, slot), 36);
        }

        MakeButton(t, "Back", new Vector2(0, -420), new Vector2(300, 90), () => ShowMenu(), 40);
    }

    void RefreshCustomMenu()
    {
        for (int i = 0; i < 6; i++)
        {
            LevelMode mode = i < 3 ? LevelMode.Tabletop : LevelMode.SideView;
            int slot = i % 3 + 1;
            bool exists = CustomLevels.Exists(mode, slot);
            float best = SaveData.GetBest(CustomLevels.Id(mode, slot));
            customInfoTexts[i].text = !exists ? "Empty - click Edit to build" : best > 0f ? "Best " + FormatTime(best) : "Not beaten yet";
            customPlayButtons[i].interactable = exists;
        }
    }

    void BuildPause()
    {
        pause = MakeLayer("Pause");
        Transform t = pause.transform;
        MakePanel(t, new Color(0f, 0f, 0f, 0.6f), Vector2.zero, new Vector2(4000, 4000));
        MakeText(t, "PAUSED", 120, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(900, 160), Gold);
        MakeButton(t, "Resume", new Vector2(0, 60), new Vector2(420, 100), () => Game.I.Resume(), 44);
        MakeButton(t, "Restart", new Vector2(0, -60), new Vector2(420, 100), () => Game.I.Restart(), 44);
        MakeButton(t, "Menu", new Vector2(0, -180), new Vector2(420, 100), () => Game.I.LeaveLevel(), 44);
    }

    void BuildResults()
    {
        results = MakeLayer("Results");
        Transform t = results.transform;
        MakePanel(t, PanelColor, Vector2.zero, new Vector2(1000, 720));

        resultTitle = MakeText(t, "", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 290), new Vector2(960, 80), Color.white);
        resultTime = MakeText(t, "", 130, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(900, 160), Color.white);
        newBestText = MakeText(t, "NEW BEST!", 52, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 75), new Vector2(700, 70), Gold);
        newBestText.gameObject.AddComponent<Blink>();
        resultBest = MakeText(t, "", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(700, 60), GhostBlue);

        for (int i = 0; i < 3; i++)
            resultStars[i] = MakeImage(t, Art.Load("Tabletop/star_outline"), new Vector2(0.5f, 0.5f), new Vector2(-130 + i * 130, -80), new Vector2(110, 110));

        medalTargets = MakeText(t, "", 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(960, 50), new Color(1f, 1f, 1f, 0.7f));

        MakeButton(t, "Retry (R)", new Vector2(-300, -270), new Vector2(270, 90), () => Game.I.Restart(), 36);
        nextButton = MakeButton(t, "Next", new Vector2(0, -270), new Vector2(270, 90), () => Game.I.NextLevel(), 36);
        MakeButton(t, "Menu (Esc)", new Vector2(300, -270), new Vector2(270, 90), () => Game.I.LeaveLevel(), 36);
    }

    void RefreshMenu()
    {
        for (int i = 0; i < Levels.All.Count; i++)
        {
            LevelInfo level = Levels.All[i];
            float best = SaveData.GetBest(level.id);
            int medal = best > 0f ? level.MedalFor(best) : 0;
            menuBestTexts[i].text = best > 0f ? "Best " + FormatTime(best) : "Not played yet";
            for (int s = 0; s < 3; s++)
                menuStars[i][s].sprite = Art.Load(s < medal ? "Tabletop/star" : "Tabletop/star_outline");
        }
    }

    // ------------------------------------------------------------------ Small helpers

    public static string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        return minutes + ":" + (seconds - minutes * 60f).ToString("00.00");
    }

    public GameObject MakeLayer(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    public RectTransform Place(GameObject go, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public Image MakePanel(Transform parent, Color color, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        Place(go, parent, new Vector2(0.5f, 0.5f), position, size);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public Image MakeImage(Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject("Image", typeof(RectTransform), typeof(Image));
        Place(go, parent, anchor, position, size);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    public Text MakeText(Transform parent, string message, int size, TextAnchor align, Vector2 anchor, Vector2 position, Vector2 box, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        Place(go, parent, anchor, position, box);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = message;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    public Button MakeButton(Transform parent, string label, Vector2 position, Vector2 size, Action onClick, int fontSize)
    {
        GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        Place(go, parent, new Vector2(0.5f, 0.5f), position, size);

        Image image = go.GetComponent<Image>();
        image.sprite = Art.LoadSliced("Tabletop/button_yellow", 30f);
        image.type = Image.Type.Sliced;

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 0.85f);
        colors.pressedColor = new Color(0.8f, 0.7f, 0.5f);
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            Sfx.Play("ui_click", 0.7f);
            onClick();
        });

        // Little tick sound when the mouse moves over a button
        EventTrigger trigger = go.AddComponent<EventTrigger>();
        EventTrigger.Entry hover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hover.callback.AddListener(_ => Sfx.Play("ui_hover", 0.4f));
        trigger.triggers.Add(hover);

        Text text = MakeText(go.transform, label, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(0.3f, 0.2f, 0.1f));
        text.GetComponent<Shadow>().enabled = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return button;
    }

    IEnumerator ShowHint(string hint)
    {
        hintText.text = hint;
        hintText.color = Color.white;
        yield return new WaitForSeconds(5f);
        for (float a = 1f; a > 0f; a -= Time.deltaTime)
        {
            hintText.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        hintText.text = "";
    }

    IEnumerator Pop(Transform target)
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
        {
            target.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, t);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator PopIn(Transform target, float delay)
    {
        yield return new WaitForSeconds(delay);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
        {
            target.localScale = Vector3.one * (Mathf.Sin(t * Mathf.PI) * 0.4f + t);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator FloatAway(Text text)
    {
        RectTransform rect = text.rectTransform;
        Color start = text.color;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f)
        {
            rect.anchoredPosition += Vector2.up * 90f * Time.deltaTime;
            text.color = new Color(start.r, start.g, start.b, 1f - t);
            yield return null;
        }
        Destroy(text.gameObject);
    }
}
