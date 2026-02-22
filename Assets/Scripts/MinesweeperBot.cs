using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinesweeperBot : MonoBehaviour
{
    private Board board;
    private bool isRunning = false;
    private Coroutine botCoroutine;
    private Vector2Int? highlightedCell = null;

    private static readonly Color HighlightSafe = new Color(0.3f, 0.9f, 0.3f);
    private static readonly Color HighlightMine = new Color(0.95f, 0.3f, 0.3f);
    private static readonly Color HighlightGuess = new Color(0.95f, 0.9f, 0.2f);

    private const float HighlightDuration = 0.15f;
    private const float MoveDelay = 0.15f;

    public bool IsRunning => isRunning;

    public void Initialize(Board board)
    {
        this.board = board;
    }

    public void StartBot()
    {
        if (isRunning) return;
        isRunning = true;
        board.IsBotPlaying = true;
        botCoroutine = StartCoroutine(BotLoop());
    }

    public void StopBot()
    {
        if (!isRunning) return;
        isRunning = false;
        board.IsBotPlaying = false;
        if (botCoroutine != null)
        {
            StopCoroutine(botCoroutine);
            botCoroutine = null;
        }
        // Restore any highlighted cell
        if (highlightedCell.HasValue)
        {
            board.RestoreCellVisual(highlightedCell.Value.x, highlightedCell.Value.y);
            highlightedCell = null;
        }
    }

    private IEnumerator BotLoop()
    {
        // If first click hasn't happened, click center
        if (!board.IsFirstClickDone())
        {
            int w = board.GetWidth();
            int h = board.GetHeight();
            int cx = w / 2;
            int cy = h / 2;
            yield return DoHighlight(cx, cy, HighlightGuess);
            board.BotRevealCell(cx, cy);
            yield return new WaitForSeconds(MoveDelay);
        }

        while (!board.IsGameOver() && isRunning)
        {
            // Layer 1: Safe reveals
            List<Vector2Int> safeReveals = FindSafeReveals();
            if (safeReveals.Count > 0)
            {
                foreach (var pos in safeReveals)
                {
                    if (board.IsGameOver() || !isRunning) break;
                    yield return DoHighlight(pos.x, pos.y, HighlightSafe);
                    board.BotRevealCell(pos.x, pos.y);
                    yield return new WaitForSeconds(MoveDelay);
                }
                continue; // Restart loop - new reveals may unlock more safe moves
            }

            // Layer 2: Definite mine flags
            List<Vector2Int> mineFlags = FindDefiniteMines();
            if (mineFlags.Count > 0)
            {
                foreach (var pos in mineFlags)
                {
                    if (board.IsGameOver() || !isRunning) break;
                    yield return DoHighlight(pos.x, pos.y, HighlightMine);
                    board.BotToggleFlag(pos.x, pos.y);
                    yield return new WaitForSeconds(MoveDelay);
                }
                continue; // New flags may unlock Layer 1 moves
            }

            // Layer 3: Probability-based guess
            Vector2Int? guess = FindBestGuess();
            if (guess.HasValue)
            {
                yield return DoHighlight(guess.Value.x, guess.Value.y, HighlightGuess);
                board.BotRevealCell(guess.Value.x, guess.Value.y);
                yield return new WaitForSeconds(MoveDelay);
                continue;
            }

            // No moves possible
            break;
        }

        isRunning = false;
        board.IsBotPlaying = false;
    }

    private IEnumerator DoHighlight(int x, int y, Color color)
    {
        highlightedCell = new Vector2Int(x, y);
        board.HighlightCell(x, y, color);
        yield return new WaitForSeconds(HighlightDuration);
        highlightedCell = null;
    }

    // ========== LAYER 1: SAFE REVEALS ==========
    // If a numbered cell's flag count equals its number,
    // all remaining closed neighbors are safe.

    private List<Vector2Int> FindSafeReveals()
    {
        HashSet<Vector2Int> safe = new HashSet<Vector2Int>();
        int w = board.GetWidth();
        int h = board.GetHeight();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = board.GetCell(x, y);
                if (!cell.isRevealed || cell.number <= 0) continue;

                int flags = 0;
                List<Vector2Int> closed = new List<Vector2Int>();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        Cell neighbor = board.GetCell(nx, ny);
                        if (neighbor.isFlagged) flags++;
                        else if (!neighbor.isRevealed) closed.Add(new Vector2Int(nx, ny));
                    }
                }

                if (flags == cell.number && closed.Count > 0)
                {
                    foreach (var pos in closed)
                        safe.Add(pos);
                }
            }
        }

        return new List<Vector2Int>(safe);
    }

    // ========== LAYER 2: DEFINITE MINES ==========
    // If (number - flags) equals the count of remaining closed neighbors,
    // all those closed neighbors are mines.

    private List<Vector2Int> FindDefiniteMines()
    {
        HashSet<Vector2Int> mines = new HashSet<Vector2Int>();
        int w = board.GetWidth();
        int h = board.GetHeight();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = board.GetCell(x, y);
                if (!cell.isRevealed || cell.number <= 0) continue;

                int flags = 0;
                List<Vector2Int> closed = new List<Vector2Int>();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        Cell neighbor = board.GetCell(nx, ny);
                        if (neighbor.isFlagged) flags++;
                        else if (!neighbor.isRevealed) closed.Add(new Vector2Int(nx, ny));
                    }
                }

                int remainingMines = cell.number - flags;
                if (remainingMines > 0 && remainingMines == closed.Count)
                {
                    foreach (var pos in closed)
                        mines.Add(pos);
                }
            }
        }

        return new List<Vector2Int>(mines);
    }

    // ========== LAYER 3: PROBABILITY GUESS ==========
    // For each closed unflagged cell, compute a danger score from
    // neighboring numbered cells. Pick the cell with lowest danger.

    private Vector2Int? FindBestGuess()
    {
        int w = board.GetWidth();
        int h = board.GetHeight();

        float bestScore = float.MaxValue;
        Vector2Int? bestCell = null;
        List<Vector2Int> unknownCells = new List<Vector2Int>();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = board.GetCell(x, y);
                if (cell.isRevealed || cell.isFlagged) continue;

                float maxDanger = 0f;
                bool hasNumberedNeighbor = false;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        Cell neighbor = board.GetCell(nx, ny);
                        if (!neighbor.isRevealed || neighbor.number <= 0) continue;

                        hasNumberedNeighbor = true;

                        // Count flags and closed around this numbered neighbor
                        int nFlags = 0, nClosed = 0;
                        for (int ddx = -1; ddx <= 1; ddx++)
                        {
                            for (int ddy = -1; ddy <= 1; ddy++)
                            {
                                if (ddx == 0 && ddy == 0) continue;
                                int nnx = nx + ddx, nny = ny + ddy;
                                if (nnx < 0 || nnx >= w || nny < 0 || nny >= h) continue;
                                Cell nn = board.GetCell(nnx, nny);
                                if (nn.isFlagged) nFlags++;
                                else if (!nn.isRevealed) nClosed++;
                            }
                        }

                        if (nClosed > 0)
                        {
                            float danger = (float)(neighbor.number - nFlags) / nClosed;
                            if (danger > maxDanger) maxDanger = danger;
                        }
                    }
                }

                if (hasNumberedNeighbor)
                {
                    if (maxDanger < bestScore)
                    {
                        bestScore = maxDanger;
                        bestCell = new Vector2Int(x, y);
                    }
                }
                else
                {
                    unknownCells.Add(new Vector2Int(x, y));
                }
            }
        }

        if (bestCell.HasValue) return bestCell;

        // No cell with numbered neighbor - pick random from unknown area
        if (unknownCells.Count > 0)
        {
            return unknownCells[Random.Range(0, unknownCells.Count)];
        }

        return null;
    }
}
