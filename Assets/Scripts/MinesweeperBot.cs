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

            // Layer 3: Tank Solver
            var tankSafe = new HashSet<Vector2Int>();
            var tankMines = new HashSet<Vector2Int>();
            Vector2Int? tankGuess;
            RunTankSolver(tankSafe, tankMines, out tankGuess);

            if (tankSafe.Count > 0 || tankMines.Count > 0)
            {
                if (tankSafe.Count > 0)
                    yield return ExecuteReveals(new List<Vector2Int>(tankSafe), HighlightSafe);
                if (tankMines.Count > 0)
                    yield return ExecuteFlags(new List<Vector2Int>(tankMines), HighlightMine);
                continue;
            }

            if (tankGuess.HasValue)
            {
                yield return DoHighlight(tankGuess.Value.x, tankGuess.Value.y, HighlightGuess);
                board.BotRevealCell(tankGuess.Value.x, tankGuess.Value.y);
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

    // ========== LAYER 3: TANK SOLVER ==========
    // Enumerates all valid mine configurations on border cells via
    // backtracking with constraint pruning, then computes exact
    // per-cell mine probabilities.

    private const int MaxGroupSize = 20;
    private const int MaxConfigurations = 100000;

    private struct TankConstraint
    {
        public int remaining;       // number - flags for this numbered cell
        public List<int> cellIndices; // indices into group's border cell list
    }

    // Orchestrates the full Tank Solver pass.
    // Returns definite safe/mine cells AND the best guess if no certain moves exist.
    private bool RunTankSolver(HashSet<Vector2Int> safeCells, HashSet<Vector2Int> mineCells,
        out Vector2Int? bestGuess)
    {
        bestGuess = null;
        int w = board.GetWidth(), h = board.GetHeight();
        int remainingMines = board.GetMineCount() - board.GetFlagCount();

        // 1. Classify all closed unflagged cells as border or interior
        var interiorCells = new List<Vector2Int>();
        var borderCellIndex = new Dictionary<Vector2Int, int>();
        var borderCellList = new List<Vector2Int>();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = board.GetCell(x, y);
                if (cell.isRevealed || cell.isFlagged) continue;

                bool isBorder = false;
                for (int dx = -1; dx <= 1 && !isBorder; dx++)
                    for (int dy = -1; dy <= 1 && !isBorder; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        Cell n = board.GetCell(nx, ny);
                        if (n.isRevealed && n.number > 0) isBorder = true;
                    }

                Vector2Int pos = new Vector2Int(x, y);
                if (isBorder)
                {
                    borderCellIndex[pos] = borderCellList.Count;
                    borderCellList.Add(pos);
                }
                else
                {
                    interiorCells.Add(pos);
                }
            }
        }

        if (borderCellList.Count == 0)
        {
            // No border cells - pick best interior
            if (interiorCells.Count > 0)
                bestGuess = PickBestInterior(interiorCells, w, h);
            return true;
        }

        // 2. Build constraints from numbered cells & union-find for components
        int n = borderCellList.Count;
        int[] ufParent = new int[n];
        int[] ufRank = new int[n];
        for (int i = 0; i < n; i++) { ufParent[i] = i; ufRank[i] = 0; }

        var allConstraints = new List<TankConstraint>();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = board.GetCell(x, y);
                if (!cell.isRevealed || cell.number <= 0) continue;

                int flags = 0;
                var indices = new List<int>();

                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        Cell nc = board.GetCell(nx, ny);
                        if (nc.isFlagged) flags++;
                        else if (!nc.isRevealed)
                        {
                            Vector2Int pos = new Vector2Int(nx, ny);
                            if (borderCellIndex.ContainsKey(pos))
                                indices.Add(borderCellIndex[pos]);
                        }
                    }

                int rem = cell.number - flags;
                if (rem >= 0 && indices.Count > 0)
                {
                    allConstraints.Add(new TankConstraint { remaining = rem, cellIndices = indices });
                    for (int i = 1; i < indices.Count; i++)
                        UFUnion(ufParent, ufRank, indices[0], indices[i]);
                }
            }
        }

        // 3. Group border cells by connected component
        var compCells = new Dictionary<int, List<int>>();
        for (int i = 0; i < n; i++)
        {
            int root = UFFind(ufParent, i);
            if (!compCells.ContainsKey(root))
                compCells[root] = new List<int>();
            compCells[root].Add(i);
        }

        // 4. Solve each group
        // Collect per-border-cell probabilities
        var probabilities = new Dictionary<Vector2Int, float>();
        float expectedBorderMines = 0f;
        foreach (var kvp in compCells)
        {
            var globalIndices = kvp.Value;
            int groupSize = globalIndices.Count;

            // Build local group: remap global indices to 0..groupSize-1
            var oldToNew = new Dictionary<int, int>();
            var groupCells = new List<Vector2Int>();
            for (int i = 0; i < groupSize; i++)
            {
                oldToNew[globalIndices[i]] = i;
                groupCells.Add(borderCellList[globalIndices[i]]);
            }

            // Collect constraints that touch this group
            var groupConstraints = new List<TankConstraint>();
            foreach (var tc in allConstraints)
            {
                var mapped = new List<int>();
                foreach (int gi in tc.cellIndices)
                    if (oldToNew.ContainsKey(gi))
                        mapped.Add(oldToNew[gi]);
                if (mapped.Count > 0)
                    groupConstraints.Add(new TankConstraint { remaining = tc.remaining, cellIndices = mapped });
            }

            // If group too large, use simple fallback for this group
            if (groupSize > MaxGroupSize)
            {
                // Fallback group - not solved exactly
                for (int i = 0; i < groupSize; i++)
                {
                    float score = SimpleDangerScore(groupCells[i]);
                    probabilities[groupCells[i]] = score;
                    expectedBorderMines += score;
                }
                continue;
            }

            // Backtracking solve
            int totalConfigs = 0;
            int[] mineCounts = new int[groupSize];
            bool overflow = false;

            // Precompute: which constraints involve each cell
            var cellToConstraints = new List<int>[groupSize];
            for (int i = 0; i < groupSize; i++)
                cellToConstraints[i] = new List<int>();
            for (int ci = 0; ci < groupConstraints.Count; ci++)
                foreach (int bi in groupConstraints[ci].cellIndices)
                    cellToConstraints[bi].Add(ci);

            // Sort cells: most constrained first for better pruning
            int[] order = new int[groupSize];
            for (int i = 0; i < groupSize; i++) order[i] = i;
            System.Array.Sort(order, (a, b) =>
                cellToConstraints[b].Count.CompareTo(cellToConstraints[a].Count));
            // Inverse mapping for results
            int[] orderInv = new int[groupSize];
            for (int i = 0; i < groupSize; i++) orderInv[order[i]] = i;

            // Remap constraints and cellToConstraints to sorted order
            var sortedConstraints = new List<TankConstraint>();
            foreach (var tc in groupConstraints)
            {
                var mapped = new List<int>();
                foreach (int ci in tc.cellIndices) mapped.Add(orderInv[ci]);
                sortedConstraints.Add(new TankConstraint { remaining = tc.remaining, cellIndices = mapped });
            }
            var sortedCellToConstraints = new List<int>[groupSize];
            for (int i = 0; i < groupSize; i++)
                sortedCellToConstraints[i] = new List<int>();
            for (int ci = 0; ci < sortedConstraints.Count; ci++)
                foreach (int bi in sortedConstraints[ci].cellIndices)
                    sortedCellToConstraints[bi].Add(ci);

            // Incremental constraint state
            int[] cMines = new int[sortedConstraints.Count];
            int[] cUnassigned = new int[sortedConstraints.Count];
            for (int ci = 0; ci < sortedConstraints.Count; ci++)
                cUnassigned[ci] = sortedConstraints[ci].cellIndices.Count;

            int[] sortedMineCounts = new int[groupSize];
            int[] assignment = new int[groupSize];

            TankBacktrack(sortedConstraints, sortedCellToConstraints,
                cMines, cUnassigned, sortedMineCounts, assignment,
                0, groupSize, 0, remainingMines,
                ref totalConfigs, ref overflow);

            if (overflow || totalConfigs == 0)
            {
                // Fallback for this group
                // Fallback group - not solved exactly
                for (int i = 0; i < groupSize; i++)
                {
                    float score = SimpleDangerScore(groupCells[i]);
                    probabilities[groupCells[i]] = score;
                    expectedBorderMines += score;
                }
                continue;
            }

            // Map results back to original cell order
            for (int sortedIdx = 0; sortedIdx < groupSize; sortedIdx++)
            {
                int origIdx = order[sortedIdx];
                float prob = (float)sortedMineCounts[sortedIdx] / totalConfigs;
                probabilities[groupCells[origIdx]] = prob;
                expectedBorderMines += prob;

                if (prob == 0f)
                    safeCells.Add(groupCells[origIdx]);
                else if (prob == 1f)
                    mineCells.Add(groupCells[origIdx]);
            }
        }

        // If we found certain moves, return without computing a guess
        if (safeCells.Count > 0 || mineCells.Count > 0)
            return true;

        // 5. Pick best guess: lowest probability border cell vs interior
        float bestBorderProb = float.MaxValue;
        Vector2Int? bestBorderCell = null;
        foreach (var kvp2 in probabilities)
        {
            if (kvp2.Value < bestBorderProb ||
                (kvp2.Value == bestBorderProb && bestBorderCell.HasValue &&
                 CountGridNeighbors(kvp2.Key, w, h) < CountGridNeighbors(bestBorderCell.Value, w, h)))
            {
                bestBorderProb = kvp2.Value;
                bestBorderCell = kvp2.Key;
            }
        }

        // Interior probability: account for expected border mines
        float interiorProb = 1f;
        if (interiorCells.Count > 0)
        {
            float interiorMines = remainingMines - expectedBorderMines;
            interiorProb = Mathf.Max(0f, interiorMines) / interiorCells.Count;
        }

        if (interiorCells.Count > 0 &&
            (bestBorderCell == null || interiorProb < bestBorderProb))
        {
            bestGuess = PickBestInterior(interiorCells, w, h);
        }
        else
        {
            bestGuess = bestBorderCell;
        }

        return true;
    }

    // ---- Backtracking engine ----

    private void TankBacktrack(
        List<TankConstraint> constraints, List<int>[] cellToConstraints,
        int[] cMines, int[] cUnassigned, int[] mineCounts, int[] assignment,
        int idx, int total, int minesSoFar, int maxMines,
        ref int totalConfigs, ref bool overflow)
    {
        if (overflow) return;

        if (idx == total)
        {
            // Valid complete assignment - record mine positions
            totalConfigs++;
            if (totalConfigs > MaxConfigurations) { overflow = true; return; }
            for (int i = 0; i < total; i++)
                mineCounts[i] += assignment[i];
            return;
        }

        for (int val = 0; val <= 1; val++)
        {
            if (overflow) return;

            int newMines = minesSoFar + val;
            if (newMines > maxMines) continue;

            assignment[idx] = val;

            // Apply incremental constraint updates
            bool valid = true;
            foreach (int ci in cellToConstraints[idx])
            {
                cUnassigned[ci]--;
                if (val == 1) cMines[ci]++;

                if (cMines[ci] > constraints[ci].remaining ||
                    cMines[ci] + cUnassigned[ci] < constraints[ci].remaining)
                    valid = false;
            }

            if (valid)
                TankBacktrack(constraints, cellToConstraints,
                    cMines, cUnassigned, mineCounts, assignment,
                    idx + 1, total, newMines, maxMines,
                    ref totalConfigs, ref overflow);

            // Undo incremental updates
            foreach (int ci in cellToConstraints[idx])
            {
                cUnassigned[ci]++;
                if (val == 1) cMines[ci]--;
            }
        }
    }

    // ---- Simple fallback for groups too large for Tank Solver ----

    private float SimpleDangerScore(Vector2Int pos)
    {
        int w = board.GetWidth(), h = board.GetHeight();
        float maxDanger = 0f;

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = pos.x + dx, ny = pos.y + dy;
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                Cell neighbor = board.GetCell(nx, ny);
                if (!neighbor.isRevealed || neighbor.number <= 0) continue;

                int nFlags = 0, nClosed = 0;
                for (int ddx = -1; ddx <= 1; ddx++)
                    for (int ddy = -1; ddy <= 1; ddy++)
                    {
                        if (ddx == 0 && ddy == 0) continue;
                        int nnx = nx + ddx, nny = ny + ddy;
                        if (nnx < 0 || nnx >= w || nny < 0 || nny >= h) continue;
                        Cell nn = board.GetCell(nnx, nny);
                        if (nn.isFlagged) nFlags++;
                        else if (!nn.isRevealed) nClosed++;
                    }

                if (nClosed > 0)
                {
                    float danger = (float)(neighbor.number - nFlags) / nClosed;
                    if (danger > maxDanger) maxDanger = danger;
                }
            }

        return maxDanger;
    }

    // ---- Union-Find ----

    private int UFFind(int[] parent, int x)
    {
        while (parent[x] != x)
        {
            parent[x] = parent[parent[x]];
            x = parent[x];
        }
        return x;
    }

    private void UFUnion(int[] parent, int[] rank, int a, int b)
    {
        int ra = UFFind(parent, a), rb = UFFind(parent, b);
        if (ra == rb) return;
        if (rank[ra] < rank[rb]) { int t = ra; ra = rb; rb = t; }
        parent[rb] = ra;
        if (rank[ra] == rank[rb]) rank[ra]++;
    }

    // ---- Helpers ----

    private Vector2Int PickBestInterior(List<Vector2Int> cells, int w, int h)
    {
        Vector2Int best = cells[0];
        int bestScore = InteriorScore(best, w, h);
        for (int i = 1; i < cells.Count; i++)
        {
            int score = InteriorScore(cells[i], w, h);
            if (score < bestScore) { bestScore = score; best = cells[i]; }
        }
        return best;
    }

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
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = pos.x + dx, ny = pos.y + dy;
                if (nx >= 0 && nx < w && ny >= 0 && ny < h) count++;
            }
        return count;
    }
}
