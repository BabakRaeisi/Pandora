using UnityEngine;

 

public static class UILineSegment
{
    public static void Place(
        RectTransform line,
        Vector2 start,
        Vector2 end,
        float thickness)
    {
        Vector2 dir = end - start;
        float length = dir.magnitude;

        line.anchoredPosition = (start + end) * 0.5f;
        line.sizeDelta = new Vector2(length, thickness);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        line.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
