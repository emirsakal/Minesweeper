using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private Sprite cachedSprite;
    private string currentDifficulty;

    public bool IsBotPlaying { get; set; }

    private const float CellSize = 1f;
    private const float CellScale = 0.9f;

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
        SetCameraBackground();
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

    private void SetCameraBackground()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
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

        GenerateBoard();
        DrawBoard();
        CenterCamera();
    }

    private Vector2Int? GetGridPosition()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int x = Mathf.RoundToInt(worldPos.x / CellSize);
        int y = Mathf.RoundToInt(worldPos.y / CellSize);

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
        float startScale = 0.5f;
        float endScale = CellScale;

        cellGO.transform.localScale = new Vector3(startScale, startScale, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease-out: 1 - (1-t)^2
            t = 1f - (1f - t) * (1f - t);
            float s = Mathf.Lerp(startScale, endScale, t);
            cellGO.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        cellGO.transform.localScale = new Vector3(endScale, endScale, 1f);
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
        SpriteRenderer sr = cellGO.GetComponent<SpriteRenderer>();

        foreach (Transform child in cellGO.transform)
        {
            Destroy(child.gameObject);
        }

        sr.color = WrongFlagColor;
        AddBorder(cellGO);
        CreateTextOnCell(cellGO, "X", MineColor);
    }

    private void DrawBoard()
    {
        cellObjects = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cellGO = new GameObject($"Cell ({x}, {y})");
                cellGO.transform.parent = transform;
                cellGO.transform.position = new Vector3(x * CellSize, y * CellSize, 0f);
                cellGO.transform.localScale = new Vector3(CellScale, CellScale, 1f);

                SpriteRenderer sr = cellGO.AddComponent<SpriteRenderer>();
                sr.sprite = GetSprite();
                cellObjects[x, y] = cellGO;

                UpdateCellVisual(x, y);
            }
        }
    }

    private void UpdateCellVisual(int x, int y)
    {
        Cell cell = cells[x, y];
        GameObject cellGO = cellObjects[x, y];
        SpriteRenderer sr = cellGO.GetComponent<SpriteRenderer>();

        // Clear existing child objects
        foreach (Transform child in cellGO.transform)
        {
            Destroy(child.gameObject);
        }

        if (!cell.isRevealed)
        {
            sr.color = ClosedColor;
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
                sr.color = isExploded ? ExplodedMineColor : MineColor;
                CreateTextOnCell(cellGO, "\u25CF", Color.black);
            }
            else
            {
                sr.color = RevealedColor;
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
        float size = 0.08f;
        float offset = 0.5f - size / 2f;
        float innerLen = 1f - size * 2f;

        // Top + Left = highlight
        CreateEdge(cellGO, new Vector3(0, offset, 0), new Vector3(1, size, 1), BevelHighlight);
        CreateEdge(cellGO, new Vector3(-offset, 0, 0), new Vector3(size, innerLen, 1), BevelHighlight);
        // Bottom + Right = shadow
        CreateEdge(cellGO, new Vector3(0, -offset, 0), new Vector3(1, size, 1), BevelShadow);
        CreateEdge(cellGO, new Vector3(offset, 0, 0), new Vector3(size, innerLen, 1), BevelShadow);
    }

    private void AddBorder(GameObject cellGO)
    {
        float size = 0.03f;
        float offset = 0.5f - size / 2f;
        float innerLen = 1f - size * 2f;

        CreateEdge(cellGO, new Vector3(0, offset, 0), new Vector3(1, size, 1), RevealedBorder);
        CreateEdge(cellGO, new Vector3(-offset, 0, 0), new Vector3(size, innerLen, 1), RevealedBorder);
        CreateEdge(cellGO, new Vector3(0, -offset, 0), new Vector3(1, size, 1), RevealedBorder);
        CreateEdge(cellGO, new Vector3(offset, 0, 0), new Vector3(size, innerLen, 1), RevealedBorder);
    }

    private void CreateEdge(GameObject parent, Vector3 localPos, Vector3 localScale, Color color)
    {
        GameObject edge = new GameObject("Edge");
        edge.transform.SetParent(parent.transform, false);
        edge.transform.localPosition = localPos;
        edge.transform.localScale = localScale;

        SpriteRenderer sr = edge.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.color = color;
        sr.sortingOrder = 1;
    }

    private void CreateTextOnCell(GameObject parent, string text, Color color)
    {
        GameObject textGO = new GameObject("Text");
        textGO.transform.parent = parent.transform;
        textGO.transform.localPosition = Vector3.zero;

        TextMesh textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = 0.2f;
        textMesh.fontSize = 40;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;

        MeshRenderer mr = textGO.GetComponent<MeshRenderer>();
        mr.sortingOrder = 2;

        textGO.transform.localScale = Vector3.one;
    }

    private Sprite GetSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        cachedSprite = Resources.Load<Sprite>("Sprites/Square");
        if (cachedSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            cachedSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        return cachedSprite;
    }

    private void CenterCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        float screenAspect = (float)Screen.width / Screen.height;

        // Grid bounds in world units (with padding)
        float gridWidth = width * CellSize;
        float gridHeight = height * CellSize;
        float padSide = 1f;
        float padBottom = 0.5f;

        // Top panel is ~50px; reserve that space in world units
        float panelPixels = 50f;

        // Determine orthographic size from grid + side padding
        float halfHeight = gridHeight * 0.5f + padBottom;
        float halfWidth = gridWidth * 0.5f + padSide;
        float sizeFromHeight = halfHeight;
        float sizeFromWidth = halfWidth / screenAspect;
        float orthoSize = Mathf.Max(sizeFromHeight, sizeFromWidth);

        // Convert panel pixel height to world units and add to ortho size
        float panelWorld = panelPixels / Screen.height * orthoSize * 2f;
        orthoSize += panelWorld * 0.5f;

        cam.orthographicSize = orthoSize;

        // Center grid, then shift camera down so the panel gap is at top
        float centerX = (width - 1) * CellSize * 0.5f;
        float centerY = (height - 1) * CellSize * 0.5f;
        float offsetY = panelWorld * 0.5f;
        cam.transform.position = new Vector3(centerX, centerY - offsetY, -10f);
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
            SpriteRenderer sr = cellObjects[x, y].GetComponent<SpriteRenderer>();
            sr.color = color;
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
