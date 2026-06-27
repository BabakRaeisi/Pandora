using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using DG.Tweening;

public class StarFieldRandomizer : MonoBehaviour
{
    [Header("Stars")]
    [SerializeField] private RectTransform starsContainer;
    [SerializeField] private Sprite[] starSprites;
    [SerializeField] private int starCount = 20;
    [SerializeField] private Vector2 sizeRange = new Vector2(0.5f, 1.5f);

    [Header("Chat Box")]
    [SerializeField] private RTLTextMeshPro titleText;
    [SerializeField] private RTLTextMeshPro messageText;
    [SerializeField] private RectTransform textContainer;
    [SerializeField] private float maxWidth = 600f;
    [SerializeField] private float minHeight = 80f;
    [SerializeField] private float extraPadding = 40f; // ← add this, tweak in Inspector

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float scaleInDuration = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private readonly List<RectTransform> stars = new List<RectTransform>();
    private Coroutine showRoutine;

    void Awake()
    {
        rt = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        rt.localScale = Vector3.zero;

        SpawnStars();
    }

    // ─── Context Menu Tests ───────────────────────────────────────────

    
    public void TestShowDialogue()
    {
        string title = "اشکالی نداره!";
        string text = "یکم اشتباه شد ولی ستاره‌ها صبورن و منتظر. دوباره نگاه کن، این بار آروم‌ تر." ; 
        ShowDialogue(title,text);
    }

  
    public void TestShowNarrative()
    {
        string text =  "ایول! ستاره‌های این جزیره دوباره راهشون رو پیدا کردن. یأس از این جزیره فرار کرد!";
        ShowNarrative(text);
    }

  
    public void TestHide()
    {
        Hide();
    }

   
    public void TestShortMessage()
    {
        ShowDialogue("نگهبان", "بایست!");
    }

    // ─── Public API ───────────────────────────────────────────────────

    public void ShowDialogue(string title, string message)
    {
        if (titleText != null)
            titleText.text = title; // always visible, never hidden

        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowRoutine(message));
    }

    public void ShowNarrative(string message)
    {
        if (titleText != null)
            titleText.text = ""; // empty string so layout group collapses it naturally

        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowRoutine(message));
    }

    public void Hide()
    {
        DOTween.Kill(rt);
        DOTween.Kill(canvasGroup);
        rt.DOScale(0f, fadeOutDuration).SetEase(hideEase);
        canvasGroup.DOFade(0f, fadeOutDuration);
    }

    // ─── Private ──────────────────────────────────────────────────────

    private IEnumerator ShowRoutine(string message)
    {
        messageText.text = message;

        yield return null;
        yield return null;

        LayoutRebuilder.ForceRebuildLayoutImmediate(textContainer);

        yield return null;

        // Get the preferred height from the layout, add extra padding on top and bottom
        float contentHeight = LayoutUtility.GetPreferredHeight(textContainer);
        float newWidth  = maxWidth;
        float newHeight = Mathf.Max(minHeight, contentHeight + extraPadding);

        rt.sizeDelta = new Vector2(newWidth, newHeight);

        // Force rebuild again now that parent is resized
        LayoutRebuilder.ForceRebuildLayoutImmediate(textContainer);

        DOTween.Kill(rt);
        DOTween.Kill(canvasGroup);

        rt.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        rt.DOScale(1f, scaleInDuration).SetEase(showEase);
        canvasGroup.DOFade(1f, fadeInDuration);

        RepositionStars(newWidth, newHeight);
    }

    private void SpawnStars()
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

            float randomScale = Random.Range(sizeRange.x, sizeRange.y);
            RectTransform starRt = star.GetComponent<RectTransform>();
            starRt.sizeDelta  = new Vector2(16f, 16f);
            starRt.localScale = new Vector3(randomScale, randomScale, 1);

            Color c = img.color;
            c.a = Random.Range(0.4f, 1f);
            img.color = c;

            stars.Add(starRt);
        }
    }

    private void RepositionStars(float width, float height)
    {
        foreach (var starRt in stars)
        {
            starRt.DOAnchorPos(new Vector2(
                Random.Range(-width / 2f, width / 2f),
                Random.Range(-height / 2f, height / 2f)
            ), scaleInDuration).SetEase(Ease.OutQuad);
        }
    }
}