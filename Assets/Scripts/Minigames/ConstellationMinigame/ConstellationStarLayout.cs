using UnityEngine;

public class ConstellationStarLayout : MonoBehaviour
{
    public RectTransform playArea;
    public RectTransform[] stars; // size 9

    // Normalized positions (0..1) inside playArea (irregular 3x3)
    public Vector2[] normalizedPositions = new Vector2[9]
    {
        new Vector2(0.18f, 0.78f),
        new Vector2(0.50f, 0.85f),
        new Vector2(0.82f, 0.76f),

        new Vector2(0.22f, 0.52f),
        new Vector2(0.52f, 0.55f),
        new Vector2(0.78f, 0.48f),

        new Vector2(0.15f, 0.22f),
        new Vector2(0.48f, 0.18f),
        new Vector2(0.85f, 0.25f),
    };

    public float padding = 30f; // keep away from edges

    void Start()
    {
        ApplyLayout();
    }

    [ContextMenu("Apply Layout")]
    public void ApplyLayout()
    {
        if (!playArea || stars == null || stars.Length != 9) return;

        Rect r = playArea.rect;

        float minX = r.xMin + padding;
        float maxX = r.xMax - padding;
        float minY = r.yMin + padding;
        float maxY = r.yMax - padding;

        for (int i = 0; i < 9; i++)
        {
            if (!stars[i]) continue;

            Vector2 n = normalizedPositions[i];
            float x = Mathf.Lerp(minX, maxX, n.x);
            float y = Mathf.Lerp(minY, maxY, n.y);

            stars[i].anchoredPosition = new Vector2(x, y);
        }
    }
}
