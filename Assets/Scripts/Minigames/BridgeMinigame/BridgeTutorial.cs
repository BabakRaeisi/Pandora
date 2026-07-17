using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BridgeTutorial : MonoBehaviour
{
    [Header("Panel Controls")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Button guideButton;
    [SerializeField] private Button closeButton;

    [Header("Stones")]
    [SerializeField] private List<RectTransform> stoneAnchors = new();
    [SerializeField] private List<Image> stoneImages = new();
    [SerializeField, Range(0f, 1f)] private float dimOpacity = 0.5f;
    [SerializeField, Range(0f, 1f)] private float highlightOpacity = 1f;

    [Header("Order")]
    [SerializeField] private bool topToBottom = true; // false = bottom to top

    [Header("Pointer + Result")]
    [SerializeField] private RectTransform pointer;
    [SerializeField] private RectTransform checkMark;
    [SerializeField] private RectTransform errorSign;

    [Header("Timing")]
    [SerializeField] private float sequenceStepDelay = 0.25f;
    [SerializeField] private float pointerMoveDuration = 0.35f;
    [SerializeField] private float pointerClickDuration = 0.16f;
    [SerializeField] private float resultShowDuration = 0.6f;
    [SerializeField] private float loopDelay = 0.7f;

    [Header("Demo")]
    [SerializeField, Range(0f, 1f)] private float wrongDemoChance = 0.35f;
    [SerializeField] private int wrongStepIndex = 2;

    public event Action TutorialCompleted;

    private Coroutine loopRoutine;
    private bool completionSent;

    private void Start()
    {
        if (guideButton != null) guideButton.onClick.AddListener(OpenTutorial);
        if (closeButton != null) closeButton.onClick.AddListener(CloseTutorial);

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (pointer != null) pointer.gameObject.SetActive(false);
        if (checkMark != null) checkMark.gameObject.SetActive(false);
        if (errorSign != null) errorSign.gameObject.SetActive(false);

        SetAllStoneOpacity(dimOpacity);
    }

    public void OpenTutorial()
    {
        completionSent = false;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        if (loopRoutine != null)
            StopCoroutine(loopRoutine);

        loopRoutine = StartCoroutine(RunTutorialLoop());
    }

    public void CloseTutorial()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        Kill(pointer);
        Kill(checkMark);
        Kill(errorSign);

        SetAllStoneOpacity(dimOpacity);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (!completionSent)
        {
            completionSent = true;
            TutorialCompleted?.Invoke();
        }
    }

    private IEnumerator RunTutorialLoop()
    {
        while (tutorialPanel != null && tutorialPanel.activeInHierarchy)
        {
            int[] order = GetOrder();
            if (order.Length == 0)
                yield break;

            ResetFeedback();
            SetAllStoneOpacity(dimOpacity);

            if (pointer != null)
            {
                pointer.gameObject.SetActive(true);
                pointer.localScale = Vector3.one;
            }

            // 1) Show the sequence first
            for (int i = 0; i < order.Length; i++)
            {
                int stoneIndex = order[i];
                if (!IsValidStone(stoneIndex))
                    continue;

                yield return FadeStoneTo(stoneIndex, highlightOpacity, 0.15f);
                yield return new WaitForSeconds(sequenceStepDelay);
            }

            yield return new WaitForSeconds(0.15f);

            // 2) Pointer repeats the same order
            bool madeMistake = false;

            for (int i = 0; i < order.Length; i++)
            {
                int expectedIndex = order[i];
                int targetIndex = expectedIndex;

                // demo a wrong click once, then continue the rest correctly
                if (!madeMistake && i == wrongStepIndex && UnityEngine.Random.value < wrongDemoChance)
                {
                    targetIndex = GetWrongStoneIndex(expectedIndex);
                    madeMistake = true;
                }

                if (!IsValidStone(targetIndex))
                    continue;

                yield return MovePointerToStone(targetIndex);
                yield return ClickAnim();

                // keep the clicked stone highlighted
                yield return FadeStoneTo(targetIndex, highlightOpacity, 0.10f);

                yield return new WaitForSeconds(sequenceStepDelay);
            }

            // 3) End result: check or error
            int resultStone = order[order.Length - 1];

            if (madeMistake)
                ShowError(resultStone);
            else
                ShowCheck(resultStone);

            yield return new WaitForSeconds(resultShowDuration);
            ResetFeedback();

            if (pointer != null)
                pointer.gameObject.SetActive(false);

            yield return new WaitForSeconds(loopDelay);
        }
    }

    private int[] GetOrder()
    {
        int count = Mathf.Min(4, stoneAnchors.Count);
        if (count <= 0) return Array.Empty<int>();

        int[] order = new int[count];

        if (topToBottom)
        {
            for (int i = 0; i < count; i++)
                order[i] = i;
        }
        else
        {
            for (int i = 0; i < count; i++)
                order[i] = count - 1 - i;
        }

        return order;
    }

    private int GetWrongStoneIndex(int expectedIndex)
    {
        if (stoneAnchors == null || stoneAnchors.Count == 0)
            return expectedIndex;

        for (int i = 0; i < stoneAnchors.Count; i++)
        {
            if (i != expectedIndex && IsValidStone(i))
                return i;
        }

        return expectedIndex;
    }

    private IEnumerator MovePointerToStone(int stoneIndex)
    {
        if (pointer == null) yield break;

        Vector2 target = GetStonePos(stoneIndex);
        bool done = false;

        pointer.DOKill();
        pointer.DOAnchorPos(target, pointerMoveDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => done = true);

        yield return new WaitUntil(() => done);
    }

    private IEnumerator ClickAnim()
    {
        if (pointer == null) yield break;

        bool done = false;

        pointer.DOKill();
        Sequence seq = DOTween.Sequence();
        seq.Append(pointer.DOScale(0.82f, pointerClickDuration * 0.5f))
           .Append(pointer.DOScale(1f, pointerClickDuration * 0.5f))
           .OnComplete(() => done = true);

        yield return new WaitUntil(() => done);
    }

    private IEnumerator FadeStoneTo(int index, float targetOpacity, float duration)
    {
        if (!IsValidStone(index)) yield break;

        var img = stoneImages[index];
        if (img == null) yield break;

        float start = img.color.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start, targetOpacity, t / duration);
            SetStoneOpacity(index, a);
            yield return null;
        }

        SetStoneOpacity(index, targetOpacity);
    }

    private void ShowCheck(int stoneIndex)
    {
        if (checkMark == null) return;

        checkMark.anchoredPosition = GetStonePos(stoneIndex) + new Vector2(0f, 70f);
        checkMark.gameObject.SetActive(true);

        if (errorSign != null)
            errorSign.gameObject.SetActive(false);
    }

    private void ShowError(int stoneIndex)
    {
        if (errorSign == null) return;

        errorSign.anchoredPosition = GetStonePos(stoneIndex) + new Vector2(0f, 70f);
        errorSign.gameObject.SetActive(true);

        if (checkMark != null)
            checkMark.gameObject.SetActive(false);
    }

    private void ResetFeedback()
    {
        if (checkMark != null) checkMark.gameObject.SetActive(false);
        if (errorSign != null) errorSign.gameObject.SetActive(false);
    }

    private void SetAllStoneOpacity(float opacity)
    {
        for (int i = 0; i < stoneImages.Count; i++)
            SetStoneOpacity(i, opacity);
    }

    private void SetStoneOpacity(int index, float opacity)
    {
        if (!IsValidStone(index)) return;
        if (stoneImages[index] == null) return;

        Color c = stoneImages[index].color;
        c.a = opacity;
        stoneImages[index].color = c;
    }

    private Vector2 GetStonePos(int index)
    {
        if (!IsValidStone(index))
            return Vector2.zero;

        return stoneAnchors[index].anchoredPosition;
    }

    private bool IsValidStone(int index)
    {
        return stoneAnchors != null &&
               stoneImages != null &&
               index >= 0 &&
               index < stoneAnchors.Count &&
               index < stoneImages.Count &&
               stoneAnchors[index] != null;
    }

    private static void Kill(RectTransform rt)
    {
        if (rt == null) return;
        rt.DOKill();
        rt.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (guideButton != null) guideButton.onClick.RemoveListener(OpenTutorial);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseTutorial);
    }
}