using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using RTLTMPro;
using UnityEngine.UI;

public class IntroductionManager : MonoBehaviour
{
    [System.Serializable]
    private class IntroductionSlide
    {
        [TextArea(3, 10)]
        public string message;
        public Sprite loomakSprite;
    }

    [Header("UI")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private RTLTextMeshPro introText;
    [SerializeField] private Image loomakImage;
    [SerializeField] private RectTransform fillContainer;
    [SerializeField] private RectTransform frameContainer;
    [SerializeField] private Button nextButton;

    [Header("Slides")]
    [SerializeField] private IntroductionSlide[] slides;

    [Header("Flow")]
    [SerializeField] private bool showOnlyFirstVisit = true;
    [SerializeField] private bool forceShowOnStart = false;

    [Header("Container Resize")]
    [SerializeField] private Vector2 textPadding = new Vector2(40f, 25f);
    [SerializeField] private Vector2 starsLayerMargin = new Vector2(12f, 12f);
    [SerializeField] private Vector2 minFillSize = new Vector2(300f, 120f);
    [SerializeField] private float maxTextWidth = 700f;
    [SerializeField] private bool resizeIntroPanel = false; // keep introPanel fixed by default

    [Header("Events")]
    [SerializeField] private UnityEvent onComplete;

    private int currentIndex;
    private string sceneVisitKey;
    private RectTransform introPanelRect;

    private void Awake()
    {
        sceneVisitKey = $"IntroShown_{SceneManager.GetActiveScene().name}";

        if (introPanel != null)
        {
            introPanelRect = introPanel.GetComponent<RectTransform>();
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextMessage);
            nextButton.onClick.AddListener(ShowNextMessage);
        }
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextMessage);
        }
    }

    public event Action IntroductionCompleted;

    private void Start()
    {
        bool hasSlides = slides != null && slides.Length > 0;
        if (!hasSlides)
        {
            if (introPanel != null)
            {
                introPanel.SetActive(false);
            }

            NotifyCompleted();
            return;
        }

        bool alreadyShown = PlayerPrefs.GetInt(sceneVisitKey, 0) == 1;

        bool testingPreview =
            forceShowOnStart ||
            (introPanel != null && introPanel.activeSelf);

        if (testingPreview)
        {
            currentIndex = 0;

            if (introPanel != null)
            {
                introPanel.SetActive(true);
            }

            ShowCurrentMessage();
            return;
        }

        bool shouldShow = !showOnlyFirstVisit || !alreadyShown;

        if (!shouldShow)
        {
            if (introPanel != null)
            {
                introPanel.SetActive(false);
            }

            NotifyCompleted();
            return;
        }

        currentIndex = 0;

        if (introPanel != null)
        {
            introPanel.SetActive(true);
        }

        ShowCurrentMessage();
    }

    public void ShowNextMessage()
    {
        if (slides == null || slides.Length == 0)
        {
            CompleteIntroduction();
            return;
        }

        currentIndex++;

        if (currentIndex >= slides.Length)
        {
            CompleteIntroduction();
            return;
        }

        ShowCurrentMessage();
    }

    public void ShowNextImage()
    {
        ShowNextMessage();
    }

    private void ShowCurrentMessage()
    {
        if (slides == null || slides.Length == 0 || currentIndex < 0 || currentIndex >= slides.Length)
        {
            return;
        }

        IntroductionSlide currentSlide = slides[currentIndex];

        if (introText != null)
        {
            introText.text = currentSlide.message;
            Canvas.ForceUpdateCanvases();
            introText.ForceMeshUpdate();
        }

        if (loomakImage != null)
        {
            loomakImage.sprite = currentSlide.loomakSprite;
            loomakImage.enabled = currentSlide.loomakSprite != null;
            loomakImage.preserveAspect = true;
        }

        ResizeContainers();
    }

    private void ResizeContainers()
    {
        if (introText == null)
        {
            return;
        }

        Vector2 preferredTextSize = introText.GetPreferredValues(introText.text, maxTextWidth, 0f);

        float fillWidth = Mathf.Max(minFillSize.x, preferredTextSize.x + (textPadding.x * 2f));
        float fillHeight = Mathf.Max(minFillSize.y, preferredTextSize.y + (textPadding.y * 2f));

        float frameWidth = fillWidth + (starsLayerMargin.x * 2f);
        float frameHeight = fillHeight + (starsLayerMargin.y * 2f);

        if (fillContainer != null)
        {
            fillContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth);
            fillContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fillHeight);
            fillContainer.anchoredPosition = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(fillContainer);
        }

        // Resize only explicit frame container, not introPanel fallback
        if (frameContainer != null)
        {
            frameContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frameWidth);
            frameContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, frameHeight);
            frameContainer.anchoredPosition = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(frameContainer);
        }

        // Optional: allow introPanel resize only if explicitly enabled
        if (resizeIntroPanel && introPanelRect != null)
        {
            introPanelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frameWidth);
            introPanelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, frameHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(introPanelRect);
        }

        RectTransform textRect = introText.rectTransform;
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth - (textPadding.x * 2f));
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fillHeight - (textPadding.y * 2f));
        textRect.anchoredPosition = Vector2.zero;

        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
    }

    private void CompleteIntroduction()
    {
        PlayerPrefs.SetInt(sceneVisitKey, 1);
        PlayerPrefs.Save();

        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }

        NotifyCompleted();
    }

    private void NotifyCompleted()
    {
        onComplete?.Invoke();
        IntroductionCompleted?.Invoke();
    }

    public void ResetSceneIntroduction()
    {
        PlayerPrefs.DeleteKey(sceneVisitKey);
        PlayerPrefs.Save();
    }
}
