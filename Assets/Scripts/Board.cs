using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private int width = 9;
    [SerializeField] private int height = 9;
    [SerializeField] private int mineCount = 10;
    [SerializeField] private bool debugMode = true;

    private Cell[,] cells;
    private GameObject[,] cellObjects;

    private const float CellSize = 1f;
    private const float CellScale = 0.9f;

    private static readonly Color ClosedColor = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color MineColor = new Color(0.9f, 0.2f, 0.2f);

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
        PlaceMines(new Vector2Int(0, 0));
        CalculateNumbers();
        DrawBoard();
        CenterCamera();
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

    private void DrawBoard()
    {
        cellObjects = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cells[x, y];

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

                if (debugMode)
                {
                    if (cell.type == CellType.Mine)
                    {
                        sr.color = MineColor;
                    }
                    else
                    {
                        sr.color = ClosedColor;
                    }

                    if (cell.type == CellType.Number && cell.number > 0)
                    {
                        CreateNumberText(cellGO, cell.number);
                    }
                }
                else
                {
                    sr.color = ClosedColor;
                }

                cellObjects[x, y] = cellGO;
            }
        }
    }

    private void CreateNumberText(GameObject parent, int number)
    {
        GameObject textGO = new GameObject("Number");
        textGO.transform.parent = parent.transform;
        textGO.transform.localPosition = Vector3.zero;

        TextMesh textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = number.ToString();
        textMesh.characterSize = 0.3f;
        textMesh.fontSize = 44;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = NumberColors[number];

        // Render text in front of the sprite
        MeshRenderer mr = textGO.GetComponent<MeshRenderer>();
        mr.sortingOrder = 1;

        // Keep text at unit scale so it stays inside the 0.9 cell
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
}
