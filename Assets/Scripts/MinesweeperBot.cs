using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinesweeperBot : MonoBehaviour
{
    private Board board;
    private bool isRunning = false;
    private Coroutine botCoroutine;
    private Vector2Int? highlightedCell = null;

    // Layer 1 & 2: standard colors
    private static readonly Color HighlightSafe = new Color(0.3f, 0.9f, 0.3f);
    private static readonly Color HighlightMine = new Color(0.95f, 0.3f, 0.3f);
    // Layer 2.5: distinct colors for subset analysis
    private static readonly Color HighlightSubsetSafe = new Color(0.2f, 0.8f, 0.9f);
    private static readonly Color HighlightSubsetMine = new Color(0.95f, 0.6f, 0.2f);
    // Layer 3: guess
    private static readonly Color HighlightGuess = new Color(0.95f, 0.9f, 0.2f);

    private const float HighlightDuration = 0.1f;
    private const float MoveDelay = 0.05f;

    public bool IsRunning => isRunning;

    // Constraint info for subset analysis
    private struct Constraint
    {
        public Vector2Int position;
        public int remaining;
        public HashSet<Vector2Int> closed;
    }

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
        if (highlightedCell.HasValue)
        {
            board.RestoreCellVisual(highlightedCell.Value.x, highlightedCell.Value.y);
            highlightedCell = null;
        }
    }

    private IEnumerator BotLoop()
    {
        // First click: center of board
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
                yield return ExecuteReveals(safeReveals, HighlightSafe);
                continue;
            }

            // Layer 2: Definite mine flags
            List<Vector2Int> mineFlags = FindDefiniteMines();
            if (mineFlags.Count > 0)
            {
                yield return ExecuteFlags(mineFlags, HighlightMine);
                continue;
            }

            // Layer 2.5: Subset / constraint analysis
            var subsetSafe = new HashSet<Vector2Int>();
            var subsetMines = new HashSet<Vector2Int>();
            FindSubsetMoves(subsetSafe, subsetMines);
            if (subsetSafe.Count > 0 || subsetMines.Count > 0)
            {
                if (subsetSafe.Count > 0)
                    yield return ExecuteReveals(new List<Vector2Int>(subsetSafe), HighlightSubsetSafe);
                if (subsetMines.Count > 0)
                    yield return ExecuteFlags(new List<Vector2Int>(subsetMines), HighlightSubsetMine);
                continue;
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

            break;
        }

        isRunning = false;
        board.IsBotPlaying = false;
    }

    // ========== EXECUTION HELPERS ==========

    private IEnumerator ExecuteReveals(List<Vector2Int> cells, Color color)
    {
        foreach (var pos in cells)
        {
            if (board.IsGameOver() || !isRunning) break;
            Cell c = board.GetCell(pos.x, pos.y);
            if (c.isRevealed || c.isFlagged) continue;
            yield return DoHighlight(pos.x, pos.y, color);
            board.BotRevealCell(pos.x, pos.y);
            yield return new WaitForSeconds(MoveDelay);
        }
    }

    private IEnumerator ExecuteFlags(List<Vector2Int> cells, Color color)
    {
        foreach (var pos in cells)
        {
            if (board.IsGameOver() || !isRunning) break;
            Cell c = board.GetCell(pos.x, pos.y);
            if (c.isRevealed || c.isFlagged) continue;
            yield return DoHighlight(pos.x, pos.y, color);
            board.BotToggleFlag(pos.x, pos.y);
            yield return new WaitForSeconds(MoveDelay);
        }
    }

    private IEnumerator DoHighlight(int x, int y, Color color)
    {
        highlightedCell = new Vector2Int(x, y);
        board.HighlightCell(x, y, color);
        yield return new WaitForSeconds(HighlightDuration);
        board.RestoreCellVisual(x, y);
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

    // ========== LAYER 2.5: SUBSET / CONSTRAINT ANALYSIS ==========
    // Compare pairs of numbered cells. If cell A's closed neighbors
    // are a subset of cell B's, deduce info about B-exclusive cells.

    private List<Constraint> BuildConstraints()
    {
        var constraints = new List<Constraint>();
        int w = board.GetWidth(), h = board.GetHeight();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = board.GetCell(x, y);
                if (!cell.isRevealed || cell.number <= 0) continue;

                int flags = 0;
                var closed = new HashSet<Vector2Int>();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        Cell n = board.GetCell(nx, ny);
                        if (n.isFlagged) flags++;
                        else if (!n.isRevealed) closed.Add(new Vector2Int(nx, ny));
                    }
                }

                int remaining = cell.number - flags;
                if (remaining >= 0 && closed.Count > 0)
                {
                    constraints.Add(new Constraint
                    {
                        position = new Vector2Int(x, y),
                        remaining = remaining,
                        closed = closed
                    });
                }
            }
        }

        return constraints;
    }

    private void FindSubsetMoves(HashSet<Vector2Int> safe, HashSet<Vector2Int> mines)
    {
        var constraints = BuildConstraints();

        for (int i = 0; i < constraints.Count; i++)
        {
            for (int j = i + 1; j < constraints.Count; j++)
            {
                var a = constraints[i];
                var b = constraints[j];

                // Only check nearby pairs (Chebyshev distance <= 3)
                int dist = Mathf.Max(
                    Mathf.Abs(a.position.x - b.position.x),
                    Mathf.Abs(a.position.y - b.position.y));
                if (dist > 3) continue;

                // Check A ⊂ B
                if (a.closed.Count <= b.closed.Count && a.closed.IsSubsetOf(b.closed))
                {
                    var exclusive = new HashSet<Vector2Int>(b.closed);
                    exclusive.ExceptWith(a.closed);
                    if (exclusive.Count > 0)
                    {
                        int diff = b.remaining - a.remaining;
                        if (diff == 0)
                        {
                            foreach (var pos in exclusive) safe.Add(pos);
                        }
                        else if (diff > 0 && diff == exclusive.Count)
                        {
                            foreach (var pos in exclusive) mines.Add(pos);
                        }
                    }
                }

                // Check B ⊂ A
                if (b.closed.Count <= a.closed.Count && b.closed.IsSubsetOf(a.closed))
                {
                    var exclusive = new HashSet<Vector2Int>(a.closed);
                    exclusive.ExceptWith(b.closed);
                    if (exclusive.Count > 0)
                    {
                        int diff = a.remaining - b.remaining;
                        if (diff == 0)
                        {
                            foreach (var pos in exclusive) safe.Add(pos);
                        }
                        else if (diff > 0 && diff == exclusive.Count)
                        {
                            foreach (var pos in exclusive) mines.Add(pos);
                        }
                    }
                }
            }
        }
    }

    // ========== LAYER 3: IMPROVED PROBABILITY GUESS ==========
    // Separates border cells (adjacent to revealed) from interior cells.
    // Uses global mine density for interior vs local constraint danger for border.
    // Prefers corners/edges as tiebreaker.

    private Vector2Int? FindBestGuess()
    {
        int w = board.GetWidth(), h = board.GetHeight();
        int remainingMines = board.GetMineCount() - board.GetFlagCount();

        var borderCells = new List<Vector2Int>();
        var interiorCells = new List<Vector2Int>();
        var dangerScores = new Dictionary<Vector2Int, float>();
        int totalClosed = 0;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = board.GetCell(x, y);
                if (cell.isRevealed || cell.isFlagged) continue;
                totalClosed++;

                bool isBorder = false;
                float maxDanger = 0f;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        Cell neighbor = board.GetCell(nx, ny);
                        if (!neighbor.isRevealed || neighbor.number <= 0) continue;

                        isBorder = true;

                        // Danger from this constraint
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

                Vector2Int pos = new Vector2Int(x, y);
                if (isBorder)
                {
                    borderCells.Add(pos);
                    dangerScores[pos] = maxDanger;
                }
                else
                {
                    interiorCells.Add(pos);
                }
            }
        }

        // Find best border cell (lowest danger, then fewest neighbors)
        float bestBorderScore = float.MaxValue;
        Vector2Int? bestBorderCell = null;
        foreach (var pos in borderCells)
        {
            float score = dangerScores[pos];
            if (score < bestBorderScore ||
                (score == bestBorderScore && bestBorderCell.HasValue &&
                 CountGridNeighbors(pos, w, h) < CountGridNeighbors(bestBorderCell.Value, w, h)))
            {
                bestBorderScore = score;
                bestBorderCell = pos;
            }
        }

        // Global density for interior cells
        float interiorProbability = totalClosed > 0
            ? (float)remainingMines / totalClosed
            : 1f;

        // If interior is safer than best border, pick interior
        if (interiorCells.Count > 0 &&
            (bestBorderCell == null || interiorProbability < bestBorderScore))
        {
            return PickBestInterior(interiorCells, w, h);
        }

        if (bestBorderCell.HasValue) return bestBorderCell;

        // Fallback: pick best interior
        if (interiorCells.Count > 0)
            return PickBestInterior(interiorCells, w, h);

        return null;
    }

    // Prefer corners then edges then middle among interior cells
    private Vector2Int PickBestInterior(List<Vector2Int> cells, int w, int h)
    {
        Vector2Int best = cells[0];
        int bestScore = InteriorScore(best, w, h);

        for (int i = 1; i < cells.Count; i++)
        {
            int score = InteriorScore(cells[i], w, h);
            if (score < bestScore)
            {
                bestScore = score;
                best = cells[i];
            }
        }

        return best;
    }

    // Lower = better. Corners (0), edges (1), other (2)
    private int InteriorScore(Vector2Int pos, int w, int h)
    {
        bool xEdge = (pos.x == 0 || pos.x == w - 1);
        bool yEdge = (pos.y == 0 || pos.y == h - 1);
        if (xEdge && yEdge) return 0;
        if (xEdge || yEdge) return 1;
        return 2;
    }

    private int CountGridNeighbors(Vector2Int pos, int w, int h)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = pos.x + dx, ny = pos.y + dy;
                if (nx >= 0 && nx < w && ny >= 0 && ny < h) count++;
            }
        }
        return count;
    }
}
