using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Menu Panel")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;

    [Header("HUD - Top Panel")]
    [SerializeField] private GameObject topPanel;
    [SerializeField] private TextMeshProUGUI mineCounterText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI restartFaceText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button botButton;
    [SerializeField] private TextMeshProUGUI botButtonText;

    [Header("End Game Panel")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI endGameMessage;
    [SerializeField] private Button endRestartButton;

    private Board board;
    private MinesweeperBot bot;

    public void Initialize(Board board)
    {
        this.board = board;

        // Wire up button listeners
        easyButton.onClick.AddListener(() => OnDifficultySelected(9, 9, 10));
        mediumButton.onClick.AddListener(() => OnDifficultySelected(16, 16, 40));
        hardButton.onClick.AddListener(() => OnDifficultySelected(30, 16, 99));
        restartButton.onClick.AddListener(OnRestartClicked);
        endRestartButton.onClick.AddListener(OnRestartClicked);
        botButton.onClick.AddListener(OnBotClicked);

        // Initial state
        menuPanel.SetActive(true);
        topPanel.SetActive(false);
        endGamePanel.SetActive(false);
    }

    private void Update()
    {
        // Auto-update bot button text when bot finishes
        if (bot != null && !bot.IsRunning && botButtonText != null
            && botButtonText.text == "Dur")
        {
            botButtonText.text = "Bot";
            botButtonText.color = Color.white;
        }
    }

    // ========== MENU ==========

    private void OnDifficultySelected(int w, int h, int mines)
    {
        menuPanel.SetActive(false);
        topPanel.SetActive(true);
        UpdateMineCounter(mines);
        UpdateTimer(0);
        UpdateFace(GameState.Playing);
        board.InitializeBoard(w, h, mines);

        // Create bot
        if (bot == null)
        {
            bot = gameObject.AddComponent<MinesweeperBot>();
        }
        bot.Initialize(board);
    }

    // ========== BOT ==========

    private void OnBotClicked()
    {
        if (bot == null) return;
        if (board.IsGameOver()) return;

        if (bot.IsRunning)
        {
            bot.StopBot();
            botButtonText.text = "Bot";
            botButtonText.color = Color.white;
        }
        else
        {
            bot.StartBot();
            botButtonText.text = "Dur";
            botButtonText.color = new Color(1f, 0.8f, 0.3f);
        }
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
        // Stop bot if running
        if (bot != null && bot.IsRunning)
        {
            bot.StopBot();
            botButtonText.text = "Bot";
            botButtonText.color = Color.white;
        }

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
