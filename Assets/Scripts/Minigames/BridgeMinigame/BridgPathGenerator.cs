// BridgPathGenerator.cs  (FULL)  — 2-column, contiguous, FORCE zigzag when requested
using System.Collections.Generic;
using UnityEngine;

public static class BridgePathGenerator
{
    /// <summary>
    /// IDs must follow: id = row*2 + col   (col 0/1).
    /// Builds a contiguous path from startRow to endRow inside activeRows span.
    /// ZigZag in 2 columns requires extra steps (horizontal switches). One switch costs +1 length.
    /// </summary>
    public static List<int> Generate2ColPath(
        int activeRows,
        int targetLength,
        bool startFromBottom,
        bool forceAtLeastOneSwitch,
        int maxAttempts = 2000)
    {
        const int cols = 2;

        activeRows = Mathf.Clamp(activeRows, 2, 999);

        int startRow = startFromBottom ? activeRows - 1 : 0;
        int endRow = startFromBottom ? 0 : activeRows - 1;
        int dir = startFromBottom ? -1 : 1;

        int minLen = activeRows;        // need at least 1 per row to reach other chasm edge
        int maxLen = activeRows * cols; // at most both tiles per row
        targetLength = Mathf.Clamp(targetLength, minLen, maxLen);

        int extras = targetLength - activeRows;               // how many horizontal inserts possible
        int maxSwitches = Mathf.Min(extras, activeRows - 1);  // at most one switch per row transition

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int col = Random.Range(0, 2);

            // Pick which row-transitions will switch columns.
            // Transition t means: between row R and R+dir (t from 0..activeRows-2)
            var switchTransitions = new HashSet<int>();

            if (maxSwitches > 0)
            {
                int switchesToUse = forceAtLeastOneSwitch ? Random.Range(1, maxSwitches + 1) : Random.Range(0, maxSwitches + 1);

                // Random unique transitions
                var candidates = new List<int>(activeRows - 1);
                for (int t = 0; t < activeRows - 1; t++) candidates.Add(t);
                Shuffle(candidates);

                for (int i = 0; i < switchesToUse; i++)
                    switchTransitions.Add(candidates[i]);
            }
            else
            {
                if (forceAtLeastOneSwitch)
                    continue; // impossible to zigzag with no extras
            }

            var path = new List<int>(targetLength);
            int r = startRow;

            path.Add(r * cols + col);

            // backbone down/up through all rows, with optional horizontal switch before each vertical step
            for (int t = 0; t < activeRows - 1; t++)
            {
                if (switchTransitions.Contains(t))
                {
                    int other = 1 - col;
                    path.Add(r * cols + other);
                    col = other;
                    if (path.Count > targetLength) break;
                }

                r += dir;
                path.Add(r * cols + col);
                if (path.Count > targetLength) break;
            }

            if (path.Count != targetLength) continue;
            if (!IsContiguous(path)) continue;
            if ((path[0] / cols) != startRow) continue;
            if ((path[path.Count - 1] / cols) != endRow) continue;

            // if we forced zigzag, ensure both columns appear
            if (forceAtLeastOneSwitch)
            {
                bool has0 = false, has1 = false;
                for (int i = 0; i < path.Count; i++)
                {
                    int c = path[i] % cols;
                    if (c == 0) has0 = true;
                    if (c == 1) has1 = true;
                }
                if (!(has0 && has1)) continue;
            }

            return path;
        }

        // deterministic fallback: straight
        return FallbackStraight(activeRows, targetLength, startFromBottom);
    }

    private static bool IsContiguous(List<int> path)
    {
        const int cols = 2;
        for (int i = 1; i < path.Count; i++)
        {
            int a = path[i - 1];
            int b = path[i];

            int ar = a / cols; int ac = a % cols;
            int br = b / cols; int bc = b % cols;

            int man = Mathf.Abs(ar - br) + Mathf.Abs(ac - bc);
            if (man != 1) return false;
        }
        return true;
    }

    private static List<int> FallbackStraight(int activeRows, int targetLength, bool startFromBottom)
    {
        const int cols = 2;

        int startRow = startFromBottom ? activeRows - 1 : 0;
        int endRow = startFromBottom ? 0 : activeRows - 1;
        int dir = startFromBottom ? -1 : 1;

        int col = 0;

        var path = new List<int>();
        int r = startRow;

        path.Add(r * cols + col);

        while (r != endRow)
        {
            r += dir;
            path.Add(r * cols + col);
        }

        // fill extras by adding horizontal at the first possible rows
        while (path.Count < targetLength)
        {
            for (int i = 0; i < path.Count && path.Count < targetLength; i++)
            {
                int id = path[i];
                int rr = id / cols;
                int cc = id % cols;
                int other = rr * cols + (1 - cc);

                // insert if it keeps contiguity locally (it will: (rr,cc)->(rr,other) is adjacent)
                path.Insert(i + 1, other);
                i++;
            }
            break;
        }

        if (path.Count > targetLength) path.RemoveRange(targetLength, path.Count - targetLength);
        return path;
    }

    private static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}