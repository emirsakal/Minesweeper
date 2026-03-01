using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameState
{
    Playing,
    Won,
    Lost
}

public class Board : MonoBehaviour
{
    private int width;
    private int height;
    private int mineCount;

    private Cell[,] cells;
    private GameObject[,] cellObjects;
    private bool boardInitialized = false;
    private bool firstClickDone = false;
    private GameState gameState = GameState.Playing;

    private Vector2Int explodedMinePos;
    private int flagCount = 0;
    private float timer = 0f;

    [SerializeField] private GameUI gameUI;
    [SerializeField] private StatsManager statsManager;
    [SerializeField] private RectTransform gridContainer;
    private string currentDifficulty;

    private float cellSize;
    private float cellScale;

    public bool IsBotPlaying { get; set; }

    // Cell colors
    private static readonly Color ClosedColor = new Color(0.78f, 0.78f, 0.82f);
    private static readonly Color RevealedColor = new Color(0.5f, 0.5f, 0.5f);
    private static readonly Color MineColor = new Color(0.9f, 0.2f, 0.2f);
    private static readonly Color ExplodedMineColor = new Color(1.0f, 0.5f, 0.0f);
    private static readonly Color WrongFlagColor = new Color(0.5f, 0.5f, 0.5f);

    // Bevel colors (closed cell 3D effect)
    private static readonly Color BevelHighlight = new Color(0.92f, 0.92f, 0.96f);
    private static readonly Color BevelShadow = new Color(0.58f, 0.58f, 0.62f);

    // Border for revealed cells
    private static readonly Color RevealedBorder = new Color(0.4f, 0.4f, 0.4f);

    private static readonly Color FlagColor = new Color(0.9f, 0.1f, 0.1f);

    private static readonly Color[] NumberColors = new Color[]
    {
        Color.white,                        // 0 - unused
        new Color(0.0f, 0.0f, 1.0f),       // 1 - blue
        new Color(0.0f, 0.5f, 0.0f),       // 2 - green
        new Color(1.0f, 0.0f, 0.0f),       // 3 - red
        new Color(0.0f, 0.0f, 0.5f),       // 4 - dark blue
        new Color(0.5f, 0.0f, 0.0f),       // 5 - dark red
        new Color(0.0f, 0.5f, 0.5f),       // 6 - teal
        new Color(0.0f, 0.0f, 0.0f),       // 7 - black
        new Color(0.5f, 0.5f, 0.5f),       // 8 - gray
    };

    private void Awake()
    {
        gameUI.Initialize(this);
    }

