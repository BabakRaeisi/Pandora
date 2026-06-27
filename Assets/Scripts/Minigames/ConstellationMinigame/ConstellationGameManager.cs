using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

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

    [Header("Onboarding")]
    [SerializeField] private bool forceShowTutorialOnStart = false;

    [Header("Session Data")]
    [SerializeField] private SessionDataSO sessionData;

    [Header("Countdown")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Flow")]
    [SerializeField, Min(0f)] private float autoNextTrialDelay = 0.9f;
    private Coroutine autoNextTrialRoutine;

    // public event Action TrialStarted;
    // public event Action TrialFailed;
    // public event Action TrialSucceeded;

    // ── Current level state ───────────────────────────────────────────────────
    private int currentLevel;
    private ConstellationConfigSO.LevelConfig levelCfg;

    // ── Trial state ───────────────────────────────────────────────────────────
    private int trialIndexInLevel;
    private int trialsCompleteInLevel;
    private int consecutiveFailsOnLevel;
    private int wrongAttempts;
    private bool busy;

    private int[] currentSequence;
    private List<int> playerSequence;
    private HashSet<int> visibleSet;

    private float levelStartTime;
    private float trialStartTime;

    private float CurrentStarOnSeconds => timingProfile ? timingProfile.StarDisplaySeconds : 1.0f;
    private float CurrentGapSeconds => timingProfile ? timingProfile.GapSeconds : 0.25f;

    private bool gameplayBootstrapped;
    private string tutorialVisitKey;

    // ADD:
    private bool bootstrapRequested;
    private bool introStepCompleted;
    private bool tutorialStepCompleted;
    private bool tutorialOpened;

    // REMOVE this if present:
    // private Coroutine tutorialFailSafeRoutine;

    void Awake()
    {
        tutorialVisitKey = $"ConstellationTutorialShown_{SceneManager.GetActiveScene().name}";

        // Intro must complete first (unless intro manager does not exist).
        introStepCompleted = (introductionManager == null);

        // Tutorial required only on first visit (unless tutorial object missing).
        tutorialStepCompleted = (constellationTutorial == null) || !NeedsTutorialFirstVisit();
        tutorialOpened = false;

        if (introductionManager != null)
            introductionManager.IntroductionCompleted += HandleIntroductionCompleted;

        if (constellationTutorial != null)
            constellationTutorial.TutorialCompleted += HandleTutorialCompleted;
    }

    void Start()
    {
        controller.OnTrialFinished += HandleTrialFinished;

        AudioManager.Instance.StopAll();
        AudioManager.Instance.Play("SpaceAmbientSound");

        RequestBootstrapGameFlow();
    }

    private void RequestBootstrapGameFlow()
    {
        bootstrapRequested = true;
        EvaluateOnboardingGate();
    }

    private void EvaluateOnboardingGate()
    {
        if (!bootstrapRequested || gameplayBootstrapped) return;

        // Step 1: Intro must finish first.
        if (!introStepCompleted) return;

        // Step 2: Then tutorial (first visit).
        if (!tutorialStepCompleted)
        {
            if (!tutorialOpened)
            {
                tutorialOpened = true;

                if (constellationTutorial != null)
                {
                    constellationTutorial.OpenTutorial();
                    return;
                }

                // Fallback if tutorial ref is missing
                tutorialStepCompleted = true;
            }
            else
            {
                return;
            }
        }

        // Step 3: start gameplay countdown
        StartGameplayIfNeeded();
    }

    private void HandleIntroductionCompleted()
    {
        if (gameplayBootstrapped) return;
        introStepCompleted = true;
        EvaluateOnboardingGate();
    }

    private void HandleTutorialCompleted()
    {
        if (gameplayBootstrapped) return;

        if (!tutorialStepCompleted)
        {
            MarkTutorialVisited();
            tutorialStepCompleted = true;
        }

        tutorialOpened = false;
        EvaluateOnboardingGate();
    }

    public void OnTutorialCompleted()
    {
        HandleTutorialCompleted();
    }

    private bool NeedsTutorialFirstVisit()
    {
        if (forceShowTutorialOnStart) return true;
        return PlayerPrefs.GetInt(tutorialVisitKey, 0) == 0;
    }

    private void MarkTutorialVisited()
    {
        PlayerPrefs.SetInt(tutorialVisitKey, 1);
        PlayerPrefs.Save();
    }

    private void StartGameplayIfNeeded()
    {
        if (gameplayBootstrapped) return;
        gameplayBootstrapped = true;
        StartCoroutine(BeginGameAfterCountdown());
    }

    IEnumerator BeginGameAfterCountdown()
    {
        var data = PlayerDataManager.Instance.Data;
        int startLevel = Mathf.Clamp(data.constellationLevel, 1, ProgressionManager.MAX_LEVEL);

      

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

    private int levelStartedAt; // strict: allow only +1 unlock from played level

    public void StartLevel(int levelNumber)
    {
        currentLevel = Mathf.Clamp(levelNumber, 1, ProgressionManager.MAX_LEVEL);
        levelCfg = config.GetLevel(currentLevel);
        if (levelCfg == null) return;

        levelStartedAt = currentLevel;

        levelStartTime = Time.time;
        trialIndexInLevel = 0;
        trialsCompleteInLevel = 0;
        consecutiveFailsOnLevel = 0;

        hud.SetupDay(levelCfg.trials);
        hud.SetTrialsDone(0);

        StartNewTrial();
    }

    void StartNewTrial()
    {
        wrongAttempts = 0;
        playerSequence = new List<int>();
        trialStartTime = Time.time;

        int span = UnityEngine.Random.Range(levelCfg.spanMin, levelCfg.spanMax + 1);
        currentSequence = GenerateUniqueSequence(span);
        visibleSet = new HashSet<int>(currentSequence);

        Debug.Log($"[ConstellationGM] StartNewTrial | trialIdx={trialIndexInLevel + 1}, done={trialsCompleteInLevel}/{levelCfg.trials}, span={span}");

        hud.SetupTrial();
        controller.ResetAll();
        controller.SetVisibleStars(visibleSet);
        controller.BeginTrial(currentSequence, CurrentStarOnSeconds, CurrentGapSeconds);
    }

    public void ReplaySameTrial()
    {
        hud.SetupTrial();
        controller.ResetAll();
        controller.SetVisibleStars(visibleSet);
        controller.BeginTrial(currentSequence, CurrentStarOnSeconds, CurrentGapSeconds);
    }

    void HandleTrialFinished(bool success, List<int> playerSeq)
    {
        if (busy) return;
        busy = true;

        int completionMs = Mathf.RoundToInt((Time.time - trialStartTime) * 1000f);
        playerSequence = playerSeq ?? new List<int>();

        if (success)
        {
            AudioManager.Instance.Play("SuccessDing2");

            trialsCompleteInLevel++;
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
                // final trial: show level success only inside CompleteLevelAfterTrials
                CompleteLevelAfterTrials(false);
                busy = false;
                return;
            }

            // non-final trial: show per-trial success message
            feedbackMessanger?.ShowSuccess(
                config.GetSuccessTitle(levelCfg),
                config.GetTrialSuccessMessage(levelCfg, trialsCompleteInLevel, currentSequence.Length)
            );

            hud.ShowTrialComplete();
            QueueAutoNextTrial();
            return;
        }
        else
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

                RecordTrial("Constellation", assistResult, false, wrongAttempts, completionMs);

                // remove old assisted toast here (panel shown at level completion)
                // var assistedToast = config.GetAssistedPassToast(levelCfg, currentLevel);
                // feedbackMessanger?.ShowInfo(assistedToast.title, assistedToast.message);

                trialsCompleteInLevel = levelCfg.trials;
                hud.SetTrialsDone(trialsCompleteInLevel);
                consecutiveFailsOnLevel = 0;

                CompleteLevelAfterTrials(true);
                busy = false;
                return;
            }
            else
            {
                feedbackMessanger?.ShowWrongPattern();
                StartCoroutine(FailRoutine());
            }
        }
    }

    IEnumerator FailRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        ReplaySameTrial();
        busy = false;
    }

    private void QueueAutoNextTrial()
    {
        if (autoNextTrialRoutine != null) StopCoroutine(autoNextTrialRoutine);
        autoNextTrialRoutine = StartCoroutine(AutoNextTrialRoutine());
    }

    private IEnumerator AutoNextTrialRoutine()
    {
        Debug.Log($"[ConstellationGM] AutoNextTrial in {autoNextTrialDelay:0.00}s");
        yield return new WaitForSeconds(autoNextTrialDelay);
        hud.HideTrialComplete();
        busy = false;
        StartNewTrial();
    }

    // REMOVE button handler entirely if not used:
    // public void OnNextTrialButton() { ... }

    // ── Level completion and progression ──────────────────────────────────────

    void CompleteLevelAfterTrials(bool assistedLevelCompletion)
    {
        int levelCompletionMs = Mathf.RoundToInt((Time.time - levelStartTime) * 1000f);
        float avgSpan = (levelCfg.spanMin + levelCfg.spanMax) * 0.5f;

        var levelResult = ProgressionManager.Instance.EvaluateTrial(
            "Constellation",
            isCorrect: !assistedLevelCompletion,
            wrongAttempts: assistedLevelCompletion ? ProgressionManager.ASSISTED_PASS_FAIL_LIMIT : 0,
            completionTimeMs: levelCompletionMs,
            span: Mathf.RoundToInt(avgSpan),
            consecutiveFails: assistedLevelCompletion ? ProgressionManager.ASSISTED_PASS_FAIL_LIMIT : 0
        );

        var finalResult = ProgressionManager.Instance.CompleteLevel("Constellation", levelResult);

        // no performance-based boost: exactly next level max from this played level
        var data = PlayerDataManager.Instance.Data;
        int strictNext = Mathf.Min(levelStartedAt + 1, ProgressionManager.MAX_LEVEL);
        data.constellationLevel = strictNext;
        PlayerDataManager.Instance.Save();

        ScheduleNextLevelLock();

        // Show dedicated end panel (NOT toast)
        bool showKey = (currentLevel == 8); // special unlock level
        if (assistedLevelCompletion)
        {
            var assistedToast = config.GetAssistedPassToast(levelCfg, currentLevel);
            feedbackMessanger?.ShowOutcomePanel(
                assistedToast.title,
                assistedToast.message,
                assisted: true,
                showKey: showKey
            );
        }
        else
        {
            var successToast = config.GetLevelSuccessToast(levelCfg, currentLevel);
            feedbackMessanger?.ShowOutcomePanel(
                successToast.title,
                successToast.message,
                assisted: false,
                showKey: showKey
            );
        }

        hud.ShowLevelComplete(
            completedLevel: currentLevel,
            currentLevel: PlayerDataManager.Instance.Data.constellationLevel
        );

        busy = false;
    }

    void ScheduleNextLevelLock()
    {
        var data = PlayerDataManager.Instance.Data;
        int nextLevel = data.constellationLevel;

        data.constellationLastLevelCompletionTime = DateTime.UtcNow.ToString("o");

        // Lock can apply to any playable next level based on config.
        if (nextLevel > ProgressionManager.MAX_LEVEL)
        {
            ClearConstellationLock();
            return;
        }

        var nextCfg = config.GetLevel(nextLevel);
        float lockHours = nextCfg != null ? Mathf.Max(0f, nextCfg.lockDurationHours) : 0f;

        if (lockHours <= 0f)
        {
            ClearConstellationLock();
            return;
        }

        DateTime lockUntil = DateTime.UtcNow.AddHours(lockHours);
        data.constellationLockLevel = nextLevel;
        data.constellationLockUntilTime = lockUntil.ToString("o");
        PlayerDataManager.Instance.Save();
    }

    void ClearConstellationLock()
    {
        var data = PlayerDataManager.Instance.Data;
        data.constellationLockLevel = 0;
        data.constellationLockUntilTime = "";
        PlayerDataManager.Instance.Save();
    }

    
    void RecordTrial(string minigameId, ProgressionManager.LevelResult result, bool isCorrect, int wrongAttempts, int completionMs)
    {
        sessionData.Add(new TrialRecord
        {
            minigame_id = minigameId,
            day = currentLevel,               // Legacy: store level as day
            level_number = currentLevel,
            trial_index = trialIndexInLevel + 1,

            span = currentSequence.Length,
            target_sequence = new List<int>(currentSequence),
            sequence_recalled = playerSequence,

            is_correct = isCorrect,
            wrong_attempts = wrongAttempts,
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

    // ── Utilities ─────────────────────────────────────────────────────────────

    static int[] GenerateUniqueSequence(int span)
    {
        List<int> pool = new();
        for (int i = 1; i <= 9; i++) pool.Add(i);

        for (int i = 0; i < span; i++)
        {
            int j = UnityEngine.Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int[] seq = new int[span];
        for (int i = 0; i < span; i++) seq[i] = pool[i];
        return seq;
    }

    private void OnDestroy()
    {
        if (controller != null) controller.OnTrialFinished -= HandleTrialFinished;
        if (introductionManager != null) introductionManager.IntroductionCompleted -= HandleIntroductionCompleted;
        if (constellationTutorial != null) constellationTutorial.TutorialCompleted -= HandleTutorialCompleted;
    }
}