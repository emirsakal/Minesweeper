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
    [SerializeField] private int width = 9;
    [SerializeField] private int height = 9;
    [SerializeField] private int mineCount = 10;
    [SerializeField] private bool debugMode = false;

    private Cell[,] cells;
    private GameObject[,] cellObjects;
    private bool firstClickDone = false;
    private GameState gameState = GameState.Playing;

    private Vector2Int explodedMinePos;
    private int flagCount = 0;
    private float timer = 0f;

    private GameUI gameUI;

    private const float CellSize = 1f;
    private const float CellScale = 0.9f;

    private static readonly Color ClosedColor = new Color(0.55f, 0.55f, 0.6f);
    private static readonly Color RevealedColor = new Color(0.9f, 0.88f, 0.82f);
    private static readonly Color MineColor = new Color(0.9f, 0.2f, 0.2f);
    private static readonly Color ExplodedMineColor = new Color(1.0f, 0.5f, 0.0f);
    private static readonly Color WrongFlagColor = new Color(0.9f, 0.88f, 0.82f);

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
        GenerateBoard();
        DrawBoard();
        CenterCamera();
        CreateUI();
    }

    private void Update()
    {
        if (gameState != GameState.Playing) return;

        // Update timer
        if (firstClickDone)
        {
            timer += Time.deltaTime;
            gameUI.UpdateTimer(Mathf.FloorToInt(timer));
        }

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

    private void CreateUI()
    {
        GameObject uiGO = new GameObject("GameUI");
        gameUI = uiGO.AddComponent<GameUI>();
        gameUI.Initialize(mineCount, this);
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
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(x, y));

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            Cell cell = cells[pos.x, pos.y];

            if (cell.isRevealed || cell.isFlagged || cell.type == CellType.Mine)
                continue;

            cell.isRevealed = true;
            UpdateCellVisual(pos.x, pos.y);

            // Only continue spreading from empty (0) cells
            if (cell.type == CellType.Empty)
            {
                List<Cell> neighbors = GetNeighbors(pos.x, pos.y);
                foreach (Cell neighbor in neighbors)
                {
                    if (!neighbor.isRevealed && !neighbor.isFlagged)
                    {
                        queue.Enqueue(neighbor.position);
                    }
                }
            }
        }
    }

    private void ToggleFlag(int x, int y)
    {
        Cell cell = cells[x, y];

        if (cell.isRevealed)
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
        gameUI.ShowEndGame(true);
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
                    // Wrong flag - not a mine but flagged
                    UpdateWrongFlagVisual(x, y);
                }
            }
        }

        gameUI.ShowEndGame(false);
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
                sr.sprite = Resources.Load<Sprite>("Sprites/Square");

                if (sr.sprite == null)
                {
                    sr.sprite = CreateDefaultSprite();
                }

                sr.color = ClosedColor;
                cellObjects[x, y] = cellGO;
            }
        }
    }

    private void UpdateCellVisual(int x, int y)
    {
        Cell cell = cells[x, y];
        GameObject cellGO = cellObjects[x, y];
        SpriteRenderer sr = cellGO.GetComponent<SpriteRenderer>();

        // Clear existing child text objects
        foreach (Transform child in cellGO.transform)
        {
            Destroy(child.gameObject);
        }

        if (!cell.isRevealed)
        {
            sr.color = ClosedColor;

            if (cell.isFlagged)
            {
                CreateTextOnCell(cellGO, "F", MineColor);
            }
        }
        else
        {
            if (cell.type == CellType.Mine)
            {
                bool isExploded = (x == explodedMinePos.x && y == explodedMinePos.y);
                sr.color = isExploded ? ExplodedMineColor : MineColor;
                CreateTextOnCell(cellGO, "*", Color.black);
            }
            else
            {
                sr.color = RevealedColor;

                if (cell.type == CellType.Number && cell.number > 0)
                {
                    CreateTextOnCell(cellGO, cell.number.ToString(), NumberColors[cell.number]);
                }
            }
        }
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
        mr.sortingOrder = 1;

        textGO.transform.localScale = Vector3.one;
    }

    private Sprite CreateDefaultSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void CenterCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float centerX = (width - 1) * CellSize * 0.5f;
        float centerY = (height - 1) * CellSize * 0.5f;
        cam.transform.position = new Vector3(centerX, centerY, -10f);

        cam.orthographic = true;

        float halfHeight = height * CellSize * 0.5f + 1f;
        float halfWidth = width * CellSize * 0.5f + 1f;
        float screenAspect = (float)Screen.width / Screen.height;
        float requiredSize = Mathf.Max(halfHeight, halfWidth / screenAspect);

        cam.orthographicSize = requiredSize;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
