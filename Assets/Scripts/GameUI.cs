using System.Collections.Generic;
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
    [SerializeField] private TextMeshProUGUI endGameStatsText;

    [Header("Stats Panel")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private Button statsButton;
    [SerializeField] private Button statsBackButton;
    [SerializeField] private TextMeshProUGUI statsContentText;
    [SerializeField] private Button statsEasyTab;
    [SerializeField] private Button statsMediumTab;
    [SerializeField] private Button statsHardTab;
    [SerializeField] private Button statsResetButton;

    [Header("Stats Tab Sprites")]
    [SerializeField] private Sprite easyTabActiveSprite;
    [SerializeField] private Sprite easyTabInactiveSprite;
    [SerializeField] private Sprite mediumTabActiveSprite;
    [SerializeField] private Sprite mediumTabInactiveSprite;
    [SerializeField] private Sprite hardTabActiveSprite;
    [SerializeField] private Sprite hardTabInactiveSprite;

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button gameSettingsButton;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle sfxToggle;

    [Header("Dependencies")]
    [SerializeField] private StatsManager statsManager;

    private Board board;
    private MinesweeperBot bot;
    private string currentDifficulty;
    private string selectedStatsTab = "Easy";
    private bool resetConfirmPending;
    private TextMeshProUGUI statsResetButtonText;

    private static readonly Color TabActiveTextColor = Color.white;
    private static readonly Color TabInactiveTextColor = new Color(0.6f, 0.6f, 0.6f);

    public void Initialize(Board board)
    {
        this.board = board;

        // Button click sounds (added first so sound plays before action)
        AddClickSound(easyButton);
        AddClickSound(mediumButton);
        AddClickSound(hardButton);
        AddClickSound(restartButton);
        AddClickSound(endRestartButton);
        AddClickSound(botButton);
        AddClickSound(settingsButton);
        AddClickSound(gameSettingsButton);
        AddClickSound(settingsBackButton);
        AddClickSound(statsButton);
        AddClickSound(statsBackButton);
        AddClickSound(statsResetButton);
        AddClickSound(statsEasyTab);
        AddClickSound(statsMediumTab);
        AddClickSound(statsHardTab);

        // Difficulty buttons
        easyButton.onClick.AddListener(() => OnDifficultySelected(9, 9, 10, "Easy"));
        mediumButton.onClick.AddListener(() => OnDifficultySelected(16, 16, 40, "Medium"));
        hardButton.onClick.AddListener(() => OnDifficultySelected(30, 16, 99, "Hard"));

        // HUD buttons
        restartButton.onClick.AddListener(OnRestartClicked);
        endRestartButton.onClick.AddListener(OnRestartClicked);
        botButton.onClick.AddListener(OnBotClicked);

        // Stats buttons
        statsButton.onClick.AddListener(OnStatsClicked);
        statsBackButton.onClick.AddListener(OnStatsBackClicked);
        statsEasyTab.onClick.AddListener(() => OnStatsTabSelected("Easy"));
        statsMediumTab.onClick.AddListener(() => OnStatsTabSelected("Medium"));
        statsHardTab.onClick.AddListener(() => OnStatsTabSelected("Hard"));
        statsResetButton.onClick.AddListener(OnStatsResetClicked);
        statsResetButtonText = statsResetButton.GetComponentInChildren<TextMeshProUGUI>();

        // Settings buttons
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (gameSettingsButton != null) gameSettingsButton.onClick.AddListener(OpenSettings);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (musicToggle != null) musicToggle.onValueChanged.AddListener(OnMusicToggled);
        if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(OnSfxToggled);

        // Initial state
        menuPanel.SetActive(true);
        topPanel.SetActive(false);
        endGamePanel.SetActive(false);
        statsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
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

    private void OnDifficultySelected(int w, int h, int mines, string difficulty)
    {
        currentDifficulty = difficulty;
        menuPanel.SetActive(false);
        topPanel.SetActive(true);
        UpdateMineCounter(mines);
        UpdateTimer(0);
        UpdateFace(GameState.Playing);
        board.InitializeBoard(w, h, mines, difficulty);

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

    // ========== STATS PANEL ==========

    private void OnStatsClicked()
    {
        menuPanel.SetActive(false);
        statsPanel.SetActive(true);
        selectedStatsTab = "Easy";
        resetConfirmPending = false;
        RefreshStatsDisplay();
    }

    private void OnStatsBackClicked()
    {
        statsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    private void OnStatsTabSelected(string difficulty)
    {
        selectedStatsTab = difficulty;
        resetConfirmPending = false;
        RefreshStatsDisplay();
    }

    private void OnStatsResetClicked()
    {
        if (!resetConfirmPending)
        {
            resetConfirmPending = true;
            statsResetButtonText.text = "Are You Sure?";
        }
        else
        {
            statsManager.ResetStats(selectedStatsTab);
            resetConfirmPending = false;
            statsResetButtonText.text = "Reset Stats";
            RefreshStatsDisplay();
        }
    }

    private void RefreshStatsDisplay()
    {
        UpdateTabVisuals();

        DifficultyStats stats = statsManager.GetStats(selectedStatsTab);
        List<int> bestTimes = statsManager.GetBestTimes(selectedStatsTab);

        string text = $"Toplam Oyun: {stats.gamesPlayed}\n" +
                      $"Kazanilan: {stats.gamesWon}\n" +
                      $"Kazanma Orani: %{stats.winRate:F0}\n" +
                      $"Mevcut Seri: {stats.currentStreak}\n" +
                      $"En Iyi Seri: {stats.bestStreak}\n\n" +
                      "En Iyi Sureler:\n";

        for (int i = 0; i < 5; i++)
        {
            if (i < bestTimes.Count)
                text += $"{i + 1}. {FormatTime(bestTimes[i])}\n";
            else
                text += $"{i + 1}. ---\n";
        }

        statsContentText.text = text;
        statsResetButtonText.text = resetConfirmPending ? "Are You Sure?" : "Reset Stats";
    }

    private void UpdateTabVisuals()
    {
        SetTabState(statsEasyTab, selectedStatsTab == "Easy",
            easyTabActiveSprite, easyTabInactiveSprite);
        SetTabState(statsMediumTab, selectedStatsTab == "Medium",
            mediumTabActiveSprite, mediumTabInactiveSprite);
        SetTabState(statsHardTab, selectedStatsTab == "Hard",
            hardTabActiveSprite, hardTabInactiveSprite);
    }

    private void SetTabState(Button tab, bool active, Sprite activeSprite, Sprite inactiveSprite)
    {
        Image img = tab.GetComponent<Image>();
        img.sprite = active ? activeSprite : inactiveSprite;
        img.color = Color.white;

        TextMeshProUGUI label = tab.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.color = active ? TabActiveTextColor : TabInactiveTextColor;
    }

    // ========== BUTTON CLICK SOUND ==========

    private void AddClickSound(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(() => SoundManager.Instance?.PlayButtonClick());
    }

    // ========== SETTINGS ==========

    private void OpenSettings()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(true);

        var sm = SoundManager.Instance;
        if (sm != null)
        {
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(sm.GetMusicVolume());
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(sm.GetSfxVolume());
            if (musicToggle != null) musicToggle.SetIsOnWithoutNotify(sm.IsMusicEnabled());
            if (sfxToggle != null) sfxToggle.SetIsOnWithoutNotify(sm.IsSfxEnabled());
        }
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnMusicVolumeChanged(float value)
    {
        SoundManager.Instance?.SetMusicUserVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        SoundManager.Instance?.SetSfxUserVolume(value);
    }

    private void OnMusicToggled(bool isOn)
    {
        SoundManager.Instance?.SetMusicEnabled(isOn);
    }

    private void OnSfxToggled(bool isOn)
    {
        SoundManager.Instance?.SetSfxEnabled(isOn);
    }

    // ========== PUBLIC API ==========

    public void UpdateMineCounter(int remaining)
    {
        mineCounterText.text = remaining.ToString("D2");
    }

    public void UpdateTimer(int totalSeconds)
    {
        timerText.text = FormatTime(totalSeconds);
    }

    private string FormatTime(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return minutes + ":" + seconds.ToString("D2");
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

    public void ShowEndGame(bool won, int timeSeconds, bool isNewRecord)
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

        // End game stats
        if (endGameStatsText != null && statsManager != null)
        {
            DifficultyStats stats = statsManager.GetStats(currentDifficulty);
            string statsText = "";

            if (won)
            {
                statsText = $"Sure: {FormatTime(timeSeconds)}";
                if (isNewRecord)
                    statsText += "  <color=#FFD700>Yeni Rekor!</color>";
                statsText += "\n";
            }

            statsText += $"Kazanma Orani: %{stats.winRate:F0}";
            endGameStatsText.text = statsText;
        }
    }

    private void OnRestartClicked()
    {
        board.RestartGame();
    }
}
