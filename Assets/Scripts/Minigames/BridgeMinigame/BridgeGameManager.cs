// filepath: c:\Users\USER\Desktop\Mahoor\PandoraUnity\Pandora\Assets\Scripts\Minigames\BridgeMinigame\BridgeGameManager.cs
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

    [Header("Pieces")]
    [Tooltip("Assign all stones here. Each stone needs a unique ID, starting at 0.")]
    [SerializeField] private List<BridgePieceUI> pieces = new();

    [Header("Logical Board")]
    [Tooltip("Each group contains this many stones.")]
    [SerializeField, Range(2, 3)] private int cols = 3;

    [Tooltip("Number of stone groups on the bridge.")]
    [SerializeField, Range(3, 4)] private int totalRows = 4;

    [Header("Direction")]
    [SerializeField] private bool randomizeBottomToTop = true;
    [SerializeField, Range(0f, 1f)] private float bottomToTopChance = 0.5f;

    [Tooltip("Chance that the next step stays in the current group of three stones.")]
    [SerializeField, Range(0f, 1f)] private float repeatSameGroupChance = 0.4f;

    [Header("Flow")]
    [SerializeField, Min(0f)] private float autoNextTrialDelay = 0.9f;
    [SerializeField, Min(1)] private int assistedFailLimit = 3;

    [Header("Runtime")]
    [SerializeField] private int currentLevel;

    private const string SelectedLevelKey = "BridgeSelectedLevel";

    private Coroutine autoNextTrialRoutine;
    private Coroutine presentThenConstructRoutine;

    private bool gameplayBootstrapped;
    private bool bootstrapRequested;
    private bool introStepCompleted;
    private bool tutorialStepCompleted;
    private bool tutorialOpened;

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
            pieces = new List<BridgePieceUI>(
                GetComponentsInChildren<BridgePieceUI>(true)
            );

        piecesById.Clear();

        foreach (BridgePieceUI piece in pieces)
        {
            if (!piece)
                continue;

            if (piecesById.ContainsKey(piece.Id))
            {
                Debug.LogError(
                    $"[BridgeGameManager] Duplicate BridgePieceUI ID {piece.Id} on '{piece.name}'."
                );
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

        if (!introStepCompleted || !tutorialStepCompleted)
            return;

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

        if (!tutorialStepCompleted)
        {
            tutorialStepCompleted = true;
            EvaluateOnboardingGate();
        }
    }

    public void OnTutorialCompleted()
    {
        HandleTutorialCompleted();
    }

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

        int requestedLevel = PlayerPrefs.GetInt(
            SelectedLevelKey,
            unlockedLevel
        );

        int startLevel = Mathf.Clamp(
            requestedLevel,
            1,
            unlockedLevel
        );

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

        currentLevel = Mathf.Clamp(
            levelNumber,
            1,
            ProgressionManager.MAX_LEVEL
        );

        levelCfg = config.GetLevel(currentLevel);

        if (levelCfg.levelNumber == 0)
        {
            Debug.LogError(
                $"[BridgeGameManager] Missing LevelConfig for level {currentLevel}."
            );
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

        activeRows = Mathf.Clamp(totalRows, 3, 4);

        int minimumSteps = Mathf.Max(1, levelCfg.minPieces);
        int maximumSteps = Mathf.Max(minimumSteps, levelCfg.maxPieces);

        goalPieces = UnityEngine.Random.Range(
            minimumSteps,
            maximumSteps + 1
        );

        bool startFromBottom =
            randomizeBottomToTop &&
            UnityEngine.Random.value < bottomToTopChance;

        targetSequence = GenerateGroupedSequence(
            activeRows,
            goalPieces,
            startFromBottom
        );

        if (targetSequence.Count != goalPieces)
        {
            Debug.LogError(
                $"[BridgeGameManager] Could not create a {goalPieces}-step sequence."
            );
            return;
        }

        ResetAllPiecesToIdle();
        SetInputEnabled(false);

        hud?.SetupTrial();
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
        ResetAllPiecesToIdle();
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

    private IEnumerator PresentThenConstruct()
    {
        foreach (int id in targetSequence)
        {
            if (!piecesById.TryGetValue(id, out BridgePieceUI piece) || !piece)
                continue;

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

        ResetAllPiecesToIdle();
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

        int completionMs = Mathf.RoundToInt(
            (Time.time - trialStartTime) * 1000f
        );

        var result = ProgressionManager.Instance.EvaluateTrial(
            "Bridge",
            !assisted,
            wrongAttempts,
            completionMs,
            goalPieces,
            consecutiveFailsOnLevel
        );

        RecordTrial(
            "Bridge",
            result,
            !assisted,
            wrongAttempts,
            completionMs
        );

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

            feedbackMessanger?.ShowSuccess(
                config.GetSuccessTitle(levelCfg),
                success.message
            );
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
        int completionMs = Mathf.RoundToInt(
            (Time.time - levelStartTime) * 1000f
        );

        float averageSpan =
            (levelCfg.minPieces + levelCfg.maxPieces) * 0.5f;

        var result = ProgressionManager.Instance.EvaluateTrial(
            "Bridge",
            !assistedLevelCompletion,
            assistedLevelCompletion
                ? assistedFailLimit
                : consecutiveFailsOnLevel,
            completionMs,
            Mathf.RoundToInt(averageSpan),
            assistedLevelCompletion
                ? assistedFailLimit
                : 0
        );

        var data = PlayerDataManager.Instance.Data;

        bool completedCurrentUnlockedLevel =
            levelStartedAt == data.bridgeLevel;

        bool passedBridgeGateway =
            completedCurrentUnlockedLevel &&
            !assistedLevelCompletion &&
            config.IsGatewayLevel(levelCfg);

        if (completedCurrentUnlockedLevel)
        {
            ProgressionManager.Instance.CompleteLevel("Bridge", result);

            int nextLevel = Mathf.Min(
                levelStartedAt + 1,
                ProgressionManager.MAX_LEVEL
            );

            data.bridgeLevel = Mathf.Max(data.bridgeLevel, nextLevel);

            if (passedBridgeGateway)
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
            passedBridgeGateway
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

    private void ResetAllPiecesToIdle()
    {
        foreach (KeyValuePair<int, BridgePieceUI> pair in piecesById)
        {
            if (pair.Value != null)
                pair.Value.SetState(BridgePieceState.Idle);
        }
    }

    private void SetInputEnabled(bool enabled)
    {
        foreach (KeyValuePair<int, BridgePieceUI> pair in piecesById)
        {
            if (pair.Value != null)
                pair.Value.SetInteractable(enabled);
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

        int requestedLevel = PlayerPrefs.GetInt(
            SelectedLevelKey,
            unlockedLevel
        );

        return Mathf.Clamp(requestedLevel, 1, unlockedLevel);
    }

    private int GetRow(int id)
    {
        return id / cols;
    }

    private List<int> GenerateGroupedSequence(
        int rowCount,
        int stepCount,
        bool startFromBottom)
    {
        List<int> sequence = new();

        if (rowCount <= 0 || stepCount <= 0)
            return sequence;

        // Build groups in gameplay order:
        // top -> bottom, or bottom -> top.
        List<List<int>> orderedGroups = new();

        for (int orderIndex = 0; orderIndex < rowCount; orderIndex++)
        {
            int row = startFromBottom
                ? rowCount - 1 - orderIndex
                : orderIndex;

            List<int> group = new();

            foreach (KeyValuePair<int, BridgePieceUI> pair in piecesById)
            {
                if (pair.Value != null && GetRow(pair.Key) == row)
                    group.Add(pair.Key);
            }

            if (group.Count == 0)
            {
                Debug.LogError(
                    $"[BridgeGameManager] Group/row {row} has no assigned stones."
                );
                return sequence;
            }

            orderedGroups.Add(group);
        }

        int totalAvailableStones = 0;

        for (int i = 0; i < orderedGroups.Count; i++)
            totalAvailableStones += orderedGroups[i].Count;

        // To travel through all groups in order, every group needs one stone.
        stepCount = Mathf.Max(stepCount, rowCount);

        // A stone may never be used twice in the same trial.
        stepCount = Mathf.Min(stepCount, totalAvailableStones);

        // Start with one stone from every group:
        // Group 1 -> Group 2 -> Group 3 -> Group 4.
        int[] selectionsPerGroup = new int[rowCount];

        for (int i = 0; i < rowCount; i++)
            selectionsPerGroup[i] = 1;

        int remainingSteps = stepCount - rowCount;

        // Add remaining steps to random groups which still have unused stones.
        // This allows: group 1 -> group 1 -> group 2 -> group 3 -> group 3 -> group 4.
        while (remainingSteps > 0)
        {
            List<int> groupsWithCapacity = new();

            for (int groupIndex = 0; groupIndex < orderedGroups.Count; groupIndex++)
            {
                if (selectionsPerGroup[groupIndex] < orderedGroups[groupIndex].Count)
                    groupsWithCapacity.Add(groupIndex);
            }

            if (groupsWithCapacity.Count == 0)
                break;

            int selectedGroupIndex = groupsWithCapacity[
                UnityEngine.Random.Range(0, groupsWithCapacity.Count)
            ];

            selectionsPerGroup[selectedGroupIndex]++;
            remainingSteps--;
        }

        // Pick unique stones from each group. Groups are processed in order,
        // so it can never jump from group 1 directly to group 3.
        for (int groupIndex = 0; groupIndex < orderedGroups.Count; groupIndex++)
        {
            List<int> availableStones = new List<int>(
                orderedGroups[groupIndex]
            );

            int stonesToPick = selectionsPerGroup[groupIndex];

            for (int pickIndex = 0; pickIndex < stonesToPick; pickIndex++)
            {
                int randomIndex = UnityEngine.Random.Range(
                    0,
                    availableStones.Count
                );

                int selectedStone = availableStones[randomIndex];

                sequence.Add(selectedStone);

                // Remove it so this stone cannot appear twice in this trial.
                availableStones.RemoveAt(randomIndex);
            }
        }

        return sequence;
    }
}


