using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class AvatarCarousel : MonoBehaviour 
{
    [Header("Refs")]
    public ScrollRect scrollRect;
    public RectTransform viewport;
    public RectTransform content;

    [Header("Settings")]
    public float snapDuration = 0.25f;
    
    public bool wrap = false;

    public int CurrentIndex { get; private set; }

   
    Coroutine snapRoutine;

    void Awake()
    {
        if (!scrollRect) scrollRect = GetComponentInChildren<ScrollRect>();
        if (!viewport) viewport = scrollRect.viewport;
        if (!content) content = scrollRect.content;

        scrollRect.inertia = false;
        
        scrollRect.vertical = false;
    }

    int ItemCount => content.childCount;

    public void MoveRight() => SetIndex(CurrentIndex + 1);
    public void MoveLeft() => SetIndex(CurrentIndex - 1);

    public void SetIndex(int index)
    {
        if (ItemCount == 0) return;

        if (wrap)
            index = (index % ItemCount + ItemCount) % ItemCount;
        else
            index = Mathf.Clamp(index, 0, ItemCount - 1);

        CurrentIndex = index;

        var target = (RectTransform)content.GetChild(CurrentIndex);
        Vector2 targetPos = GetCenteredPosition(target);

        if (snapRoutine != null) StopCoroutine(snapRoutine);
        snapRoutine = StartCoroutine(SnapTo(targetPos));
    }
 
    Vector2 GetCenteredPosition(RectTransform item)
    {
        Canvas.ForceUpdateCanvases();

        Vector3 itemCenterWorld = item.TransformPoint(item.rect.center);
        Vector3 itemCenterLocal = content.InverseTransformPoint(itemCenterWorld);

        Vector3 viewportCenterWorld = viewport.TransformPoint(viewport.rect.center);
        Vector3 viewportCenterLocal = content.InverseTransformPoint(viewportCenterWorld);

        float deltaX = viewportCenterLocal.x - itemCenterLocal.x;
        Vector2 pos = content.anchoredPosition + new Vector2(deltaX, 0f);

        // Clamp content so it doesn't overscroll
        float contentWidth = content.rect.width;
        float viewportWidth = viewport.rect.width;

        if (contentWidth > viewportWidth)
        {
            float minX = -(contentWidth - viewportWidth);
            float maxX = 0f;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
        }
        else
        {
            pos.x = 0f;
        }

        return pos;
    }

    IEnumerator SnapTo(Vector2 target)
    {
        Vector2 start = content.anchoredPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / snapDuration;
            content.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        content.anchoredPosition = target;
    }
}
