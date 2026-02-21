using UnityEngine;

public enum CellType
{
    Empty,
    Mine,
    Number
}

public class Cell
{
    public Vector2Int position;
    public CellType type;
    public int number; // 0-8
    public bool isRevealed;
    public bool isFlagged;

    public Cell(Vector2Int position)
    {
        this.position = position;
        this.type = CellType.Empty;
        this.number = 0;
        this.isRevealed = false;
        this.isFlagged = false;
    }
}
