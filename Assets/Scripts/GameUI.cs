using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameUI : MonoBehaviour
{
    private Board board;
    private Transform canvasTransform;

    // Menu
    private GameObject menuPanel;

    // HUD (top panel)
    private GameObject topPanel;
    private TextMeshProUGUI mineCounterText;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI restartFaceText;

    // End game
    private GameObject endGamePanel;
    private TextMeshProUGUI endGameMessage;

    public void Initialize(Board board)
    {
        this.board = board;
        CreateCanvas();
    }

    private void CreateCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasTransform = canvasGO.transform;

        // EventSystem is required for UI button clicks to register
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(transform);
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }

        CreateMenu();
        CreateTopPanel();
        CreateEndGamePanel();
    }

    // ========== MENU ==========

    private void CreateMenu()
    {
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(canvasTransform, false);
        RectTransform rect = menuPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = menuPanel.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);

        // Center container
        GameObject container = new GameObject("Container");
        container.transform.SetParent(menuPanel.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(350, 320);

        // Title
        TextMeshProUGUI title = CreateTMPText(container.transform, "Title",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -10), new Vector2(340, 60));
        title.text = "Minesweeper";
        title.fontSize = 42;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;

        // Difficulty buttons
        CreateDifficultyButton(container.transform, "Kolay (9x9, 10 mayin)",
            new Vector2(0, -85), () => OnDifficultySelected(9, 9, 10));
        CreateDifficultyButton(container.transform, "Orta (16x16, 40 mayin)",
            new Vector2(0, -150), () => OnDifficultySelected(16, 16, 40));
        CreateDifficultyButton(container.transform, "Zor (30x16, 99 mayin)",
            new Vector2(0, -215), () => OnDifficultySelected(30, 16, 99));
    }

    private void CreateDifficultyButton(Transform parent, string label, Vector2 pos,
        UnityEngine.Events.UnityAction action)
    {
        GameObject btnGO = new GameObject("DiffBtn");
        btnGO.transform.SetParent(parent, false);
        RectTransform rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(300, 50);

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(0.3f, 0.45f, 0.7f);

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.4f, 0.55f, 0.8f);
        colors.pressedColor = new Color(0.2f, 0.35f, 0.6f);
        btn.colors = colors;
        btn.onClick.AddListener(action);

        TextMeshProUGUI text = CreateTMPText(btnGO.transform, "Label",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        text.text = label;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }

    private void OnDifficultySelected(int w, int h, int mines)
    {
        menuPanel.SetActive(false);
        topPanel.SetActive(true);
        UpdateMineCounter(mines);
        UpdateTimer(0);
        UpdateFace(GameState.Playing);
        board.InitializeBoard(w, h, mines);
    }

    // ========== TOP PANEL (HUD) ==========

    private void CreateTopPanel()
    {
        topPanel = new GameObject("TopPanel");
        topPanel.transform.SetParent(canvasTransform, false);
        RectTransform panelRect = topPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(0.5f, 1);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0, 50);

        Image panelBg = topPanel.AddComponent<Image>();
        panelBg.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);

        // Mine icon (left)
        TextMeshProUGUI mineIcon = CreateTMPText(topPanel.transform, "MineIcon",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(15, 0), new Vector2(30, 40));
        mineIcon.text = "\u25CF";
        mineIcon.fontSize = 18;
        mineIcon.color = Color.red;
        mineIcon.alignment = TextAlignmentOptions.Center;

        // Mine counter (left, after icon)
        mineCounterText = CreateTMPText(topPanel.transform, "MineCounter",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(48, 0), new Vector2(60, 40));
        mineCounterText.alignment = TextAlignmentOptions.Left;
        mineCounterText.fontSize = 28;
        mineCounterText.color = Color.red;

        // Restart button (center) with face
        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(topPanel.transform, false);
        RectTransform btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(44, 38);

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(0.35f, 0.35f, 0.4f);

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock btnColors = btn.colors;
        btnColors.highlightedColor = new Color(0.45f, 0.45f, 0.5f);
        btnColors.pressedColor = new Color(0.25f, 0.25f, 0.3f);
        btn.colors = btnColors;
        btn.onClick.AddListener(OnRestartClicked);

        restartFaceText = CreateTMPText(btnGO.transform, "Face",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        restartFaceText.alignment = TextAlignmentOptions.Center;
        restartFaceText.fontSize = 22;
        restartFaceText.color = Color.yellow;
        restartFaceText.text = ":)";

        // Timer (right)
        timerText = CreateTMPText(topPanel.transform, "Timer",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-20, 0), new Vector2(80, 40));
        timerText.alignment = TextAlignmentOptions.Right;
        timerText.fontSize = 28;
        timerText.color = Color.white;

        topPanel.SetActive(false);
    }

    // ========== END GAME PANEL ==========

    private void CreateEndGamePanel()
    {
        endGamePanel = new GameObject("EndGamePanel");
        endGamePanel.transform.SetParent(canvasTransform, false);
        RectTransform overlayRect = endGamePanel.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayBg = endGamePanel.AddComponent<Image>();
        overlayBg.color = new Color(0, 0, 0, 0.5f);

        // Center message box
        GameObject boxGO = new GameObject("MessageBox");
        boxGO.transform.SetParent(endGamePanel.transform, false);
        RectTransform boxRect = boxGO.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(300, 160);

        Image boxBg = boxGO.AddComponent<Image>();
        boxBg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        // End game message
        endGameMessage = CreateTMPText(boxGO.transform, "EndMessage",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -20), new Vector2(280, 60));
        endGameMessage.alignment = TextAlignmentOptions.Center;
        endGameMessage.fontSize = 32;
        endGameMessage.fontStyle = FontStyles.Bold;

        // Restart button in end panel
        GameObject restartBtnGO = new GameObject("EndRestartButton");
        restartBtnGO.transform.SetParent(boxGO.transform, false);
        RectTransform restartRect = restartBtnGO.AddComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0);
        restartRect.anchorMax = new Vector2(0.5f, 0);
        restartRect.pivot = new Vector2(0.5f, 0);
        restartRect.anchoredPosition = new Vector2(0, 20);
        restartRect.sizeDelta = new Vector2(200, 45);

        Image restartBg = restartBtnGO.AddComponent<Image>();
        restartBg.color = new Color(0.3f, 0.5f, 0.8f);

        Button restartBtn = restartBtnGO.AddComponent<Button>();
        ColorBlock restartColors = restartBtn.colors;
        restartColors.highlightedColor = new Color(0.4f, 0.6f, 0.9f);
        restartColors.pressedColor = new Color(0.2f, 0.4f, 0.7f);
        restartBtn.colors = restartColors;
        restartBtn.onClick.AddListener(OnRestartClicked);

        TextMeshProUGUI restartText = CreateTMPText(restartBtnGO.transform, "RestartText",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        restartText.alignment = TextAlignmentOptions.Center;
        restartText.fontSize = 22;
        restartText.color = Color.white;
        restartText.text = "Yeniden Basla";

        endGamePanel.SetActive(false);
    }

    // ========== HELPERS ==========

    private TextMeshProUGUI CreateTMPText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        return tmp;
    }

    // ========== PUBLIC API ==========

    public void UpdateMineCounter(int remaining)
    {
        mineCounterText.text = remaining.ToString("D2");
    }

    public void UpdateTimer(int seconds)
    {
        timerText.text = Mathf.Min(seconds, 999).ToString("D3");
    }

    public void UpdateFace(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                restartFaceText.text = ":)";
                restartFaceText.color = Color.yellow;
                break;
            case GameState.Won:
                restartFaceText.text = "B)";
                restartFaceText.color = Color.green;
                break;
            case GameState.Lost:
                restartFaceText.text = "X(";
                restartFaceText.color = Color.red;
                break;
        }
    }

    public void ShowEndGame(bool won)
    {
        endGamePanel.SetActive(true);
        UpdateFace(won ? GameState.Won : GameState.Lost);

        if (won)
        {
            endGameMessage.text = "Kazandiniz!";
            endGameMessage.color = new Color(0.2f, 0.9f, 0.2f);
        }
        else
        {
            endGameMessage.text = "Kaybettiniz!";
            endGameMessage.color = new Color(0.9f, 0.2f, 0.2f);
        }
    }

    private void OnRestartClicked()
    {
        board.RestartGame();
    }
}
