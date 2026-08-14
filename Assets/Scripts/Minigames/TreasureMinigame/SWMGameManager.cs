// SWMGameManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using RTLTMPro;
using UnityEngine;

public class SWMGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ChestSpawnerRandom spawner;
    [SerializeField] private SWMHUD hud;
    [SerializeField] private SWMConfig config;
    [SerializeField] private FeedbackMessanger feedbackMessanger;
    [SerializeField] private IntroductionManager introductionManager;
    [SerializeField] private SWMTutorial swmTutorial;

    [Header("Session Data")]
    [SerializeField] private SessionDataSO sessionData;

    [Header("Countdown")]
    [SerializeField] private RTLTextMeshPro countdownText;

    [Header("Flow")]
    [SerializeField, Min(0f)] private float autoNextTrialDelay = 0.9f;
    [SerializeField, Min(1)] private int assistedFailLimit = 3;

    private const string SelectedLevelKey = "TreasureSelectedLevel";
    private const string GatewayPanelPendingKey = "SWMGatewayPanelPending";

    private Coroutine autoNextTrialRoutine;

    // ── Onboarding state ──────────────────────────────────────────────────────
    private bool gameplayBootstrapped;
    private bool bootstrapRequested;
    private bool introStepCompleted;
    private bool tutorialStepCompleted;
    private bool tutorialOpened;

    // ── Current level state ───────────────────────────────────────────────────
    private int currentLevel;
    private int levelStartedAt;
    private SWMConfig.LevelConfig levelCfg;

    // ── Trial state ───────────────────────────────────────────────────────────
    private int trialsCompleteInLevel;
    private int consecutiveFailsOnLevel;
    private int trialIndexInLevel;
    private int successfulTrialsInLevel;
