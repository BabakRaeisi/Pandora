// BridgeGameManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using RTLTMPro;

public class BridgeGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SWMHUD hud;
    [SerializeField] private BridgeConfig config;
    [SerializeField] private SessionDataSO sessionData;

    [Header("Countdown")]
    [SerializeField] private RTLTextMeshPro countdownText;

    [Header("UI Layout (Anchors)")]
    [SerializeField] private RectTransform playArea;
    [SerializeField] private RectTransform topChasmAnchor;
    [SerializeField] private RectTransform bottomChasmAnchor;

    [Header("Day (1..7)")]
    [SerializeField, Range(1, 7)] private int day = 1;

    [Header("Pieces (IDs 0..11, 2 columns)")]
    [SerializeField] private List<BridgePieceUI> pieces = new();

    [Header("Board")]
    [SerializeField] private int cols = 2;
    [SerializeField] private int totalRows = 6;

    [Header("Placement")]
    [SerializeField] private float columnGap = 260f;
    [SerializeField] private float staggerX = 35f;

    [Header("Direction")]
    [SerializeField] private bool randomizeBottomToTop = true;
    [SerializeField, Range(0f, 1f)] private float bottomToTopChance = 0.5f;

    private BridgeConfig.DayConfig dayCfg;
    private int trialIndex;

    private readonly Dictionary<int, BridgePieceUI> piecesById = new();

    private int activeRows;
    private int goalPieces;
    private List<int> targetSequence = new();

    private float trialStartTime;
    private bool trialComplete;
    private bool inputEnabled;
    private int builtCount;
    private int wrongAttempts;

    private void Awake()
    {
        if (pieces == null || pieces.Count == 0)
            pieces = new List<BridgePieceUI>(GetComponentsInChildren<BridgePieceUI>(true));

        piecesById.Clear();

        foreach (var p in pieces)
        {
            if (!p) continue;

            if (piecesById.ContainsKey(p.Id))
            {
                Debug.LogError($"BridgeGameManager: Duplicate Id={p.Id} on '{p.name}'.");
                continue;
            }

            piecesById.Add(p.Id, p);

            int r = p.Id / cols;
            int c = p.Id % cols;
            p.SetGrid(r, c);

            p.Clicked -= OnPiecePressed;
            p.Clicked += OnPiecePressed;
        }
    }

    private void OnDestroy()
    {
        foreach (var kv in piecesById)
            if (kv.Value) kv.Value.Clicked -= OnPiecePressed;
    }

    private void Start()
    {
        AudioManager.Instance.StopAll();
        AudioManager.Instance.Play("BridgeAmbient");

        StartCoroutine(BeginGameAfterCountdown());
    }

    private IEnumerator BeginGameAfterCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            yield return new WaitForSeconds(1f);

            countdownText.text = "2";
            yield return new WaitForSeconds(1f);

            countdownText.text = "1";
            yield return new WaitForSeconds(1f);

            countdownText.gameObject.SetActive(false);
        }

        StartDay(PlayerDataManager.Instance.Data.currentDay);
    }

    public void StartDay(int dayNumber)
    {
        day = Mathf.Clamp(dayNumber, 1, 7);

        if (!config || !sessionData)
        {
            Debug.LogError("BridgeGameManager: Missing config or sessionData.");
            return;
        }

        dayCfg = config.GetDay(day);

        trialIndex = 0;
        hud?.SetupDay(dayCfg.trials);
        hud?.SetTrialsDone(0);

        StartNextTrial();
    }

    public void StartNextTrial()
    {
        if (trialIndex >= dayCfg.trials)
        {
            hud?.ShowDayComplete();
            return;
        }

        StopAllCoroutines();

        trialComplete = false;
        inputEnabled = false;
        builtCount = 0;
        wrongAttempts = 0;

        activeRows = Mathf.Clamp(dayCfg.minPieces, 2, totalRows);

        if (dayCfg.pattern == BridgePattern.ZigZag)
        {
            goalPieces = Mathf.Clamp(dayCfg.maxPieces, activeRows + 1, activeRows * 2);
        }
        else
        {
            int minLen = Mathf.Max(dayCfg.minPieces, activeRows);
            int maxLen = Mathf.Min(dayCfg.maxPieces, activeRows * 2);
            goalPieces = UnityEngine.Random.Range(minLen, maxLen + 1);
        }

        ApplyActiveSpan(activeRows);
        LayoutActiveSpanConnectingChasms(activeRows);

        bool startFromBottom = randomizeBottomToTop
            ? (UnityEngine.Random.value < bottomToTopChance)
            : false;

        bool forceSwitch = (dayCfg.pattern == BridgePattern.ZigZag);

        targetSequence = BridgePathGenerator.Generate2ColPath(
            activeRows,
            goalPieces,
            startFromBottom,
            forceSwitch,
            3000
        );

        if (targetSequence == null || targetSequence.Count == 0)
        {
            Debug.LogError($"BridgeGameManager: Failed to generate path day={day}");
            return;
        }

        hud?.SetupTrial(goalPieces);
        hud?.SetCollectedFound(0);

        ResetActivePiecesToIdle();
        SetInputEnabled(false);

        StartCoroutine(PresentThenConstruct());
    }

    private void ApplyActiveSpan(int spanRows)
    {
        foreach (var kv in piecesById)
        {
            int id = kv.Key;
            var piece = kv.Value;
            if (!piece) continue;

            int r = id / cols;
            piece.gameObject.SetActive(r >= 0 && r < spanRows);
        }
    }

    private void LayoutActiveSpanConnectingChasms(int spanRows)
    {
        if (!playArea) return;

        float leftX = -(columnGap * 0.5f);
        float rightX = (columnGap * 0.5f);

        float topY, bottomY;

        if (topChasmAnchor && bottomChasmAnchor)
        {
            Vector2 localTop, localBottom;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                playArea,
                RectTransformUtility.WorldToScreenPoint(null, topChasmAnchor.position),
                null,
                out localTop
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                playArea,
                RectTransformUtility.WorldToScreenPoint(null, bottomChasmAnchor.position),
                null,
                out localBottom
            );

            topY = localTop.y;
            bottomY = localBottom.y;

            if (topY < bottomY) (topY, bottomY) = (bottomY, topY);
        }
        else
        {
            var pr = playArea.rect;
            topY = pr.height * 0.5f - 140f;
            bottomY = -pr.height * 0.5f + 140f;
        }

        float spanH = Mathf.Max(10f, topY - bottomY);
        float rowStep = (spanRows <= 1) ? 0f : (spanH / (spanRows - 1));

        for (int id = 0; id < totalRows * cols; id++)
        {
            if (!piecesById.TryGetValue(id, out var piece) || !piece) continue;
            if (!piece.gameObject.activeInHierarchy) continue;

            int r = id / cols;
            int c = id % cols;
            if (r < 0 || r >= spanRows) continue;

            RectTransform rt = piece.GetComponent<RectTransform>();
            if (!rt) continue;

            if (rt.parent != playArea)
                rt.SetParent(playArea, false);

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float y = topY - (r * rowStep);
            float x = (c == 0) ? leftX : rightX;
            x += ((r % 2) == 0) ? -staggerX : staggerX;

            rt.anchoredPosition = new Vector2(x, y);
        }
    }

    private IEnumerator PresentThenConstruct()
    {
        foreach (var id in targetSequence)
        {
            if (!piecesById.TryGetValue(id, out var piece) || !piece) continue;
            if (!piece.gameObject.activeInHierarchy) continue;

            piece.SetState(BridgePieceState.Highlighted);
            yield return new WaitForSeconds(dayCfg.displayMs / 1000f);
            piece.SetState(BridgePieceState.Idle);
            yield return new WaitForSeconds(dayCfg.gapMs / 1000f);
        }

        trialStartTime = Time.time;
        inputEnabled = true;
        SetInputEnabled(true);
    }

    private void OnPiecePressed(BridgePieceUI piece)
    {
        if (!inputEnabled) return;
        if (trialComplete || !piece) return;
        if (!piece.gameObject.activeInHierarchy) return;
        if (builtCount < 0 || builtCount >= targetSequence.Count) return;

        int expectedId = targetSequence[builtCount];

        if (piece.Id == expectedId)
        {
            builtCount++;
            piece.SetState(BridgePieceState.Built);
            hud?.SetCollectedFound(builtCount);
            AudioManager.Instance.Play("StoneCorrectStep");
            if (builtCount >= goalPieces)
                CompleteTrial();
        }
        else
        {
            wrongAttempts++;
            piece.FlashError();
            hud?.AddErrorAndWarn();
            AudioManager.Instance.Play("StepStoneWrong");
        }
    }

    private void CompleteTrial()
    {
        trialComplete = true;
        inputEnabled = false;
        SetInputEnabled(false);

        int completionMs = Mathf.RoundToInt((Time.time - trialStartTime) * 1000f);

        sessionData.Add(new TrialRecord
        {
            minigame_id = "BRIDGE",
            day = day,
            trial_index = trialIndex + 1,
            span = goalPieces,
            target_sequence = new List<int>(targetSequence),
            wrong_attempts = wrongAttempts,
            completion_time_ms = completionMs,
            timestamp_iso = DateTime.UtcNow.ToString("o")
        });

        trialIndex++;
        hud?.SetTrialsDone(trialIndex);

        if (trialIndex >= dayCfg.trials)
        {
            var data = PlayerDataManager.Instance.Data;

            if (!data.bridgeCompletedToday)
            {
                data.bridgeCompletedToday = true;
                data.miniGamesCompletedToday += 1;
                PlayerDataManager.Instance.Save();
            }

            hud?.ShowDayComplete();
        }
        else
        {
            hud?.ShowTrialComplete();
        }
    }

    private void ResetActivePiecesToIdle()
    {
        foreach (var kv in piecesById)
        {
            var p = kv.Value;
            if (!p) continue;
            if (!p.gameObject.activeInHierarchy) continue;
            p.SetState(BridgePieceState.Idle);
        }
    }

    private void SetInputEnabled(bool enabled)
    {
        foreach (var kv in piecesById)
        {
            var p = kv.Value;
            if (!p) continue;
            if (!p.gameObject.activeInHierarchy) continue;
            p.SetInteractable(enabled);
        }
    }
}