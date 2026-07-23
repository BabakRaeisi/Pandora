using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SWMTutorial : MonoBehaviour
{
    [Serializable]
    public class TutorialChest
    {
        [Header("UI")]
        public RectTransform rect;

        [Tooltip("Assign the Image component on the child ChestVisual object.")]
        public Image chestVisual;

        [Header("Chest Images")]
        public Sprite closedSprite;
        public Sprite emptySprite;
        public Sprite fullSprite;

        [Header("Tutorial Data")]
        [Tooltip("If enabled, this chest reveals the full/treasure image.")]
        public bool hasTreasure;
    }

    [Header("Panel")]
    [SerializeField] private GameObject tutorialPanel;

    [Header("One Tutorial Slide")]
    [SerializeField] private RectTransform field;
    [SerializeField] private Button closeButton;

    [Header("Demo Chests (assign 4)")]
    [SerializeField] private List<TutorialChest> demoChests = new();

    [Header("Pointer and Error")]
    [SerializeField] private RectTransform pointer;
    [SerializeField] private RectTransform errorSign;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float initialDelay = 0.7f;
    [SerializeField, Min(0.1f)] private float pointerMoveDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float pointerClickDuration = 0.2f;
    [SerializeField, Min(0.1f)] private float chestRevealDuration = 0.6f;
    [SerializeField, Min(0.1f)] private float errorShowDuration = 0.7f;
    [SerializeField, Min(0.1f)] private float loopPause = 0.8f;

    public event Action TutorialCompleted;

    private Coroutine tutorialRoutine;
    private bool tutorialCompletionSent;
    private bool tutorialIsOpen;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CompleteTutorial);
            closeButton.onClick.AddListener(CompleteTutorial);
        }

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        SetAllChestsClosed();
        HidePointerAndError();
    }

    private void OnDisable()
    {
        StopTutorialLoop();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CompleteTutorial);

        pointer?.DOKill();
        errorSign?.DOKill();
    }

    // Called only by SWMGameManager.
    public void OpenTutorial()
    {
        tutorialCompletionSent = false;
        tutorialIsOpen = true;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        SetAllChestsClosed();
        HidePointerAndError();

        StopTutorialLoop();
        tutorialRoutine = StartCoroutine(RunTutorialLoop());
    }

    // Called by the tutorial's Next / Got It button.
    public void CompleteTutorial()
    {
        if (!tutorialIsOpen)
            return;

        tutorialIsOpen = false;
        StopTutorialLoop();

        SetAllChestsClosed();
        HidePointerAndError();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (tutorialCompletionSent)
            return;

        tutorialCompletionSent = true;
        TutorialCompleted?.Invoke();
    }

    private IEnumerator RunTutorialLoop()
    {
        while (tutorialIsOpen)
        {
            // A new trial starts: every chest is closed.
            SetAllChestsClosed();
            HidePointerAndError();

            yield return new WaitForSeconds(initialDelay);

            if (!tutorialIsOpen)
                yield break;

            int treasureChestIndex = GetTreasureChestIndex();

            if (treasureChestIndex < 0)
            {
                Debug.LogWarning(
                    "[SWMTutorial] Assign exactly one demo chest with Has Treasure enabled.",
                    this
                );

                yield return new WaitForSeconds(loopPause);
                continue;
            }

            // Demonstrate searching an empty chest first.
            int emptyChestIndex = GetFirstEmptyChestIndex(treasureChestIndex);

            if (emptyChestIndex >= 0)
            {
                yield return MovePointerTo(
                    demoChests[emptyChestIndex].rect.anchoredPosition
                );

                yield return AnimatePointerClick();

                // Empty chests remain open for the rest of the trial.
                RevealChest(emptyChestIndex);

                yield return new WaitForSeconds(chestRevealDuration);

                if (!tutorialIsOpen)
                    yield break;

                // The chest closes after the player sees it is empty.
                CloseChest(emptyChestIndex);

                yield return new WaitForSeconds(0.25f);

                if (!tutorialIsOpen)
                    yield break;

                // Second click on the same chest: it opens empty again.
                // This demonstrates the repeated-selection error.
                yield return MovePointerTo(
                    demoChests[emptyChestIndex].rect.anchoredPosition
                );

                yield return AnimatePointerClick();
                RevealChest(emptyChestIndex);

                yield return new WaitForSeconds(chestRevealDuration * 0.5f);

                if (!tutorialIsOpen)
                    yield break;

                yield return AnimatePointerError();
                yield return ShowErrorAtPointer();

                CloseChest(emptyChestIndex);

                if (!tutorialIsOpen)
                    yield break;

                yield return new WaitForSeconds(0.2f);
            }

            // Find the one treasure. This completes the trial.
            yield return MovePointerTo(
                demoChests[treasureChestIndex].rect.anchoredPosition
            );

            yield return AnimatePointerClick();

            RevealChest(treasureChestIndex);

            yield return new WaitForSeconds(chestRevealDuration);

            if (!tutorialIsOpen)
                yield break;

            // The following reset represents the next trial:
            // one new treasure location, and chests return to closed state.
            pointer.gameObject.SetActive(false);

            yield return new WaitForSeconds(loopPause);
        }
    }

    private IEnumerator MovePointerTo(Vector2 targetPosition)
    {
        if (pointer == null)
            yield break;

        pointer.DOKill();
        pointer.gameObject.SetActive(true);
        pointer.localScale = Vector3.one;

        bool completed = false;

        pointer
            .DOAnchorPos(targetPosition, pointerMoveDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => completed = true);

        yield return new WaitUntil(() => completed);
    }

    private IEnumerator AnimatePointerClick()
    {
        if (pointer == null)
            yield break;

        pointer.DOKill();

        bool completed = false;

        DOTween.Sequence()
            .Append(
                pointer
                    .DOScale(0.82f, pointerClickDuration * 0.5f)
                    .SetEase(Ease.InQuad)
            )
            .Append(
                pointer
                    .DOScale(1f, pointerClickDuration * 0.5f)
                    .SetEase(Ease.OutQuad)
            )
            .OnComplete(() => completed = true);

        yield return new WaitUntil(() => completed);
    }

    private IEnumerator AnimatePointerError()
    {
        if (pointer == null)
            yield break;

        pointer.DOKill();

        bool completed = false;

        pointer
            .DOShakeAnchorPos(
                0.3f,
                new Vector2(18f, 0f),
                20,
                0f,
                false,
                true
            )
            .OnComplete(() => completed = true);

        yield return new WaitUntil(() => completed);
    }

    private IEnumerator ShowErrorAtPointer()
    {
        if (errorSign == null || pointer == null)
            yield break;

        errorSign.DOKill();
        errorSign.anchoredPosition = pointer.anchoredPosition;
        errorSign.localScale = Vector3.one * 0.75f;
        errorSign.gameObject.SetActive(true);

        bool completed = false;

        DOTween.Sequence()
            .Append(errorSign.DOScale(1f, 0.12f).SetEase(Ease.OutBack))
            .AppendInterval(errorShowDuration)
            .Append(errorSign.DOScale(0.75f, 0.12f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                if (errorSign != null)
                    errorSign.gameObject.SetActive(false);

                completed = true;
            });

        yield return new WaitUntil(() => completed);
    }

    private void RevealChest(int index)
    {
        if (!IsChestValid(index))
            return;

        TutorialChest chest = demoChests[index];

        chest.chestVisual.sprite = chest.hasTreasure
            ? chest.fullSprite
            : chest.emptySprite;

        chest.chestVisual.transform.DOKill();
        chest.chestVisual.transform.localScale = Vector3.one;

        chest.chestVisual.transform
            .DOPunchScale(
                new Vector3(0.1f, 0.1f, 0f),
                0.25f,
                5,
                0.5f
            );
    }

    private void SetAllChestsClosed()
    {
        if (demoChests == null)
            return;

        foreach (TutorialChest chest in demoChests)
        {
            if (chest == null || chest.chestVisual == null)
                continue;

            chest.chestVisual.transform.DOKill();
            chest.chestVisual.transform.localScale = Vector3.one;
            chest.chestVisual.sprite = chest.closedSprite;
        }
    }

    private void HidePointerAndError()
    {
        if (pointer != null)
        {
            pointer.DOKill();
            pointer.localScale = Vector3.one;
            pointer.gameObject.SetActive(false);
        }

        if (errorSign != null)
        {
            errorSign.DOKill();
            errorSign.localScale = Vector3.one;
            errorSign.gameObject.SetActive(false);
        }
    }

    private int GetFirstValidChestIndex()
    {
        for (int i = 0; i < demoChests.Count; i++)
        {
            if (IsChestValid(i))
                return i;
        }

        return -1;
    }

    private bool IsChestValid(int index)
    {
        return demoChests != null &&
               index >= 0 &&
               index < demoChests.Count &&
               demoChests[index] != null &&
               demoChests[index].rect != null &&
               demoChests[index].chestVisual != null;
    }

    private void StopTutorialLoop()
    {
        if (tutorialRoutine == null)
            return;

        StopCoroutine(tutorialRoutine);
        tutorialRoutine = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (demoChests != null && demoChests.Count > 4)
            demoChests.RemoveRange(4, demoChests.Count - 4);
    }
#endif

    private void CloseChest(int index)
    {
        if (!IsChestValid(index))
            return;

        TutorialChest chest = demoChests[index];

        chest.chestVisual.transform.DOKill();
        chest.chestVisual.transform.localScale = Vector3.one;
        chest.chestVisual.sprite = chest.closedSprite;
    }

    private int GetTreasureChestIndex()
    {
        for (int i = 0; i < demoChests.Count; i++)
        {
            if (demoChests[i].hasTreasure)
                return i;
        }

        return -1;
    }

    private int GetFirstEmptyChestIndex(int treasureChestIndex)
    {
        for (int i = 0; i < demoChests.Count; i++)
        {
            if (i != treasureChestIndex && demoChests[i].hasTreasure == false)
                return i;
        }

        return -1;
    }
}