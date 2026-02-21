using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private int width = 9;
    [SerializeField] private int height = 9;
    [SerializeField] private int mineCount = 10;

    private Cell[,] cells;
    private GameObject[,] cellObjects;

    private const float CellSize = 1f;
    private const float CellScale = 0.9f;

    private void Awake()
    {
        GenerateBoard();
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

    private void DrawBoard()
    {
        cellObjects = new GameObject[width, height];
        Color closedColor = new Color(0.7f, 0.7f, 0.7f);

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

                sr.color = closedColor;

                cellObjects[x, y] = cellGO;
            }
        }
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
