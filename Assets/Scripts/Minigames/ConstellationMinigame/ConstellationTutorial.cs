using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for Image
using DG.Tweening;    // Required for DOTween

public class ConstellationTutorial : MonoBehaviour
{
    [Header("Panel Controls")]
    [SerializeField] private GameObject tutorialPanel; // Assign the main overlay panel GameObject
    [SerializeField] private Button guideButton;       // Assign GuideBtn from your main HUD
    [SerializeField] private Button closeButton;       // Tick button: next slide, then close on last slide

    [Header("Slides (assign 3)")]
    [SerializeField] private List<GameObject> slides = new List<GameObject>();
    [SerializeField] private int requiredSlidesCount = 3;

    [Header("UI References")]
    [SerializeField] private List<Image> starsList;
    [SerializeField] private RectTransform pointer; // Use RectTransform for UI positioning

    [Header("Sprites")]
    [SerializeField] private Sprite starOffSprite;
    [SerializeField] private Sprite starOnSprite;

    [Header("Timing Configuration")]
    [SerializeField] private float starFlashDuration = 1.2f;
    [SerializeField] private float pointerMoveDuration = 0.5f;
    [SerializeField] private float pointerClickDuration = 0.2f;
    [SerializeField] private float loopPause = 0.4f;

    private readonly List<int> patternSequence = new List<int> { 0, 2, 1, 3 };
    private Coroutine tutorialRoutine;

    public event Action TutorialCompleted;
    private bool tutorialCompletionSent;
    private int currentSlideIndex;

    void Start()
    {
        if (guideButton != null) guideButton.onClick.AddListener(OpenTutorial);
        if (closeButton != null) closeButton.onClick.AddListener(OnTickButtonClicked);

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (pointer != null) pointer.gameObject.SetActive(false);

        SetAllSlidesInactive();
    }

    public void OpenTutorial()
    {
        tutorialCompletionSent = false;
        currentSlideIndex = 0;

        ResetTutorialState();

        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        ShowSlide(currentSlideIndex);

        tutorialRoutine = StartCoroutine(RunUITutorialLoop());
    }

    private void OnTickButtonClicked()
    {
        int slideCount = Mathf.Min(requiredSlidesCount, slides.Count);

        if (slideCount <= 0)
        {
            CloseTutorial();
            return;
        }

        if (currentSlideIndex < slideCount - 1)
        {
            currentSlideIndex++;
            ShowSlide(currentSlideIndex);
            return;
        }

        // Last slide reached -> same behavior as old close button
        CloseTutorial();
    }

    public void CloseTutorial()
    {
        ResetTutorialState();
        SetAllSlidesInactive();

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        RaiseTutorialCompleted();
    }

    private IEnumerator RunUITutorialLoop()
    {
        while (tutorialPanel != null && tutorialPanel.activeInHierarchy)
        {
            yield return RunSingleTutorialPass();
            if (loopPause > 0f) yield return new WaitForSeconds(loopPause);
        }
    }

    private IEnumerator RunSingleTutorialPass()
    {
        if (starsList == null || starsList.Count == 0 || pointer == null)
            yield break;

        // PHASE 1: Flash pattern sequence
        foreach (int index in patternSequence)
        {
            if (!IsValidStarIndex(index)) continue;

            FlashStar(index);
            yield return new WaitForSeconds(starFlashDuration + 0.7f);
        }

        yield return new WaitForSeconds(0.5f);

        // PHASE 2: Pointer guidance
        pointer.gameObject.SetActive(true);
        pointer.localScale = Vector3.one;

        int firstIndex = patternSequence[0];
        if (IsValidStarIndex(firstIndex))
            pointer.anchoredPosition = starsList[firstIndex].rectTransform.anchoredPosition;

        foreach (int index in patternSequence)
        {
            if (!IsValidStarIndex(index)) continue;

            Vector2 targetAnchoredPos = starsList[index].rectTransform.anchoredPosition;

            bool moveComplete = false;
            pointer.DOAnchorPos(targetAnchoredPos, pointerMoveDuration)
                   .SetEase(Ease.InOutQuad)
                   .OnComplete(() => moveComplete = true);
            yield return new WaitUntil(() => moveComplete);

            bool clickComplete = false;
            Sequence clickSeq = DOTween.Sequence();
            clickSeq.Append(pointer.DOScale(0.8f, pointerClickDuration / 2f).SetEase(Ease.InQuad))
                    .AppendCallback(() => FlashStar(index))
                    .Append(pointer.DOScale(1.0f, pointerClickDuration / 2f).SetEase(Ease.OutQuad))
                    .OnComplete(() => clickComplete = true);

            yield return new WaitUntil(() => clickComplete);
            yield return new WaitForSeconds(0.15f);
        }

        pointer.gameObject.SetActive(false);
    }

    private void FlashStar(int index)
    {
        if (!IsValidStarIndex(index)) return;

        Image starImage = starsList[index];
        if (starImage == null) return;

        starImage.sprite = starOnSprite;

        starImage.rectTransform.DOKill(true);
        starImage.rectTransform.DOPunchScale(Vector3.one * 0.15f, starFlashDuration, 1, 0.5f);

        DOVirtual.DelayedCall(starFlashDuration, () =>
        {
            if (starImage != null) starImage.sprite = starOffSprite;
        }).SetId(this);
    }

    private void ResetTutorialState()
    {
        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }

        DOTween.Kill(this);

        if (pointer != null)
        {
            pointer.DOKill();
            pointer.gameObject.SetActive(false);
            pointer.localScale = Vector3.one;
        }

        if (starsList != null)
        {
            foreach (var star in starsList)
            {
                if (star == null) continue;
                star.rectTransform.DOKill();
                star.rectTransform.localScale = Vector3.one;
                star.sprite = starOffSprite;
            }
        }
    }

    private void ShowSlide(int index)
    {
        SetAllSlidesInactive();

        if (index >= 0 && index < slides.Count && slides[index] != null)
            slides[index].SetActive(true);
    }

    private void SetAllSlidesInactive()
    {
        if (slides == null) return;

        foreach (var slide in slides)
        {
            if (slide != null) slide.SetActive(false);
        }
    }

    private bool IsValidStarIndex(int index)
    {
        return starsList != null && index >= 0 && index < starsList.Count && starsList[index] != null;
    }

    private void OnDestroy()
    {
        if (guideButton != null) guideButton.onClick.RemoveListener(OpenTutorial);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnTickButtonClicked);

        DOTween.Kill(this);
    }

    private void RaiseTutorialCompleted()
    {
        if (tutorialCompletionSent) return;
        tutorialCompletionSent = true;
        TutorialCompleted?.Invoke();
    }
}