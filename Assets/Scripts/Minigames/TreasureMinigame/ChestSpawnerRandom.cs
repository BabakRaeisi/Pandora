// ChestSpawnerRandom.cs
using System.Collections.Generic;
using UnityEngine;

public class ChestSpawnerRandom : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform playArea;
    [SerializeField] private SWMChest chestPrefab;

    [Header("Placement")]
    [SerializeField] private float padding = 30f;          // keep away from edges
    [SerializeField] private float minGap = 20f;           // extra gap between chests
    [SerializeField] private int maxPlacementAttempts = 250;

    private readonly List<SWMChest> spawned = new();

    public IReadOnlyList<SWMChest> Spawn(int count, SWMGameManager manager)
    {
        Clear();

        RectTransform area = playArea ? playArea : (RectTransform)transform;
        Rect r = area.rect;

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
            return spawned;
        }

        float minDist = Mathf.Max(size.x, size.y) + minGap;
        float minDistSqr = minDist * minDist;

        List<Vector2> points = new();

        for (int i = 0; i < count; i++)
        {
            bool placed = false;

            for (int t = 0; t < maxPlacementAttempts; t++)
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

                SWMChest chest = Instantiate(chestPrefab, area);
                RectTransform rt = chest.GetComponent<RectTransform>();
                rt.anchoredPosition = p;
                rt.localScale = Vector3.one;

                chest.name = $"Chest_{i}";
                chest.Init(i, manager);

                spawned.Add(chest);
                placed = true;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning("Could not place all chests. Reduce count/minGap/padding or enlarge PlayArea.");
                break;
            }
        }

        return spawned;
    }

    public void Clear()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i]) Destroy(spawned[i].gameObject);

        spawned.Clear();
    }

    public IReadOnlyList<SWMChest> GetSpawned() => spawned;
}