private ActiveLevelTimer activeLevelTimer = new();
    private List<SWMChest> pool = new();
    private int poolSize;

    private readonly Dictionary<SWMChest, int> chestId = new();

    private HashSet<int> usedTreasureIndicesInLevel = new();

    private int numBoxes;
    private int goalCollected;
    private HashSet<int> treasureIndices = new();

    private float levelStartTime;
    private float trialStartTime;
    private float firstClickTime = -1f;
    private bool trialComplete;

    private int collectedFound;
    private int betweenErrors;
    private int withinErrors;
    private int totalSelections;

    private SWMTrialData currentData;

    private void Awake()
    {
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

        if (isStartingLevelOne && swmTutorial != null)
        {
            tutorialStepCompleted = false;
            tutorialOpened = false;

            swmTutorial.TutorialCompleted -= HandleTutorialCompleted;
            swmTutorial.TutorialCompleted += HandleTutorialCompleted;

            // The manager enables it only after IntroductionManager completes.
            swmTutorial.enabled = false;
        }
        else
        {
            tutorialStepCompleted = true;
            tutorialOpened = false;

            if (swmTutorial != null)
                swmTutorial.enabled = false;
        }

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    private void Start()
    {
        AudioManager.Instance.StopAll();
        AudioManager.Instance.Play("MapAmbient");
        AudioManager.Instance.Play("MusicLoop");

        RequestBootstrapGameFlow();
    }

    private void OnDestroy()
    {
        if (introductionManager != null)
            introductionManager.IntroductionCompleted -= HandleIntroductionCompleted;

        if (swmTutorial != null)
            swmTutorial.TutorialCompleted -= HandleTutorialCompleted;
    }

    // ── Introduction / Tutorial / Countdown flow ──────────────────────────────

    private void RequestBootstrapGameFlow()
    {
        bootstrapRequested = true;
        EvaluateOnboardingGate();
    }

    private void EvaluateOnboardingGate()
    {
        if (!bootstrapRequested || gameplayBootstrapped)
            return;

        // Level 1 waits for Introduction, then Tutorial.
        if (!introStepCompleted || !tutorialStepCompleted)
            return;

        // Level 2+ reaches here immediately.
        StartGameplayIfNeeded();
    }

    private void HandleIntroductionCompleted()
    {
        if (gameplayBootstrapped || introStepCompleted)
            return;

        introStepCompleted = true;

        if (!tutorialStepCompleted && swmTutorial != null)
        {
            swmTutorial.enabled = true;
            StartCoroutine(OpenTutorialAfterIntroduction());
            return;
        }

        EvaluateOnboardingGate();
    }

    private IEnumerator OpenTutorialAfterIntroduction()
    {
        // Allows SWMTutorial OnEnable/Start setup to finish.
        yield return null;

        if (gameplayBootstrapped ||
            tutorialStepCompleted ||
            tutorialOpened ||
            swmTutorial == null)
        {
            yield break;
        }

        tutorialOpened = true;
        swmTutorial.OpenTutorial();
    }

    private void HandleTutorialCompleted()
    {
        if (!tutorialOpened)
            return;

        tutorialOpened = false;

        // Only the automatic level-one tutorial releases the countdown.
        if (!tutorialStepCompleted)
        {
            tutorialStepCompleted = true;
            EvaluateOnboardingGate();
        }
    }

    // Optional target for the final tutorial button in the Inspector.
    public void OnTutorialCompleted()
    {
        HandleTutorialCompleted();
    }

    // Assign this method to the in-game "How To Play" button.
    public void ShowHowToPlay()
    {
        if (swmTutorial == null || tutorialOpened)
            return;

        tutorialOpened = true;
        swmTutorial.enabled = true;
        StartCoroutine(OpenTutorialManually());
    }

    private IEnumerator OpenTutorialManually()
    {
        yield return null;

        if (swmTutorial != null)
            swmTutorial.OpenTutorial();
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
            data.swmLevel,
            1,
            GetTotalLevels()
        );

        int requestedLevel = PlayerPrefs.GetInt(
            SelectedLevelKey,
            unlockedLevel
        );

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

    // ── Level / trial flow ────────────────────────────────────────────────────

    public void StartLevel(int levelNumber)
    {
        if (config == null || sessionData == null || spawner == null)
        {
        
        }

        currentLevel = Mathf.Clamp(levelNumber, 1, GetTotalLevels());
        levelCfg = config.GetLevel(currentLevel);

        if (levelCfg.levelNumber == 0)
        {
       
            return;
        }

        // Trials must equal chest count: exactly one trial per chest,
        // so the treasure visits every chest once per elimination cycle
        // before positions are reshuffled.
        poolSize = Mathf.Clamp(levelCfg.boxes, 3, 12);

        if (levelCfg.trials != poolSize)
        {
           
            levelCfg.trials = poolSize;
        }

        StopRunningTrialRoutines();

        levelStartedAt = currentLevel;
        levelStartTime = Time.time;
        trialsCompleteInLevel = 0;
successfulTrialsInLevel = 0;
consecutiveFailsOnLevel = 0;
trialIndexInLevel = 0;

activeLevelTimer.Start();
        usedTreasureIndicesInLevel.Clear();

        hud?.SetupDay(levelCfg.trials);
        hud?.SetTrialsDone(trialsCompleteInLevel);
        hud?.SetupTrial();
        hud?.ShowDayComplete();

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null)
                Destroy(pool[i].gameObject);
        }

        pool.Clear();
        chestId.Clear();

        pool = spawner.SpawnPool(poolSize, this);

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null)
                chestId[pool[i]] = i;
        }
    
        // Randomize chest positions once for this level.
        // They remain fixed while all trials in this level are played.
        spawner.Reposition(pool, poolSize);

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
    firstClickTime = -1f;

    numBoxes = Mathf.Clamp(levelCfg.boxes, 3, poolSize);

    // Once the treasure has visited every chest, reshuffle chest positions
    // and start a new "elimination cycle".
    if (usedTreasureIndicesInLevel.Count >= numBoxes)
    {
        usedTreasureIndicesInLevel.Clear();
        spawner.Reposition(pool, numBoxes);
    }

    goalCollected = 1;

    collectedFound = 0;
    betweenErrors = 0;
    withinErrors = 0;
    totalSelections = 0;

    hud?.SetupTrial();

    // One treasure changes location each trial.
    // It must never reuse a chest index that already held the treasure
    // in this elimination cycle.
    treasureIndices = PickUniqueIndices(numBoxes, 1, usedTreasureIndicesInLevel);
    usedTreasureIndicesInLevel.UnionWith(treasureIndices);

    for (int i = 0; i < numBoxes; i++)
    {
        if (pool[i] != null)
            pool[i].ResetForTrial(treasureIndices.Contains(i));
    }

    trialStartTime = Time.time;
    currentData = NewTrialDataSkeleton();
}
    public void OnChestPressed(SWMChest chest)
    {
        if (trialComplete || chest == null || !chest.gameObject.activeInHierarchy)
            return;

        if (firstClickTime < 0f)
            firstClickTime = Time.time;

        totalSelections++;

        int elapsedMs = Mathf.RoundToInt(
            (Time.time - trialStartTime) * 1000f
        );

        int chestIndex = GetChestId(chest);

        // A chest that ever held the treasure during this elimination cycle
        // is permanently an error target, regardless of the current trial's
        // visual reset state (ResetForTrial re-hides chests every trial).
        bool chestEverHadTreasure =
            usedTreasureIndicesInLevel.Contains(chestIndex) &&
            !treasureIndices.Contains(chestIndex);

        if (chestEverHadTreasure)
        {
            chest.RevealAgain();

            withinErrors++;
            RecordSelection(chestIndex, "within_error", elapsedMs);
            consecutiveFailsOnLevel++;
            HandleWrongSelection();
            return;
        }

        if (chest.State == SWMChest.ChestState.Unopened)
        {
            chest.RevealFirstTime();
            AudioManager.Instance.Play("ChestOpen");

            if (chest.HasTreasure)
            {
                collectedFound = 1;

                RecordSelection(chestIndex, "treasure", elapsedMs);
                AudioManager.Instance.Play("Coin");

                CompleteTrial(false);
                return;
            }

            // First opening of a chest that has never had treasure is not an error.
            RecordSelection(chestIndex, "empty", elapsedMs);
            AudioManager.Instance.Play("ChestClose");
            return;
        }

        // Already opened this trial, currently holds the treasure
        // (repeat click on the correct chest before completing the trial).
        chest.RevealAgain();
        RecordSelection(chestIndex, "empty_repeat", elapsedMs);
    }

    private void HandleWrongSelection()
    {
        var wrong = config.GetRandomWrongPattern(levelCfg);
        feedbackMessanger?.ShowWrongPattern(wrong.title, wrong.message);

        int wrongAttempts = betweenErrors + withinErrors;

        // Option A: assist after three wrong selections in this trial.
        if (wrongAttempts >= assistedFailLimit)
            CompleteTrial(true);
    }

    private int GetChestId(SWMChest chest)
    {
        if (chestId.TryGetValue(chest, out int id))
            return id;

        return pool.IndexOf(chest);
    }

    private void CompleteTrial(bool assisted)
    {
        if (trialComplete)
            return;

        trialComplete = true;

        int completionMs = Mathf.RoundToInt(
            (Time.time - trialStartTime) * 1000f
        );

        int firstClickLatencyMs = firstClickTime < 0f
            ? 0
            : Mathf.RoundToInt(
                (firstClickTime - trialStartTime) * 1000f
            );

        if (currentData == null)
            currentData = NewTrialDataSkeleton();

        currentData.between_errors = betweenErrors;
        currentData.within_errors = withinErrors;
        currentData.total_selections = totalSelections;
        currentData.completion_time_ms = completionMs;
        currentData.first_click_latency_ms = firstClickLatencyMs;

        int wrongAttempts = betweenErrors + withinErrors;
        int span = goalCollected;
        bool isCorrect = !assisted && collectedFound == goalCollected;

        var trialResult = ProgressionManager.Instance.EvaluateTrial(
            "SWM",
            isCorrect,
            wrongAttempts,
            completionMs,
            span,
            consecutiveFailsOnLevel
        );

        var targets = new List<int>(treasureIndices);
        targets.Sort();

        RecordTrial(
            "SWM",
            trialResult,
            isCorrect,
            wrongAttempts,
            completionMs,
            targets
        );

        currentData = null;

        if (assisted)
        {
            // Match Bridge behavior: assisted pass ends this level.
            trialsCompleteInLevel = levelCfg.trials;
            hud?.SetTrialsDone(trialsCompleteInLevel);
            CompleteLevelAfterTrials(true);
            return;
        }

       trialsCompleteInLevel++;
successfulTrialsInLevel++;

hud?.SetTrialsDone(trialsCompleteInLevel);

        if (trialsCompleteInLevel >= levelCfg.trials)
        {
            CompleteLevelAfterTrials(false);
            return;
        }

        var success = config.GetRandomTrialSuccess(levelCfg);
        feedbackMessanger?.ShowSuccess(
            config.GetSuccessTitle(levelCfg),
            success.message
        );

        consecutiveFailsOnLevel = 0;
        hud?.ShowTrialComplete();

        autoNextTrialRoutine = StartCoroutine(AutoNextTrialRoutine());
    }

    private IEnumerator AutoNextTrialRoutine()
    {
        yield return new WaitForSeconds(autoNextTrialDelay);
        StartNextTrial();
    }
    private void OnApplicationPause(bool pauseStatus)
{
    activeLevelTimer.SetPaused(pauseStatus);
}
    private void CompleteLevelAfterTrials(bool assistedLevelCompletion)
    {
        int activeDurationMs =
    activeLevelTimer.StopAndGetMilliseconds();

var levelRecord = new LevelCompletionRecord
{
    eventId = Guid.NewGuid().ToString(),

    playerId = PlayerDataManager.Instance.Data.profile.phoneNumber,

    minigame = "SWM",
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
        StopRunningTrialRoutines();

        int completionMs = Mathf.RoundToInt(
            (Time.time - levelStartTime) * 1000f
        );

        var result = ProgressionManager.Instance.EvaluateTrial(
            "SWM",
            !assistedLevelCompletion,
            assistedLevelCompletion
                ? assistedFailLimit
                : consecutiveFailsOnLevel,
            completionMs,
            levelCfg.treasures,
            assistedLevelCompletion
                ? assistedFailLimit
                : 0
        );

        var data = PlayerDataManager.Instance.Data;

        bool completedCurrentUnlockedLevel =
            levelStartedAt == data.swmLevel;

        if (completedCurrentUnlockedLevel)
        {
            ProgressionManager.Instance.CompleteLevel("SWM", result);

            int nextLevel = Mathf.Min(
                levelStartedAt + 1,
                GetTotalLevels()
            );

            data.swmLevel = Mathf.Max(data.swmLevel, nextLevel);

            // Match Constellation and Bridge gateway behavior.
            if (config.IsGatewayLevel(levelCfg))
                data.swmGateReached = true;

            PlayerDataManager.Instance.Save();
        }

        bool passedGatewayLevel =
            completedCurrentUnlockedLevel &&
            config.IsGatewayLevel(levelCfg);

        // Gateway completion must be saved even if this level was replayed.
        if (passedGatewayLevel)
        {
            data.swmGateReached = true;

            // Permanent progression state: changes SWM map background.
            data.swmGateReached = true;

            // Temporary state: show the gateway popup only on the next SWM map load.
            PlayerPrefs.SetInt(GatewayPanelPendingKey, 1);
            PlayerPrefs.Save();

         
        }

        feedbackMessanger?.ShowOutcomePanel(
            string.Empty,
            assistedLevelCompletion
                ? config.GetRandomAssistedPassMessage()
                : config.GetFinalSuccessMessage(levelCfg),
            assistedLevelCompletion,
            passedGatewayLevel
        );

        hud?.ShowDayComplete();
    }

    private void StopRunningTrialRoutines()
    {
        if (autoNextTrialRoutine == null)
            return;

        StopCoroutine(autoNextTrialRoutine);
        autoNextTrialRoutine = null;
    }

    private void RecordTrial(
        string minigameId,
        ProgressionManager.LevelResult result,
        bool isCorrect,
        int wrongAttempts,
        int completionMs,
        List<int> targets)
    {
        sessionData.Add(new TrialRecord
        {
            minigame_id = minigameId,
            day = currentLevel,
            level_number = currentLevel,
            trial_index = trialIndexInLevel + 1,

            span = goalCollected,
            target_sequence = targets,
            sequence_recalled = new List<int>(),

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

    private SWMTrialData NewTrialDataSkeleton()
    {
        var data = new SWMTrialData
        {
            trial_id =
                $"treasure_hunt_level{currentLevel}_trial{trialIndexInLevel + 1}",
            day = currentLevel,
            trial_index = trialIndexInLevel + 1,
            boxes = numBoxes,
            treasures = goalCollected
        };

        for (int i = 0; i < numBoxes; i++)
        {
            if (pool[i] == null)
                continue;

            RectTransform rectTransform =
                pool[i].GetComponent<RectTransform>();

            if (rectTransform == null)
                continue;

            data.box_positions.Add(new SWMBoxPos
            {
                box_id = i,
                x = rectTransform.anchoredPosition.x,
                y = rectTransform.anchoredPosition.y
            });
        }

        return data;
    }

    private void RecordSelection(
        int boxId,
        string outcome,
        int elapsedMs)
    {
        if (currentData == null)
            currentData = NewTrialDataSkeleton();

        currentData.search_sequence.Add(new SWMSelection
        {
            box_id = boxId,
            outcome = outcome,
            timestamp_ms = elapsedMs
        });
    }

    private static HashSet<int> PickUniqueIndices(int count, int selectedCount)
    {
        var indices = new List<int>(count);

        for (int i = 0; i < count; i++)
            indices.Add(i);

        for (int i = 0; i < selectedCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, count);
            (indices[i], indices[randomIndex]) =
                (indices[randomIndex], indices[i]);
        }

        var result = new HashSet<int>();

        for (int i = 0; i < selectedCount; i++)
            result.Add(indices[i]);

        return result;
    }

    private static HashSet<int> PickUniqueIndices(int count, int selectedCount, HashSet<int> excluded)
    {
        var candidates = new List<int>(count);

        for (int i = 0; i < count; i++)
        {
            if (!excluded.Contains(i))
                candidates.Add(i);
        }

        // Safety net: if every chest has already held the treasure
        // (e.g. trials > boxes), reset and allow reuse rather than crashing.
        if (candidates.Count < selectedCount)
        {
            excluded.Clear();
            candidates.Clear();

            for (int i = 0; i < count; i++)
                candidates.Add(i);
        }

        for (int i = 0; i < selectedCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, candidates.Count);
            (candidates[i], candidates[randomIndex]) =
                (candidates[randomIndex], candidates[i]);
        }

        var result = new HashSet<int>();

        for (int i = 0; i < selectedCount; i++)
            result.Add(candidates[i]);

        return result;
    }

    private int GetSelectedStartingLevel()
    {
        var data = PlayerDataManager.Instance.Data;

        int unlockedLevel = Mathf.Clamp(
            data.swmLevel,
            1,
            GetTotalLevels()
        );

        int requestedLevel = PlayerPrefs.GetInt(
            SelectedLevelKey,
            unlockedLevel
        );

        return Mathf.Clamp(requestedLevel, 1, unlockedLevel);
    }

    private int GetTotalLevels()
    {
        if (config != null &&
            config.levels != null &&
            config.levels.Length > 0)
        {
            return config.levels.Length;
        }

        return 1;
    }
}
