using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstellationController : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform playArea;
    public RectTransform linePrefab;
    public ConstellationStar[] stars; // 9

    [Header("Line Style")]
    public float lineThickness = 8f;

    RectTransform linesContainer;

    readonly List<ConstellationStar> selected = new();
    readonly List<RectTransform> lines = new();

    bool inputEnabled;
    int[] targetSequence;

    public event Action<bool> OnTrialFinished; // bool success

    void Awake()
    {
        CreateLinesContainer();

        foreach (var s in stars)
        {
            s.button.onClick.RemoveAllListeners();
            s.button.onClick.AddListener(() => OnStarClicked(s));
        }

        ResetAll();
    }

    public void BeginTrial(int[] sequence, float starOnSeconds, float gapSeconds)
    {
        StopAllCoroutines();
        targetSequence = sequence;

        ResetAll();
        StartCoroutine(Presentation(starOnSeconds, gapSeconds));
    }

    IEnumerator Presentation(float starOnSeconds, float gapSeconds)
    {
        inputEnabled = false;

        for (int i = 0; i < targetSequence.Length; i++)
        {
            var star = GetStar(targetSequence[i]);
            star.SetBright();
            yield return new WaitForSeconds(starOnSeconds);
            star.SetDim();
            yield return new WaitForSeconds(gapSeconds);
        }

        inputEnabled = true;
    }

    void OnStarClicked(ConstellationStar star)
    {
        if (!inputEnabled) return;
        if (selected.Count > 0 && selected[^1] == star) return;

        star.SetBright();
        selected.Add(star);

        if (selected.Count >= 2)
            DrawLine(selected[^2], star);

        if (targetSequence != null && selected.Count == targetSequence.Length)
        {
            inputEnabled = false;
            bool success = CheckSuccess();
            OnTrialFinished?.Invoke(success);
        }
    }

    bool CheckSuccess()
    {
        for (int i = 0; i < targetSequence.Length; i++)
        {
            if (selected[i].id != targetSequence[i])
                return false;
        }
        return true;
    }

    void CreateLinesContainer()
    {
        GameObject go = new GameObject("Lines");
        go.transform.SetParent(playArea, false);

        linesContainer = go.AddComponent<RectTransform>();
        linesContainer.anchorMin = Vector2.zero;
        linesContainer.anchorMax = Vector2.one;
        linesContainer.offsetMin = Vector2.zero;
        linesContainer.offsetMax = Vector2.zero;
    }

    void DrawLine(ConstellationStar a, ConstellationStar b)
    {
        RectTransform seg = Instantiate(linePrefab, linesContainer);

        Vector2 aPos = (Vector2)playArea.InverseTransformPoint(a.Rect.position);
        Vector2 bPos = (Vector2)playArea.InverseTransformPoint(b.Rect.position);

        UILineSegment.Place(seg, aPos, bPos, lineThickness);
        lines.Add(seg);
    }

    ConstellationStar GetStar(int id)
    {
        for (int i = 0; i < stars.Length; i++)
            if (stars[i].id == id) return stars[i];

        Debug.LogError($"Star {id} not found");
        return null;
    }

    public void ResetAll()
    {
        foreach (var s in stars)
            s.SetDim();

        for (int i = 0; i < lines.Count; i++)
            if (lines[i]) Destroy(lines[i].gameObject);

        lines.Clear();
        selected.Clear();
    }
}
