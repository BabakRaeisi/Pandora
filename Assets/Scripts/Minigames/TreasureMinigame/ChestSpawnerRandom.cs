using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ChestSpawnerRandom : MonoBehaviour
{
    [Header("Spawn Area (UI RectTransform)")]
    [SerializeField] private RectTransform playArea;

    [Header("Chest Prefab (must be UI: RectTransform + SWMChest)")]
    [SerializeField] private SWMChest chestPrefab;

    [Header("Placement")]
    [SerializeField] private float padding = 20f;
    [SerializeField] private float minGap = 20f;
    [SerializeField] private float minDistance = 120f;     // tweak based on chest size + desired spacing
    [SerializeField] private int maxAttemptsPerChest = 200;
    [SerializeField] private int layoutAttempts = 40;      // tries to find a full non-overlapping layout

    // Spawn ONCE for the day, initialize Index + manager, and place inside bounds.
    public List<SWMChest> SpawnPool(int count, SWMGameManager gm)
    {
        var list = new List<SWMChest>(count);

        for (int i = 0; i < count; i++)
        {
            var chest = Instantiate(chestPrefab, playArea);

            // Restore old wiring (Index + GameManager)
            chest.Init(i, gm);

            list.Add(chest);
        }

        // initial layout
        Reposition(list, count);
        return list;
    }

    // Reposition SAME chests each trial.
    // - Activates first activeCount, disables rest
    // - Tries multiple full layouts so it doesn't "half succeed"
    // - Keeps chests fully inside playArea bounds (uses chest rect size)
    public void Reposition(List<SWMChest> pool, int activeCount)
    {
        if (pool == null) return;

        RectTransform area = playArea ? playArea : (RectTransform)transform;
        Rect r = area.rect;

        activeCount = Mathf.Clamp(activeCount, 0, pool.Count);

        // Enable first N, disable rest
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i]) continue;
            pool[i].gameObject.SetActive(i < activeCount);
        }

        if (activeCount <= 0) return;

        // Chest size (use prefab sizeDelta like your old working function)
        RectTransform prefabRT = chestPrefab.GetComponent<RectTransform>();
        Vector2 size = prefabRT.sizeDelta;

        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        float minX = r.xMin + padding + halfW;
        float maxX = r.xMax - padding - halfW;
        float minY = r.yMin + padding + halfH;
        float maxY = r.yMax - padding - halfH;

        if (minX >= maxX || minY >= maxY)
        {
            Debug.LogError("PlayArea too small for chest size + padding.");
            // Keep them inside anyway (all centered) to avoid NaNs/out of bounds
            for (int i = 0; i < activeCount; i++)
            {
                if (!pool[i]) continue;
                var rt = pool[i].GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            return;
        }

        float minDist = Mathf.Max(size.x, size.y) + minGap;
        float minDistSqr = minDist * minDist;

        // We try multiple full-layout passes (avoids partial success)
        const int layoutAttempts = 30;
        bool success = false;

        for (int pass = 0; pass < layoutAttempts && !success; pass++)
        {
            success = true;
            List<Vector2> points = new List<Vector2>(activeCount);

            for (int i = 0; i < activeCount; i++)
            {
                bool placed = false;

                for (int t = 0; t < maxAttemptsPerChest; t++)
                {
                    Vector2 p = new Vector2(
                        Random.Range(minX, maxX),
                        Random.Range(minY, maxY)
                    );

                    bool overlaps = false;
                    for (int k = 0; k < points.Count; k++)
                    {
                        if ((p - points[k]).sqrMagnitude < minDistSqr)
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (overlaps) continue;

                    points.Add(p);
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    success = false;
                    break; // fail this pass, retry whole layout
                }
            }

            if (success)
            {
                // Apply positions
                for (int i = 0; i < activeCount; i++)
                {
                    if (!pool[i]) continue;
                    var rt = pool[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = points[i];
                    rt.localScale = Vector3.one;
                }
            }
        }

        if (!success)
        {
            Debug.LogWarning("Reposition: Could not place all chests without overlap. Reduce count/minGap/padding or enlarge PlayArea.");

            // Fallback: still keep inside bounds (may overlap, but never out of bounds)
            for (int i = 0; i < activeCount; i++)
            {
                if (!pool[i]) continue;

                Vector2 p = new Vector2(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY)
                );

                var rt = pool[i].GetComponent<RectTransform>();
                rt.anchoredPosition = p;
                rt.localScale = Vector3.one;
            }
        }
    }


    // Picks a position for the chest at index "placedUpTo" that doesn't overlap already-placed [0..placedUpTo-1]
    private bool TryPickPosition(List<SWMChest> pool, int placedUpTo,
                                 float minX, float maxX, float minY, float maxY,
                                 out Vector2 result)
    {
        result = Vector2.zero;

        int attempts = Mathf.Max(1, maxAttemptsPerChest);

        for (int a = 0; a < attempts; a++)
        {
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            Vector2 candidate = new Vector2(x, y);

            bool ok = true;

            for (int j = 0; j < placedUpTo; j++)
            {
                var other = pool[j];
                if (!other) continue;

                var rt = other.GetComponent<RectTransform>();
                if (!rt) continue;

                if (Vector2.Distance(candidate, rt.anchoredPosition) < minDistance)
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                result = candidate;
                return true;
            }
        }

        return false;
    }
}
