using System.Collections.Generic;
using UnityEngine;

public class ChestSpawnerRandom : MonoBehaviour
{
    [Header("Spawn Area (UI RectTransform)")]
    [SerializeField] private RectTransform playArea;

    [Header("Chest Prefab (must be UI: RectTransform + SWMChest)")]
    [SerializeField] private SWMChest chestPrefab;

    [Header("Placement")]
    [SerializeField, Min(0f)] private float padding = 20f;
    [SerializeField, Min(0f)] private float minGap = 20f;

    [Tooltip("Small variation inside each safe grid cell.")]
    [SerializeField, Min(0f)] private float positionJitter = 18f;

    [Tooltip("Do not make chests smaller than this unless the play area cannot fit them.")]
    [SerializeField, Range(0.1f, 1f)] private float preferredMinimumScale = 0.55f;

    // Spawn once for the level. The manager should call Reposition once after
    // deciding how many chests that level uses.
    public List<SWMChest> SpawnPool(int count, SWMGameManager gm)
    {
        var list = new List<SWMChest>(count);

        for (int i = 0; i < count; i++)
        {
            SWMChest chest = Instantiate(chestPrefab, playArea);
            chest.Init(i, gm);
            list.Add(chest);
        }

        return list;
    }

    /// <summary>
    /// Places active chests in randomized, non-overlapping cells.
    /// If the area cannot fit all chests at scale 1, all active chests are
    /// scaled down equally rather than being allowed to overlap.
    /// </summary>
    public void Reposition(List<SWMChest> pool, int activeCount)
    {
        if (pool == null || pool.Count == 0)
            return;

        RectTransform area = playArea != null
            ? playArea
            : transform as RectTransform;

        if (area == null || chestPrefab == null)
        {
            Debug.LogError("[ChestSpawnerRandom] Missing Play Area or Chest Prefab.");
            return;
        }

        activeCount = Mathf.Clamp(activeCount, 0, pool.Count);

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null)
                pool[i].gameObject.SetActive(i < activeCount);
        }

        if (activeCount == 0)
            return;

        RectTransform prefabRect = chestPrefab.GetComponent<RectTransform>();

        Vector2 chestSize = prefabRect.rect.size;

        if (chestSize.x <= 0f || chestSize.y <= 0f)
            chestSize = prefabRect.sizeDelta;

        if (chestSize.x <= 0f || chestSize.y <= 0f)
        {
            Debug.LogError("[ChestSpawnerRandom] Chest prefab has an invalid RectTransform size.");
            return;
        }

        Rect areaRect = area.rect;

        float availableWidth = areaRect.width - padding * 2f;
        float availableHeight = areaRect.height - padding * 2f;

        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            Debug.LogError("[ChestSpawnerRandom] Play area is too small after padding.");
            return;
        }

        int bestColumns = 1;
        int bestRows = activeCount;
        float bestScale = 0f;

        // Find the grid shape that allows the largest possible chest scale.
        for (int columns = 1; columns <= activeCount; columns++)
        {
            int rows = Mathf.CeilToInt(activeCount / (float)columns);

            float requiredWidthAtScaleOne =
                columns * chestSize.x + (columns - 1) * minGap;

            float requiredHeightAtScaleOne =
                rows * chestSize.y + (rows - 1) * minGap;

            float scaleForWidth = availableWidth / requiredWidthAtScaleOne;
            float scaleForHeight = availableHeight / requiredHeightAtScaleOne;
            float candidateScale = Mathf.Min(scaleForWidth, scaleForHeight, 1f);

            if (candidateScale > bestScale)
            {
                bestScale = candidateScale;
                bestColumns = columns;
                bestRows = rows;
            }
        }

        if (bestScale <= 0f)
        {
            Debug.LogError("[ChestSpawnerRandom] Could not calculate a valid chest layout.");
            return;
        }

        if (bestScale < preferredMinimumScale)
        {
            Debug.LogWarning(
                $"[ChestSpawnerRandom] {activeCount} chests require scale {bestScale:F2} " +
                $"to avoid overlap in the current Play Area. Enlarge the Play Area, " +
                $"reduce Padding/Min Gap, or use a smaller chest sprite."
            );
        }

        float scaledChestWidth = chestSize.x * bestScale;
        float scaledChestHeight = chestSize.y * bestScale;

        float layoutWidth =
            bestColumns * scaledChestWidth + (bestColumns - 1) * minGap;

        float layoutHeight =
            bestRows * scaledChestHeight + (bestRows - 1) * minGap;

        float layoutLeft = areaRect.center.x - layoutWidth * 0.5f;
        float layoutTop = areaRect.center.y + layoutHeight * 0.5f;

        List<int> slotIndices = new List<int>(bestColumns * bestRows);

        for (int slot = 0; slot < bestColumns * bestRows; slot++)
            slotIndices.Add(slot);

        Shuffle(slotIndices);

        float cellWidth = scaledChestWidth + minGap;
        float cellHeight = scaledChestHeight + minGap;

        // Jitter is limited to half the gap, so neighboring chest rectangles
        // can never overlap.
        float safeJitter = Mathf.Min(positionJitter, minGap * 0.45f);

        for (int chestIndex = 0; chestIndex < activeCount; chestIndex++)
        {
            SWMChest chest = pool[chestIndex];

            if (chest == null)
                continue;

            int slot = slotIndices[chestIndex];
            int row = slot / bestColumns;
            int column = slot % bestColumns;

            float x = layoutLeft + scaledChestWidth * 0.5f + column * cellWidth;
            float y = layoutTop - scaledChestHeight * 0.5f - row * cellHeight;

            x += Random.Range(-safeJitter, safeJitter);
            y += Random.Range(-safeJitter, safeJitter);

            RectTransform chestRect = chest.GetComponent<RectTransform>();

            chestRect.anchorMin = new Vector2(0.5f, 0.5f);
            chestRect.anchorMax = new Vector2(0.5f, 0.5f);
            chestRect.pivot = new Vector2(0.5f, 0.5f);
            chestRect.anchoredPosition = new Vector2(x, y);
            chestRect.localScale = Vector3.one * bestScale;
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            T temporary = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temporary;
        }
    }
}
