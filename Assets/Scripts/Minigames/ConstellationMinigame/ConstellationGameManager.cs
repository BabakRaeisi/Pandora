using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConstellationGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ConstellationController controller;
    [SerializeField] private ConstellationHUD hud;
    [SerializeField] private ConstellationConfigSO config;
    [SerializeField] private ConstellationTimingProfile timingProfile;
    [SerializeField] private FeedbackMessanger feedbackMessanger;
    [SerializeField] private IntroductionManager introductionManager;
    [SerializeField] private ConstellationTutorial constellationTutorial;

    [Header("Session Data")]
    [SerializeField] private SessionDataSO sessionData;

    [Header("Countdown")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Flow")]
    [SerializeField, Min(0f)] private float autoNextTrialDelay = 0.9f;

    private const string SelectedLevelKey = "ConstellationSelectedLevel";

    private Coroutine autoNextTrialRoutine;

    private int currentLevel;
    private int levelStartedAt;
    private ConstellationConfigSO.LevelConfig levelCfg;

    private int trialIndexInLevel;
    private int trialsCompleteInLevel;
    private int consecutiveFailsOnLevel;
    private int wrongAttempts;
private int successfulTrialsInLevel;
private ActiveLevelTimer activeLevelTimer = new();
    private bool busy;
    private bool gameplayBootstrapped;
    private bool bootstrapRequested;
    private bool introStepCompleted;
    private bool tutorialStepCompleted;
    private bool tutorialOpened;

    private int[] currentSequence;
    private List<int> playerSequence;
    private HashSet<int> visibleSet;

    private float levelStartTime;
    private float trialStartTime;

    private float CurrentStarOnSeconds =>
        timingProfile ? timingProfile.StarDisplaySeconds : 1f;

    private float CurrentGapSeconds =>
        timingProfile ? timingProfile.GapSeconds : 0.25f;

    private void Awake()
    {
        bool isStartingLevelOne = GetSelectedStartingLevel() == 1;

        // Introduction is allowed ONLY when level 1 was selected.
        if (introductionManager != null)
        {
            introductionManager.SetAllowedForCurrentLevel(isStartingLevelOne);
            introductionManager.IntroductionCompleted -= HandleIntroductionCompleted;
            introductionManager.IntroductionCompleted += HandleIntroductionCompleted;

            if (isStartingLevelOne)
            {
                introStepCompleted = false;
            }
            else
            {
                introStepCompleted = true;
                introductionManager.enabled = false;
            }
        }
        else
        {
            introStepCompleted = true;
        }

        // Tutorial is required ONLY for level 1.
        if (isStartingLevelOne && constellationTutorial != null)
        {
            tutorialStepCompleted = false;
            tutorialOpened = false;

            constellationTutorial.TutorialCompleted -= HandleTutorialCompleted;
            constellationTutorial.TutorialCompleted += HandleTutorialCompleted;

            // Tutorial cannot start before introduction completion.
            constellationTutorial.enabled = false;
        }
        else
        {
            tutorialStepCompleted = true;
            tutorialOpened = false;

            if (constellationTutorial != null)
                constellationTutorial.enabled = false;
        }

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (controller != null)
            controller.OnTrialFinished += HandleTrialFinished;

        AudioManager.Instance.StopAll();
        AudioManager.Instance.Play("SpaceAmbientSound");

        RequestBootstrapGameFlow();
    }

    private int GetSelectedStartingLevel()
    {
        var data = PlayerDataManager.Instance.Data;

        int unlockedLevel = Mathf.Clamp(
            data.constellationLevel,
            1,
            ProgressionManager.MAX_LEVEL
        );

        int requestedLevel = PlayerPrefs.GetInt(SelectedLevelKey, unlockedLevel);
        return Mathf.Clamp(requestedLevel, 1, unlockedLevel);
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

        // Level 1 waits for the intro's final slide.
        if (!introStepCompleted)
            return;

        // Level 1 then waits for tutorial completion.
        if (!tutorialStepCompleted)
            return;

        // Level 2+ arrives here immediately.
        StartGameplayIfNeeded();
    }

    private void HandleIntroductionCompleted()
    {
        if (gameplayBootstrapped || introStepCompleted)
            return;

        introStepCompleted = true;

        // Level 1: enable and open the tutorial only after intro completion.
        if (!tutorialStepCompleted && constellationTutorial != null)
        {
            constellationTutorial.enabled = true;
            StartCoroutine(OpenTutorialAfterIntroduction());
            return;
        }

        EvaluateOnboardingGate();
    }

    private IEnumerator OpenTutorialAfterIntroduction()
    {
        // Allow the tutorial's OnEnable / Start code to initialize first.
        yield return null;

        if (gameplayBootstrapped ||
            tutorialStepCompleted ||
            tutorialOpened ||
            constellationTutorial == null)
        {
            yield break;
        }

        tutorialOpened = true;
        constellationTutorial.OpenTutorial();
    }

    private void HandleTutorialCompleted()
    {
        if (gameplayBootstrapped || tutorialStepCompleted)
            return;

        tutorialStepCompleted = true;
        tutorialOpened = false;

        EvaluateOnboardingGate();
    }

    // Optional Unity Button event target for a tutorial completion button.
    public void OnTutorialCompleted()
    {
        HandleTutorialCompleted();
    }

    public void ShowHowToPlay()
    {
        if (constellationTutorial == null || tutorialOpened)
            return;

        tutorialOpened = true;

        // It was disabled for level 2+, so it must be enabled for manual viewing.
        constellationTutorial.enabled = true;
        StartCoroutine(OpenTutorialManually());
    }

    private IEnumerator OpenTutorialManually()
    {
        yield return null;

        if (constellationTutorial == null)
            yield break;

        constellationTutorial.OpenTutorial();
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
            data.constellationLevel,
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
        if (config == null)
        {
             return;
        }

        currentLevel = Mathf.Clamp(levelNumber, 1, ProgressionManager.MAX_LEVEL);
        levelCfg = config.GetLevel(currentLevel);

        if (levelCfg == null)
        {
           return;
        }

        levelStartedAt = currentLevel;
        levelStartTime = Time.time;
     trialIndexInLevel = 0;
trialsCompleteInLevel = 0;
successfulTrialsInLevel = 0;
consecutiveFailsOnLevel = 0;
wrongAttempts = 0;

activeLevelTimer.Start();
        busy = false;

        hud.SetupDay(levelCfg.trials);
        hud.SetTrialsDone(0);

        StartNewTrial();
    }

    private void StartNewTrial()
    {
        wrongAttempts = 0;
        playerSequence = new List<int>();
        trialStartTime = Time.time;

        int span = UnityEngine.Random.Range(
            levelCfg.spanMin,
            levelCfg.spanMax + 1
        );

        currentSequence = GenerateUniqueSequence(span);
        visibleSet = new HashSet<int>(currentSequence);

        hud.SetupTrial();
        controller.ResetAll();
        controller.SetVisibleStars(visibleSet);
        controller.BeginTrial(
            currentSequence,
            CurrentStarOnSeconds,
            CurrentGapSeconds
        );
    }

    public void ReplaySameTrial()
    {
        hud.SetupTrial();
        controller.ResetAll();
        controller.SetVisibleStars(visibleSet);
        controller.BeginTrial(
            currentSequence,
            CurrentStarOnSeconds,
            CurrentGapSeconds
        );
    }

    private void HandleTrialFinished(bool success, List<int> playerSeq)
    {
        if (busy)
            return;

        busy = true;

        int completionMs = Mathf.RoundToInt(
            (Time.time - trialStartTime) * 1000f
        );

        playerSequence = playerSeq ?? new List<int>();

        if (success)
        {
            HandleCorrectTrial(completionMs);
            return;
        }

        HandleWrongTrial(completionMs);
    }
private void OnApplicationPause(bool pauseStatus)
{
    activeLevelTimer.SetPaused(pauseStatus);
}
    private void HandleCorrectTrial(int completionMs)
    {
        AudioManager.Instance.Play("SuccessDing2");

        trialsCompleteInLevel++;
successfulTrialsInLevel++;
consecutiveFailsOnLevel = 0;
        hud.SetTrialsDone(trialsCompleteInLevel);

        var trialResult = ProgressionManager.Instance.EvaluateTrial(
            "Constellation",
            isCorrect: true,
            wrongAttempts: wrongAttempts,
            completionTimeMs: completionMs,
            span: currentSequence.Length,
            consecutiveFails: 0
        );

        RecordTrial("Constellation", trialResult, true, wrongAttempts, completionMs);

        if (trialsCompleteInLevel >= levelCfg.trials)
        {
            CompleteLevelAfterTrials(false);
            busy = false;
            return;
        }

        feedbackMessanger?.ShowSuccess(
            config.GetSuccessTitle(levelCfg),
            config.GetTrialSuccessMessage(
                levelCfg,
                trialsCompleteInLevel,
                currentSequence.Length
            )
        );

        hud.ShowTrialComplete();
        QueueAutoNextTrial();
    }

    private void HandleWrongTrial(int completionMs)
    {
        AudioManager.Instance.Play("StarError");

        wrongAttempts++;
        consecutiveFailsOnLevel++;

        if (consecutiveFailsOnLevel >= ProgressionManager.ASSISTED_PASS_FAIL_LIMIT)
        {
            var assistResult = ProgressionManager.Instance.EvaluateTrial(
                "Constellation",
                isCorrect: false,
                wrongAttempts: wrongAttempts,
                completionTimeMs: completionMs,
                span: currentSequence.Length,
                consecutiveFails: consecutiveFailsOnLevel
            );

            RecordTrial(
                "Constellation",
                assistResult,
                false,
                wrongAttempts,
                completionMs
            );

            trialsCompleteInLevel = levelCfg.trials;
            hud.SetTrialsDone(trialsCompleteInLevel);
            consecutiveFailsOnLevel = 0;

            CompleteLevelAfterTrials(true);
            busy = false;
            return;
        }

        var wrong = config.GetRandomWrongPattern(levelCfg);
        feedbackMessanger?.ShowWrongPattern(wrong.title, wrong.message);

        StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
    {
        yield return new WaitForSeconds(0.3f);

        ReplaySameTrial();
        busy = false;
    }

    private void QueueAutoNextTrial()
    {
        if (autoNextTrialRoutine != null)
            StopCoroutine(autoNextTrialRoutine);

        autoNextTrialRoutine = StartCoroutine(AutoNextTrialRoutine());
    }

    private IEnumerator AutoNextTrialRoutine()
    {
        yield return new WaitForSeconds(autoNextTrialDelay);

        hud.HideTrialComplete();
        busy = false;

        StartNewTrial();
    }

    private void CompleteLevelAfterTrials(bool assistedLevelCompletion)
    {int activeDurationMs =
    activeLevelTimer.StopAndGetMilliseconds();

var levelRecord = new LevelCompletionRecord
{
    eventId = Guid.NewGuid().ToString(),

    playerId = PlayerDataManager.Instance.Data.profile.phoneNumber,

    minigame = "Constellation",
    levelNumber = currentLevel,

    successfulTrials = successfulTrialsInLevel,
    requiredTrials = levelCfg.trials,

    normalPass = !assistedLevelCompletion,
    assistedPass = assistedLevelCompletion,

    activeDurationMs = activeDurationMs,

    startedAtUtc = activeLevelTimer.StartedAtUtc,
    completedAtUtc = DateTime.UtcNow.ToString("o")
};

Debug.Log(
    $"[LEVEL REPORT] " +
    $"{levelRecord.playerId} | " +
    $"{levelRecord.minigame} L{levelRecord.levelNumber} | " +
    $"{levelRecord.successfulTrials}/{levelRecord.requiredTrials} | " +
    $"Normal={levelRecord.normalPass} | " +
    $"Assisted={levelRecord.assistedPass} | " +
    $"Duration={levelRecord.activeDurationMs}ms | " +
    $"Started={levelRecord.startedAtUtc} | " +
    $"Completed={levelRecord.completedAtUtc}"
);OfflineQueue.Instance?.EnqueueLevelReport(levelRecord);
        int levelCompletionMs = Mathf.RoundToInt(
            (Time.time - levelStartTime) * 1000f
        );

        float averageSpan = (levelCfg.spanMin + levelCfg.spanMax) * 0.5f;

        var levelResult = ProgressionManager.Instance.EvaluateTrial(
            "Constellation",
            isCorrect: !assistedLevelCompletion,
            wrongAttempts: assistedLevelCompletion
                ? ProgressionManager.ASSISTED_PASS_FAIL_LIMIT
                : 0,
            completionTimeMs: levelCompletionMs,
            span: Mathf.RoundToInt(averageSpan),
            consecutiveFails: assistedLevelCompletion
                ? ProgressionManager.ASSISTED_PASS_FAIL_LIMIT
                : 0
        );

        var data = PlayerDataManager.Instance.Data;

        bool completedCurrentUnlockedLevel =
            levelStartedAt == data.constellationLevel;

        if (completedCurrentUnlockedLevel)
        {
            ProgressionManager.Instance.CompleteLevel(
                "Constellation",
                levelResult
            );

            int nextLevel = Mathf.Min(
                levelStartedAt + 1,
                ProgressionManager.MAX_LEVEL
            );

            data.constellationLevel = Mathf.Max(
                data.constellationLevel,
                nextLevel
            );

            // Completing Constellation's gateway, including an assisted pass,
            // awards gem 1 and unlocks the Bridge minigame.
            if (config.IsGatewayLevel(levelCfg))
            {
                data.constellationGateReached = true;
                data.bridgeUnlocked = true;
            }

            PlayerDataManager.Instance.Save();
            ScheduleNextLevelLock();
        }

        bool showKey =
            completedCurrentUnlockedLevel &&
            config.IsGatewayLevel(levelCfg);

        string message = assistedLevelCompletion
            ? config.GetRandomAssistedPassMessage()
            : config.GetFinalSuccessMessage(levelCfg);

        feedbackMessanger?.ShowOutcomePanel(
            title: string.Empty,
            message: message,
            assisted: assistedLevelCompletion,
            showKey: showKey
        );

        hud.ShowLevelComplete(
            completedLevel: currentLevel,
            currentLevel: PlayerDataManager.Instance.Data.constellationLevel
        );

        busy = false;
    }

    private void ScheduleNextLevelLock()
    {
        var data = PlayerDataManager.Instance.Data;
        int nextLevel = data.constellationLevel;

        data.constellationLastLevelCompletionTime =
            DateTime.UtcNow.ToString("o");

        if (nextLevel > ProgressionManager.MAX_LEVEL)
        {
            ClearConstellationLock();
            return;
        }

        var nextCfg = config.GetLevel(nextLevel);

        float lockHours = nextCfg != null
            ? Mathf.Max(0f, nextCfg.lockDurationHours)
            : 0f;

        if (lockHours <= 0f)
        {
            ClearConstellationLock();
            return;
        }

        data.constellationLockLevel = nextLevel;
        data.constellationLockUntilTime =
            DateTime.UtcNow.AddHours(lockHours).ToString("o");

        PlayerDataManager.Instance.Save();
    }

    private void ClearConstellationLock()
    {
        var data = PlayerDataManager.Instance.Data;

        data.constellationLockLevel = 0;
        data.constellationLockUntilTime = "";

        PlayerDataManager.Instance.Save();
    }

    private void RecordTrial(
        string minigameId,
        ProgressionManager.LevelResult result,
        bool isCorrect,
        int attempts,
        int completionMs
    )
    {
        sessionData.Add(new TrialRecord
        {
            minigame_id = minigameId,
            day = currentLevel,
            level_number = currentLevel,
            trial_index = trialIndexInLevel + 1,

            span = currentSequence.Length,
            target_sequence = new List<int>(currentSequence),
            sequence_recalled = playerSequence,

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

    private static int[] GenerateUniqueSequence(int span)
    {
        List<int> pool = new();

        for (int i = 1; i <= 9; i++)
            pool.Add(i);

        for (int i = 0; i < span; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, pool.Count);
            (pool[i], pool[randomIndex]) =
                (pool[randomIndex], pool[i]);
        }

        int[] sequence = new int[span];

        for (int i = 0; i < span; i++)
            sequence[i] = pool[i];

        return sequence;
    }

    private void OnDestroy()
    {
        if (controller != null)
            controller.OnTrialFinished -= HandleTrialFinished;

        if (introductionManager != null)
            introductionManager.IntroductionCompleted -= HandleIntroductionCompleted;

        if (constellationTutorial != null)
            constellationTutorial.TutorialCompleted -= HandleTutorialCompleted;
    }
}