    private void Update()
    {
        if (!boardInitialized || gameState != GameState.Playing) return;

        if (firstClickDone)
        {
            timer += Time.deltaTime;
            gameUI.UpdateTimer(Mathf.FloorToInt(timer));
        }

        // Block player input while bot is playing
        if (IsBotPlaying) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int? gridPos = GetGridPosition();
            if (gridPos.HasValue)
                HandleLeftClick(gridPos.Value);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Vector2Int? gridPos = GetGridPosition();
            if (gridPos.HasValue)
                ToggleFlag(gridPos.Value.x, gridPos.Value.y);
        }
    }

    public void InitializeBoard(int w, int h, int mines, string difficulty)
    {
        width = w;
        height = h;
        mineCount = mines;
        currentDifficulty = difficulty;

        boardInitialized = true;
        firstClickDone = false;
        gameState = GameState.Playing;
        flagCount = 0;
        timer = 0f;
        IsBotPlaying = false;

        CalculateGridLayout();
        GenerateBoard();
        DrawBoard();
    }

    private void CalculateGridLayout()
    {
        float containerW = gridContainer.rect.width;
        float containerH = gridContainer.rect.height;
        cellSize = Mathf.Min(containerW / width, containerH / height);
        cellScale = cellSize * 0.92f;
    }

    private Vector2Int? GetGridPosition()
    {
        Vector2 localPoint;
        // null camera = Screen Space Overlay canvas
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridContainer, Input.mousePosition, null, out localPoint))
            return null;

        float totalW = width * cellSize;
        float totalH = height * cellSize;

        int x = Mathf.FloorToInt((localPoint.x + totalW * 0.5f) / cellSize);
        int y = Mathf.FloorToInt((localPoint.y + totalH * 0.5f) / cellSize);

        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;

        return new Vector2Int(x, y);
    }

    private void HandleLeftClick(Vector2Int pos)
    {
        if (!firstClickDone)
        {
            firstClickDone = true;
            PlaceMines(pos);
            CalculateNumbers();
        }

        RevealCell(pos.x, pos.y);
    }

    private void GenerateBoard()
    {
        cells = new Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell(new Vector2Int(x, y));
            }
        }
    }

    private void PlaceMines(Vector2Int firstClick)
    {
        HashSet<Vector2Int> safeCells = new HashSet<Vector2Int>();
        safeCells.Add(firstClick);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = firstClick.x + dx;
                int ny = firstClick.y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    safeCells.Add(new Vector2Int(nx, ny));
                }
            }
        }

        int placed = 0;
        int maxAttempts = width * height * 10;
        int attempts = 0;

        while (placed < mineCount && attempts < maxAttempts)
        {
            attempts++;
            int rx = Random.Range(0, width);
            int ry = Random.Range(0, height);
            Vector2Int pos = new Vector2Int(rx, ry);

            if (safeCells.Contains(pos))
                continue;

            if (cells[rx, ry].type == CellType.Mine)
                continue;

            cells[rx, ry].type = CellType.Mine;
            placed++;
        }
    }

    private void CalculateNumbers()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y].type == CellType.Mine)
                    continue;

                int count = 0;
                List<Cell> neighbors = GetNeighbors(x, y);

                foreach (Cell neighbor in neighbors)
                {
                    if (neighbor.type == CellType.Mine)
                        count++;
                }

                cells[x, y].number = count;
                cells[x, y].type = count > 0 ? CellType.Number : CellType.Empty;
            }
        }
    }

    private List<Cell> GetNeighbors(int x, int y)
    {
        List<Cell> neighbors = new List<Cell>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    neighbors.Add(cells[nx, ny]);
                }
            }
        }

        return neighbors;
    }

    private void RevealCell(int x, int y)
    {
        Cell cell = cells[x, y];

        if (cell.isRevealed || cell.isFlagged)
            return;

        if (cell.type == CellType.Mine)
        {
            GameOver(x, y);
            return;
        }

        if (cell.type == CellType.Number)
        {
            cell.isRevealed = true;
            UpdateCellVisual(x, y);
            CheckWin();
            return;
        }

        // Empty cell - flood fill
        FloodFill(x, y);
        CheckWin();
    }

    private void FloodFill(int x, int y)
    {
        // BFS collecting cells with wave depth numbers
        var waveCells = new List<KeyValuePair<Vector2Int, int>>();
        var queue = new Queue<KeyValuePair<Vector2Int, int>>();

        Vector2Int start = new Vector2Int(x, y);
        queue.Enqueue(new KeyValuePair<Vector2Int, int>(start, 0));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            Vector2Int pos = current.Key;
            int wave = current.Value;
            Cell cell = cells[pos.x, pos.y];

            if (cell.isRevealed || cell.isFlagged || cell.type == CellType.Mine)
                continue;

            // Mark logically revealed IMMEDIATELY (game logic stays synchronous)
            cell.isRevealed = true;
            waveCells.Add(new KeyValuePair<Vector2Int, int>(pos, wave));

            if (cell.type == CellType.Empty)
            {
                List<Cell> neighbors = GetNeighbors(pos.x, pos.y);
                foreach (Cell neighbor in neighbors)
                {
                    if (!neighbor.isRevealed && !neighbor.isFlagged)
                    {
                        queue.Enqueue(new KeyValuePair<Vector2Int, int>(neighbor.position, wave + 1));
                    }
                }
            }
        }

        // Start visual wave animation (runs in background, doesn't block gameplay)
        StartCoroutine(AnimateFloodFill(waveCells));
    }

    private IEnumerator AnimateFloodFill(List<KeyValuePair<Vector2Int, int>> waveCells)
    {
        if (waveCells.Count == 0) yield break;

        int currentWave = 0;
        int i = 0;

        while (i < waveCells.Count)
        {
            // Reveal all cells in the current wave simultaneously
            while (i < waveCells.Count && waveCells[i].Value == currentWave)
            {
                Vector2Int pos = waveCells[i].Key;
                UpdateCellVisual(pos.x, pos.y);
                StartCoroutine(AnimateCellScale(pos.x, pos.y));
                i++;
            }

            currentWave++;

            // Short delay between waves (0.04s)
            yield return new WaitForSeconds(0.04f);
        }
    }

    private IEnumerator AnimateCellScale(int x, int y)
    {
        GameObject cellGO = cellObjects[x, y];
        float duration = 0.1f;
        float elapsed = 0f;
        float startS = 0.55f;
        float endS = 1f;

        cellGO.transform.localScale = new Vector3(startS, startS, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease-out: 1 - (1-t)^2
            t = 1f - (1f - t) * (1f - t);
            float s = Mathf.Lerp(startS, endS, t);
            cellGO.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        cellGO.transform.localScale = Vector3.one;
    }

    private void ToggleFlag(int x, int y)
    {
        Cell cell = cells[x, y];

        if (cell.isRevealed)
            return;

        // Prevent placing more flags than mines
        if (!cell.isFlagged && flagCount >= mineCount)
            return;

        cell.isFlagged = !cell.isFlagged;
        flagCount += cell.isFlagged ? 1 : -1;
        UpdateCellVisual(x, y);
        gameUI.UpdateMineCounter(mineCount - flagCount);
    }

    private void CheckWin()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cells[x, y];
                if (cell.type != CellType.Mine && !cell.isRevealed)
                    return;
            }
        }

        gameState = GameState.Won;
        Debug.Log("You Win!");
        int timeSeconds = Mathf.FloorToInt(timer);
        bool isNewRecord = statsManager.RecordGame(currentDifficulty, true, timeSeconds);
        gameUI.ShowEndGame(true, timeSeconds, isNewRecord);
    }

    private void GameOver(int clickedX, int clickedY)
    {
        gameState = GameState.Lost;
        explodedMinePos = new Vector2Int(clickedX, clickedY);
        Debug.Log("Game Over!");

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cells[x, y];

                if (cell.type == CellType.Mine)
                {
                    cell.isRevealed = true;
                    UpdateCellVisual(x, y);
                }
                else if (cell.isFlagged)
                {
                    UpdateWrongFlagVisual(x, y);
                }
            }
        }

        int timeSeconds = Mathf.FloorToInt(timer);
        statsManager.RecordGame(currentDifficulty, false, timeSeconds);
        gameUI.ShowEndGame(false, timeSeconds, false);
    }

    private void UpdateWrongFlagVisual(int x, int y)
    {
        GameObject cellGO = cellObjects[x, y];
        Image img = cellGO.GetComponent<Image>();

        foreach (Transform child in cellGO.transform)
        {
            Destroy(child.gameObject);
        }

        img.color = WrongFlagColor;
        AddBorder(cellGO);
        CreateTextOnCell(cellGO, "X", MineColor);
    }

    // ========== UI GRID DRAWING ==========

    private void DrawBoard()
    {
        cellObjects = new GameObject[width, height];

        float totalW = width * cellSize;
        float totalH = height * cellSize;
        float startX = -totalW * 0.5f + cellSize * 0.5f;
        float startY = -totalH * 0.5f + cellSize * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cellGO = new GameObject($"Cell ({x}, {y})");
                cellGO.transform.SetParent(gridContainer, false);

                RectTransform rt = cellGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cellScale, cellScale);
                rt.anchoredPosition = new Vector2(
                    startX + x * cellSize,
                    startY + y * cellSize);

                Image img = cellGO.AddComponent<Image>();
                img.raycastTarget = false;
                cellObjects[x, y] = cellGO;

                UpdateCellVisual(x, y);
            }
        }
    }

    private void UpdateCellVisual(int x, int y)
    {
        Cell cell = cells[x, y];
        GameObject cellGO = cellObjects[x, y];
        Image img = cellGO.GetComponent<Image>();

        // Clear existing child objects
        foreach (Transform child in cellGO.transform)
        {
            Destroy(child.gameObject);
        }

        if (!cell.isRevealed)
        {
            img.color = ClosedColor;
            AddBevel(cellGO);

            if (cell.isFlagged)
            {
                CreateTextOnCell(cellGO, "\u25B6", FlagColor);
            }
        }
        else
        {
            if (cell.type == CellType.Mine)
            {
                bool isExploded = (x == explodedMinePos.x && y == explodedMinePos.y);
                img.color = isExploded ? ExplodedMineColor : MineColor;
                CreateTextOnCell(cellGO, "\u25CF", Color.black);
            }
            else
            {
                img.color = RevealedColor;
                AddBorder(cellGO);

                if (cell.type == CellType.Number && cell.number > 0)
                {
                    CreateTextOnCell(cellGO, cell.number.ToString(), NumberColors[cell.number]);
                }
            }
        }
    }

    private void AddBevel(GameObject cellGO)
    {
        float bevel = cellScale * 0.08f;
        float offset = cellScale * 0.5f - bevel * 0.5f;
        float innerLen = cellScale - bevel * 2f;

        // Top + Left = highlight
        CreateEdge(cellGO, new Vector2(0, offset), new Vector2(cellScale, bevel), BevelHighlight);
        CreateEdge(cellGO, new Vector2(-offset, 0), new Vector2(bevel, innerLen), BevelHighlight);
        // Bottom + Right = shadow
        CreateEdge(cellGO, new Vector2(0, -offset), new Vector2(cellScale, bevel), BevelShadow);
        CreateEdge(cellGO, new Vector2(offset, 0), new Vector2(bevel, innerLen), BevelShadow);
    }

    private void AddBorder(GameObject cellGO)
    {
        float border = cellScale * 0.03f;
        float offset = cellScale * 0.5f - border * 0.5f;
        float innerLen = cellScale - border * 2f;

        CreateEdge(cellGO, new Vector2(0, offset), new Vector2(cellScale, border), RevealedBorder);
        CreateEdge(cellGO, new Vector2(-offset, 0), new Vector2(border, innerLen), RevealedBorder);
        CreateEdge(cellGO, new Vector2(0, -offset), new Vector2(cellScale, border), RevealedBorder);
        CreateEdge(cellGO, new Vector2(offset, 0), new Vector2(border, innerLen), RevealedBorder);
    }

    private void CreateEdge(GameObject parent, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject edge = new GameObject("Edge");
        edge.transform.SetParent(parent.transform, false);

        RectTransform rt = edge.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = edge.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private void CreateTextOnCell(GameObject parent, string text, Color color)
    {
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(parent.transform, false);

        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = cellScale * 0.55f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
    }

    // ========== PUBLIC API FOR BOT ==========

    public Cell GetCell(int x, int y)
    {
        return cells[x, y];
    }

    public int GetWidth()
    {
        return width;
    }

    public int GetHeight()
    {
        return height;
    }

    public bool IsGameOver()
    {
        return gameState != GameState.Playing;
    }

    public bool IsFirstClickDone()
    {
        return firstClickDone;
    }

    public int GetMineCount()
    {
        return mineCount;
    }

    public int GetFlagCount()
    {
        return flagCount;
    }

    public string GetDifficulty()
    {
        return currentDifficulty;
    }

    public void BotRevealCell(int x, int y)
    {
        if (gameState != GameState.Playing) return;

        if (!firstClickDone)
        {
            firstClickDone = true;
            PlaceMines(new Vector2Int(x, y));
            CalculateNumbers();
        }

        RevealCell(x, y);
    }

    public void BotToggleFlag(int x, int y)
    {
        if (gameState != GameState.Playing) return;
        ToggleFlag(x, y);
    }

    public void HighlightCell(int x, int y, Color color)
    {
        if (cellObjects != null && x >= 0 && x < width && y >= 0 && y < height)
        {
            Image img = cellObjects[x, y].GetComponent<Image>();
            img.color = color;
        }
    }

    public void RestoreCellVisual(int x, int y)
    {
        if (cellObjects != null && x >= 0 && x < width && y >= 0 && y < height)
        {
            UpdateCellVisual(x, y);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
