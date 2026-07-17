using System;
using System.Collections;
using System.Collections.Generic;
using RTLTMPro;
using UnityEngine;

public class BridgeGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BridgeHUD hud;
    [SerializeField] private BridgeConfig config;
    [SerializeField] private SessionDataSO sessionData;
    [SerializeField] private FeedbackMessanger feedbackMessanger;
    [SerializeField] private IntroductionManager introductionManager;
    [SerializeField] private BridgeTutorial bridgeTutorial;

    [Header("Countdown")]
    [SerializeField] private RTLTextMeshPro countdownText;

    [Header("UI Layout (Anchors)")]
    [SerializeField] private RectTransform playArea;
    [SerializeField] private RectTransform topChasmAnchor;
    [SerializeField] private RectTransform bottomChasmAnchor;

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

    [Header("Flow")]
    [SerializeField, Min(0f)] private float autoNextTrialDelay = 0.9f;
    [SerializeField, Min(1)] private int assistedFailLimit = 3;

    private const string SelectedLevelKey = "BridgeSelectedLevel";

    private Coroutine autoNextTrialRoutine;
    private Coroutine presentThenConstructRoutine;

    private bool gameplayBootstrapped;
    private bool bootstrapRequested;
    private bool introStepCompleted;
    private bool tutorialStepCompleted;
    private bool tutorialOpened;

    private int currentLevel;
    private int levelStartedAt;
    private BridgeConfig.LevelConfig levelCfg;

    private int trialsCompleteInLevel;
    private int consecutiveFailsOnLevel;
    private int trialIndexInLevel;

    private readonly Dictionary<int, BridgePieceUI> piecesById = new();

    private int activeRows;
    private int goalPieces;
    private List<int> targetSequence = new();

    private float levelStartTime;
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

        foreach (BridgePieceUI piece in pieces)
        {
            if (!piece)
                continue;

            if (piecesById.ContainsKey(piece.Id))
            {
                Debug.LogError($"BridgeGameManager: Duplicate Id={piece.Id} on '{piece.name}'.");
                continue;
            }

            piecesById.Add(piece.Id, piece);

            int row = piece.Id / cols;
            int column = piece.Id % cols;
            piece.SetGrid(row, column);

            piece.Clicked -= OnPiecePressed;
            piece.Clicked += OnPiecePressed;
        }

        bool isStartingLevelOne = GetSelectedStartingLevel() == 1;

        if (introductionManager != null)
        {
            introductionManager.SetAllowedForCurrentLevel(isStartingLevelOne);
            introductionManager.IntroductionCompleted -= HandleIntroductionCompleted;
            introductionManager.IntroductionCompleted += HandleIntroductionCompleted;

            introStepCompleted = !isStartingLevelOne;

            if (!isStartingLevelOne)
                introductionManager.enabled = false;
        }
        else
        {
            introStepCompleted = true;
        }

        if (isStartingLevelOne && bridgeTutorial != null)
        {
            tutorialStepCompleted = false;
            tutorialOpened = false;

            bridgeTutorial.TutorialCompleted -= HandleTutorialCompleted;
            bridgeTutorial.TutorialCompleted += HandleTutorialCompleted;

            // It is enabled only after the introduction completes.
            bridgeTutorial.enabled = false;
        }
        else
        {
            tutorialStepCompleted = true;
            tutorialOpened = false;

            if (bridgeTutorial != null)
                bridgeTutorial.enabled = false;
        }

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    private void Start()
    {
        AudioManager.Instance.StopAll();
        AudioManager.Instance.Play("BridgeAmbient");

        RequestBootstrapGameFlow();
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<int, BridgePieceUI> pair in piecesById)
        {
            if (pair.Value != null)
                pair.Value.Clicked -= OnPiecePressed;
        }

        if (introductionManager != null)
            introductionManager.IntroductionCompleted -= HandleIntroductionCompleted;

        if (bridgeTutorial != null)
            bridgeTutorial.TutorialCompleted -= HandleTutorialCompleted;
    }

    private void RequestBootstrapGameFlow()
    {
        bootstrapRequested = true;
        EvaluateOnboardingGate();
    }

    private void EvaluateOnboardingGate()
    {
        if (!bootstrapRequested || gameplayBootstrapped)
            return;

        // Level 1: Introduction -> Tutorial -> Countdown.
        if (!introStepCompleted || !tutorialStepCompleted)
            return;

        // Level 2+: both are already complete, so countdown begins.
        StartGameplayIfNeeded();
    }

    private void HandleIntroductionCompleted()
    {
        if (gameplayBootstrapped || introStepCompleted)
            return;

        introStepCompleted = true;

        if (!tutorialStepCompleted && bridgeTutorial != null)
        {
            bridgeTutorial.enabled = true;
            StartCoroutine(OpenTutorialAfterIntroduction());
            return;
        }

        EvaluateOnboardingGate();
    }

    private IEnumerator OpenTutorialAfterIntroduction()
    {
        // Allows BridgeTutorial OnEnable/Start initialization to complete.
        yield return null;

        if (gameplayBootstrapped ||
            tutorialStepCompleted ||
            tutorialOpened ||
            bridgeTutorial == null)
        {
            yield break;
        }

        tutorialOpened = true;
        bridgeTutorial.OpenTutorial();
    }

    private void HandleTutorialCompleted()
    {
        if (!tutorialOpened)
            return;

        tutorialOpened = false;

        // Only the automatic level-1 tutorial releases the countdown.
        if (!tutorialStepCompleted)
        {
            tutorialStepCompleted = true;
            EvaluateOnboardingGate();
        }
    }

    // Optional target for the tutorial's final button.
    public void OnTutorialCompleted()
    {
        HandleTutorialCompleted();
    }

    // Assign this to the in-game "How To Play" button.
    public void ShowHowToPlay()
    {
        if (bridgeTutorial == null || tutorialOpened)
            return;

        tutorialOpened = true;
        bridgeTutorial.enabled = true;
        StartCoroutine(OpenTutorialManually());
    }

    private IEnumerator OpenTutorialManually()
    {
        yield return null;

        if (bridgeTutorial != null)
            bridgeTutorial.OpenTutorial();
    }

    private void StartGameplayIfNeeded()
    {
        if (gameplayBootstrapped)
            return;

        gameplayBootstrapped = true;
        StartCoroutine(BeginGameAfterCountdown());
    }

    private IEnumerator BeginGameAfterCountdown()
    {
        var data = PlayerDataManager.Instance.Data;

        int unlockedLevel = Mathf.Clamp(
            data.bridgeLevel,
            1,
            ProgressionManager.MAX_LEVEL
        );

        int requestedLevel = PlayerPrefs.GetInt(SelectedLevelKey, unlockedLevel);
        int startLevel = Mathf.Clamp(requestedLevel, 1, unlockedLevel);

        PlayerPrefs.DeleteKey(SelectedLevelKey);

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

        StartLevel(startLevel);
    }

    public void StartLevel(int levelNumber)
    {
        if (!config || !sessionData)
        {
            Debug.LogError("[BridgeGameManager] Missing config or sessionData.");
            return;
        }

        currentLevel = Mathf.Clamp(levelNumber, 1, ProgressionManager.MAX_LEVEL);
        levelCfg = config.GetLevel(currentLevel);

        if (levelCfg.levelNumber == 0)
        {
            Debug.LogError($"[BridgeGameManager] Missing LevelConfig for level {currentLevel}");
            return;
        }

        levelStartedAt = currentLevel;
        levelStartTime = Time.time;
        trialsCompleteInLevel = 0;
        consecutiveFailsOnLevel = 0;
        trialIndexInLevel = 0;

        hud?.SetupDay(levelCfg.trials);
        hud?.SetTrialsDone(0);

        StartNextTrial();
    }

    public void StartNextTrial()
    {
        if (trialsCompleteInLevel >= levelCfg.trials)
        {
            CompleteLevelAfterTrials(false);
            return;
        }

        StopRunningTrialRoutines();

        trialComplete = false;
        inputEnabled = false;
        builtCount = 0;
        wrongAttempts = 0;

        activeRows = Mathf.Clamp(levelCfg.minPieces, 2, totalRows);

        if (levelCfg.pattern == BridgeConfig.BridgePattern.ZigZag)
        {
            goalPieces = Mathf.Clamp(levelCfg.maxPieces, activeRows + 1, activeRows * 2);
        }
        else
        {
            int minLength = Mathf.Max(levelCfg.minPieces, activeRows);
            int maxLength = Mathf.Min(levelCfg.maxPieces, activeRows * 2);
            goalPieces = UnityEngine.Random.Range(minLength, maxLength + 1);
        }

        ApplyActiveSpan(activeRows);
        LayoutActiveSpanConnectingChasms(activeRows);

        bool startFromBottom = randomizeBottomToTop &&
            UnityEngine.Random.value < bottomToTopChance;

        bool forceSwitch = levelCfg.pattern == BridgeConfig.BridgePattern.ZigZag;

        targetSequence = BridgePathGenerator.Generate2ColPath(
            activeRows,
            goalPieces,
            startFromBottom,
            forceSwitch,
            3000
        );

        if (targetSequence == null || targetSequence.Count == 0)
        {
            Debug.LogError($"[Bridge] Failed to generate path for level {currentLevel}");
            return;
        }

        hud?.SetupTrial();
        ResetActivePiecesToIdle();
        SetInputEnabled(false);

        presentThenConstructRoutine = StartCoroutine(PresentThenConstruct());
    }

    public void ReplaySameTrial()
    {
        StopRunningTrialRoutines();

        trialComplete = false;
        inputEnabled = false;
        builtCount = 0;
        wrongAttempts = 0;

        hud?.SetupTrial();
        ResetActivePiecesToIdle();
        SetInputEnabled(false);

        presentThenConstructRoutine = StartCoroutine(PresentThenConstruct());
    }

    private void StopRunningTrialRoutines()
    {
        if (autoNextTrialRoutine != null)
        {
            StopCoroutine(autoNextTrialRoutine);
            autoNextTrialRoutine = null;
        }

        if (presentThenConstructRoutine != null)
        {
            StopCoroutine(presentThenConstructRoutine);
            presentThenConstructRoutine = null;
        }
    }

    private void ApplyActiveSpan(int spanRows)
    {
        foreach (KeyValuePair<int, BridgePieceUI> pair in piecesById)
        {
            BridgePieceUI piece = pair.Value;

            if (!piece)
                continue;

            int row = pair.Key / cols;
            piece.gameObject.SetActive(row >= 0 && row < spanRows);
        }
    }

    private void LayoutActiveSpanConnectingChasms(int spanRows)
    {
        if (!playArea)
            return;

        float leftX = -columnGap * 0.5f;
        float rightX = columnGap * 0.5f;
        float topY;
        float bottomY;

        if (topChasmAnchor && bottomChasmAnchor)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                playArea,
                RectTransformUtility.WorldToScreenPoint(null, topChasmAnchor.position),
                null,
                out Vector2 localTop
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                playArea,
                RectTransformUtility.WorldToScreenPoint(null, bottomChasmAnchor.position),
                null,
                out Vector2 localBottom
            );

            topY = localTop.y;
            bottomY = localBottom.y;

            if (topY < bottomY)
                (topY, bottomY) = (bottomY, topY);
        }
        else
        {
            Rect rect = playArea.rect;
            topY = rect.height * 0.5f - 140f;
            bottomY = -rect.height * 0.5f + 140f;
        }

        float spanHeight = Mathf.Max(10f, topY - bottomY);
        float rowStep = spanRows <= 1 ? 0f : spanHeight / (spanRows - 1);

        for (int id = 0; id < totalRows * cols; id++)
        {
            if (!piecesById.TryGetValue(id, out BridgePieceUI piece) ||
                !piece ||
                !piece.gameObject.activeInHierarchy)
            {
                continue;
            }

            int row = id / cols;
            int column = id % cols;

            if (row < 0 || row >= spanRows)
                continue;

            RectTransform pieceRect = piece.GetComponent<RectTransform>();

            if (!pieceRect)
                continue;

            if (pieceRect.parent != playArea)
                pieceRect.SetParent(playArea, false);

            pieceRect.anchorMin = new Vector2(0.5f, 0.5f);
            pieceRect.anchorMax = new Vector2(0.5f, 0.5f);
            pieceRect.pivot = new Vector2(0.5f, 0.5f);

            float y = topY - row * rowStep;
            float x = column == 0 ? leftX : rightX;
            x += row % 2 == 0 ? -staggerX : staggerX;

            pieceRect.anchoredPosition = new Vector2(x, y);
        }
    }

    private IEnumerator PresentThenConstruct()
    {
        foreach (int id in targetSequence)
        {
            if (!piecesById.TryGetValue(id, out BridgePieceUI piece) ||
                !piece ||
                !piece.gameObject.activeInHierarchy)
            {
                continue;
            }

            piece.SetState(BridgePieceState.Highlighted);
            yield return new WaitForSeconds(levelCfg.displayMs / 1000f);

            piece.SetState(BridgePieceState.Idle);
            yield return new WaitForSeconds(levelCfg.gapMs / 1000f);
        }

        trialStartTime = Time.time;
        inputEnabled = true;
        SetInputEnabled(true);
    }

    private void OnPiecePressed(BridgePieceUI piece)
    {
        if (!inputEnabled ||
            trialComplete ||
            !piece ||
            !piece.gameObject.activeInHierarchy ||
            builtCount < 0 ||
            builtCount >= targetSequence.Count)
        {
            return;
        }

        int expectedId = targetSequence[builtCount];

        if (piece.Id == expectedId)
        {
            builtCount++;
            piece.SetState(BridgePieceState.Built);
            AudioManager.Instance.Play("StoneCorrectStep");

            if (builtCount >= goalPieces)
                CompleteTrial(false);

            return;
        }

        wrongAttempts++;
        consecutiveFailsOnLevel++;

        piece.FlashError();
        AudioManager.Instance.Play("StepStoneWrong");

        var wrong = config.GetRandomWrongPattern(levelCfg);
        feedbackMessanger?.ShowWrongPattern(wrong.title, wrong.message);

        if (wrongAttempts >= assistedFailLimit)
        {
            trialsCompleteInLevel = levelCfg.trials;
            hud?.SetTrialsDone(trialsCompleteInLevel);
            CompleteLevelAfterTrials(true);
            return;
        }

        StartCoroutine(RestartSameTrialAfterWrong());
    }

    private IEnumerator RestartSameTrialAfterWrong()
    {
        inputEnabled = false;
        SetInputEnabled(false);

        yield return new WaitForSeconds(0.25f);

        if (presentThenConstructRoutine != null)
            StopCoroutine(presentThenConstructRoutine);

        trialComplete = false;
        builtCount = 0;

        ResetActivePiecesToIdle();
        hud?.SetupTrial();

        presentThenConstructRoutine = StartCoroutine(PresentThenConstruct());
    }

    private void CompleteTrial(bool assisted)
    {
        if (trialComplete)
            return;

        trialComplete = true;
        inputEnabled = false;
        SetInputEnabled(false);

        int completionMs = Mathf.RoundToInt((Time.time - trialStartTime) * 1000f);

        var result = ProgressionManager.Instance.EvaluateTrial(
            "Bridge",
            !assisted,
            wrongAttempts,
            completionMs,
            goalPieces,
            consecutiveFailsOnLevel
        );

        RecordTrial("Bridge", result, !assisted, wrongAttempts, completionMs);

        trialsCompleteInLevel++;
        hud?.SetTrialsDone(trialsCompleteInLevel);

        if (trialsCompleteInLevel >= levelCfg.trials)
        {
            CompleteLevelAfterTrials(assisted);
            return;
        }

        if (!assisted)
        {
            var success = config.GetRandomTrialSuccess(levelCfg);
            feedbackMessanger?.ShowSuccess(config.GetSuccessTitle(levelCfg), success.message);
        }

        consecutiveFailsOnLevel = 0;
        autoNextTrialRoutine = StartCoroutine(AutoNextTrialRoutine());
    }

    private IEnumerator AutoNextTrialRoutine()
    {
        yield return new WaitForSeconds(autoNextTrialDelay);
        StartNextTrial();
    }

    private void CompleteLevelAfterTrials(bool assistedLevelCompletion)
    {
        int completionMs = Mathf.RoundToInt((Time.time - levelStartTime) * 1000f);
        float averageSpan = (levelCfg.minPieces + levelCfg.maxPieces) * 0.5f;

        var result = ProgressionManager.Instance.EvaluateTrial(
            "Bridge",
            !assistedLevelCompletion,
            assistedLevelCompletion ? assistedFailLimit : consecutiveFailsOnLevel,
            completionMs,
            Mathf.RoundToInt(averageSpan),
            assistedLevelCompletion ? assistedFailLimit : 0
        );

        var data = PlayerDataManager.Instance.Data;
        bool completedCurrentUnlockedLevel = levelStartedAt == data.bridgeLevel;

        if (completedCurrentUnlockedLevel)
        {
            ProgressionManager.Instance.CompleteLevel("Bridge", result);

            int nextLevel = Mathf.Min(
                levelStartedAt + 1,
                ProgressionManager.MAX_LEVEL
            );

            data.bridgeLevel = Mathf.Max(data.bridgeLevel, nextLevel);

            if (!assistedLevelCompletion &&
                config.IsGatewayLevel(levelCfg))
            {
                data.bridgeGateReached = true;
                data.swmUnlocked = true;
            }

            PlayerDataManager.Instance.Save();
        }

        feedbackMessanger?.ShowOutcomePanel(
            string.Empty,
            assistedLevelCompletion
                ? config.GetRandomAssistedPassMessage()
                : config.GetFinalSuccessMessage(levelCfg),
            assistedLevelCompletion,
            completedCurrentUnlockedLevel &&
            !assistedLevelCompletion &&
            config.IsGatewayLevel(levelCfg)
        );

        hud?.ShowDayComplete();
    }

    private void RecordTrial(
        string minigameId,
        ProgressionManager.LevelResult result,
        bool isCorrect,
        int attempts,
        int completionMs)
    {
        sessionData.Add(new TrialRecord
        {
            minigame_id = minigameId,
            day = currentLevel,
            level_number = currentLevel,
            trial_index = trialIndexInLevel + 1,
            span = goalPieces,
            target_sequence = new List<int>(targetSequence),
            sequence_recalled = new List<int>(),
            is_correct = isCorrect,
            wrong_attempts = attempts,
            completion_time_ms = completionMs,
            level_score = result.score,
            stars = result.stars,
            passed = result.passed,
            strong_pass = result.strongPass,
            assisted_pass = result.assistedPass,
            consecutive_fails = consecutiveFailsOnLevel,
            timestamp_iso = DateTime.UtcNow.ToString("o")
        });

        trialIndexInLevel++;
    }

    private void ResetActivePiecesToIdle()
    {
        foreach (KeyValuePair<int, BridgePieceUI> pair in piecesById)
        {
            BridgePieceUI piece = pair.Value;

            if (piece && piece.gameObject.activeInHierarchy)
                piece.SetState(BridgePieceState.Idle);
        }
    }

    private void SetInputEnabled(bool enabled)
    {
        foreach (KeyValuePair<int, BridgePieceUI> pair in piecesById)
        {
            BridgePieceUI piece = pair.Value;

            if (piece && piece.gameObject.activeInHierarchy)
                piece.SetInteractable(enabled);
        }
    }

    private int GetSelectedStartingLevel()
    {
        var data = PlayerDataManager.Instance.Data;

        int unlockedLevel = Mathf.Clamp(
            data.bridgeLevel,
            1,
            ProgressionManager.MAX_LEVEL
        );

        int requestedLevel = PlayerPrefs.GetInt(SelectedLevelKey, unlockedLevel);
        return Mathf.Clamp(requestedLevel, 1, unlockedLevel);
    }
}
