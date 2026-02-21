using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameUI : MonoBehaviour
{
    private TextMeshProUGUI mineCounterText;
    private TextMeshProUGUI timerText;
    private GameObject endGamePanel;
    private TextMeshProUGUI endGameMessage;
    private Board board;

    public void Initialize(int initialMineCount, Board board)
    {
        this.board = board;
        CreateCanvas();
        UpdateMineCounter(initialMineCount);
        UpdateTimer(0);
    }

    private void CreateCanvas()
    {
        // Canvas
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

        // EventSystem is required for UI button clicks to register
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(transform);
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }

        CreateTopPanel(canvasGO.transform);
        CreateEndGamePanel(canvasGO.transform);
    }

    private void CreateTopPanel(Transform canvasTransform)
    {
        // Top panel background
        GameObject panelGO = new GameObject("TopPanel");
        panelGO.transform.SetParent(canvasTransform, false);
        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(0.5f, 1);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0, 50);

        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

        // Mine counter (left)
        mineCounterText = CreateTMPText(panelGO.transform, "MineCounter",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(20, 0), new Vector2(120, 40));
        mineCounterText.alignment = TextAlignmentOptions.Left;
        mineCounterText.fontSize = 24;
        mineCounterText.color = Color.red;

        // Restart button (center)
        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(panelGO.transform, false);
        RectTransform btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(90, 36);

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(0.4f, 0.4f, 0.45f);

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.5f, 0.5f, 0.55f);
        colors.pressedColor = new Color(0.3f, 0.3f, 0.35f);
        btn.colors = colors;
        btn.onClick.AddListener(OnRestartClicked);

        TextMeshProUGUI btnText = CreateTMPText(btnGO.transform, "BtnText",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 18;
        btnText.color = Color.white;
        btnText.text = "Restart";

        // Timer (right)
        timerText = CreateTMPText(panelGO.transform, "Timer",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-20, 0), new Vector2(120, 40));
        timerText.alignment = TextAlignmentOptions.Right;
        timerText.fontSize = 24;
        timerText.color = Color.white;
    }

    private void CreateEndGamePanel(Transform canvasTransform)
    {
        // Semi-transparent overlay
        endGamePanel = new GameObject("EndGamePanel");
        endGamePanel.transform.SetParent(canvasTransform, false);
        RectTransform overlayRect = endGamePanel.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayBg = endGamePanel.AddComponent<Image>();
        overlayBg.color = new Color(0, 0, 0, 0.5f);

        // Center box
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

    public void UpdateMineCounter(int remaining)
    {
        mineCounterText.text = remaining.ToString();
    }

    public void UpdateTimer(int seconds)
    {
        timerText.text = seconds.ToString();
    }

    public void ShowEndGame(bool won)
    {
        endGamePanel.SetActive(true);

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
