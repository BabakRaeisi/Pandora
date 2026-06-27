using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;

public class ToastPanel : MonoBehaviour
{
    public enum ToastType { Info, Success, Warning, Unlock }

    [Header("Refs")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private RTLTextMeshPro titleText;
    [SerializeField] private RTLTextMeshPro messageText;
    [SerializeField] private RectTransform textContainer;

    [Header("Stars")]
    [SerializeField] private RectTransform starsContainer;
    [SerializeField] private Sprite[] starSprites;
    [SerializeField] private int starCount = 20;
    [SerializeField] private Vector2 sizeRange = new Vector2(0.5f, 1.5f);

    [Header("Title Colors per Type")]
    [SerializeField] private Color infoTitleColor    = new Color(0.6f, 0.85f, 1f);
    [SerializeField] private Color successTitleColor = new Color(0.3f, 1f, 0.5f);
    [SerializeField] private Color warningTitleColor = new Color(1f, 0.4f, 0.3f);
    [SerializeField] private Color unlockTitleColor  = new Color(0.9f, 0.6f, 1f);

    [Header("Slide Animation")]
    [SerializeField] private Vector2 hiddenPosition  = new Vector2(0f, -200f);
    [SerializeField] private Vector2 visiblePosition = new Vector2(0f, -60f);
    [SerializeField] private float slideInDuration  = 0.35f;
    [SerializeField] private float holdDuration     = 2.5f;
    [SerializeField] private float slideOutDuration = 0.28f;

    [Header("Scale & Fade")]
    [SerializeField] private float scaleInDuration = 0.35f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Header("Layout")]
    [SerializeField] private float maxWidth = 600f;
    [SerializeField] private float minHeight = 80f;
    [SerializeField] private float extraPadding = 40f;

[SerializeField]
    private CanvasGroup canvasGroup;
    private readonly List<RectTransform> stars = new();
    private Coroutine _current;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        panelRect.anchoredPosition = hiddenPosition;

        SpawnStars();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Show with a title and message.</summary>
    public void Show(string title, string message, ToastType type = ToastType.Info)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ToastRoutine(title, message, type));
    }

    /// <summary>Show message only, no title.</summary>
    public void Show(string message, ToastType type = ToastType.Info)
    {
        Show("", message, type);
    }

    // ── Core Routine ──────────────────────────────────────────────────────────

    IEnumerator ToastRoutine(string title, string message, ToastType type)
    {
        // Set title
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = string.IsNullOrEmpty(title) ? Color.clear : GetTitleColor(type);
        }

        // Message always white
        messageText.text = message;
        messageText.color = Color.white;

        // Wait two frames for layout
        yield return null;
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(textContainer);
        yield return null;

        // Resize panel to fit content
        float contentHeight = LayoutUtility.GetPreferredHeight(textContainer);
        panelRect.sizeDelta = new Vector2(maxWidth, Mathf.Max(minHeight, contentHeight + extraPadding));
        LayoutRebuilder.ForceRebuildLayoutImmediate(textContainer);

        RepositionStars(panelRect.sizeDelta.x, panelRect.sizeDelta.y);

        // Kill any running tweens
        DOTween.Kill(panelRect);
        DOTween.Kill(canvasGroup);

        // Slide in + fade in + scale in
        panelRect.anchoredPosition = hiddenPosition;
        panelRect.localScale = Vector3.one * 0.8f;
        canvasGroup.alpha = 0f;

        panelRect.DOAnchorPos(visiblePosition, slideInDuration).SetEase(showEase);
        panelRect.DOScale(1f, scaleInDuration).SetEase(showEase);
        canvasGroup.DOFade(1f, slideInDuration);

        yield return new WaitForSeconds(slideInDuration + holdDuration);

        // Slide out + fade out
        panelRect.DOAnchorPos(hiddenPosition, slideOutDuration).SetEase(hideEase);
        panelRect.DOScale(0.8f, slideOutDuration).SetEase(hideEase);
        canvasGroup.DOFade(0f, slideOutDuration);

        yield return new WaitForSeconds(slideOutDuration);
    }
 
    // ── Helpers ───────────────────────────────────────────────────────────────

    Color GetTitleColor(ToastType type) => type switch
    {
        ToastType.Success => successTitleColor,
        ToastType.Warning => warningTitleColor,
        ToastType.Unlock  => unlockTitleColor,
        _                 => infoTitleColor
    };

    void SpawnStars()
    {
        Transform parent = starsContainer != null ? starsContainer : transform;
        for (int i = 0; i < starCount; i++)
        {
            GameObject star = new GameObject("Star_" + i, typeof(Image));
            star.transform.SetParent(parent, false);

            Image img = star.GetComponent<Image>();
            if (starSprites != null && starSprites.Length > 0)
                img.sprite = starSprites[Random.Range(0, starSprites.Length)];
            img.raycastTarget = false;

            float s = Random.Range(sizeRange.x, sizeRange.y);
            RectTransform rt = star.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(16f, 16f);
            rt.localScale = new Vector3(s, s, 1f);

            Color c = img.color;
            c.a = Random.Range(0.4f, 1f);
            img.color = c;

            stars.Add(rt);
        }
    }

    void RepositionStars(float width, float height)
    {
        foreach (var star in stars)
        {
            star.DOAnchorPos(new Vector2(
                Random.Range(-width / 2f, width / 2f),
                Random.Range(-height / 2f, height / 2f)
            ), scaleInDuration).SetEase(Ease.OutQuad);
        }
    }
}