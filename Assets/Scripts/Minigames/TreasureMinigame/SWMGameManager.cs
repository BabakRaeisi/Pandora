// SWMGameManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class SWMGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ChestSpawnerRandom spawner;
    [SerializeField] private SWMHUD hud;
    [SerializeField] private SWMConfig config;
    
    [Header("Session Data")]
    [SerializeField] private SessionDataSO sessionData;

    // ── Current level state ───────────────────────────────────────────────────
    private int currentLevel;
    private SWMConfig.LevelConfig levelCfg;

    // ── Trial state ───────────────────────────────────────────────────────────
    private int trialsCompleteInLevel;
    private int consecutiveFailsOnLevel;
    private int trialIndexInLevel;

    private List<SWMChest> pool = new();
    private int poolSize;

    private readonly Dictionary<SWMChest, int> chestId = new();

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

    void Start()
    {
        AudioManager.Instance.StopAll();
        AudioManager.Instance.Play("MapAmbient");
        AudioManager.Instance.Play("MusicLoop");

        StartLevel(PlayerDataManager.Instance.Data.swmLevel);
    }

    public void StartLevel(int levelNumber)
    {
        currentLevel = Mathf.Clamp(levelNumber, 1, ProgressionManager.MAX_LEVEL);
        levelCfg = config.GetLevel(currentLevel);

        if (sessionData == null)
        {
            Debug.LogError("[SWM] SessionDataSO is NOT assigned.");
            return;
        }

        levelStartTime = Time.time;
        trialsCompleteInLevel = 0;
        consecutiveFailsOnLevel = 0;
        trialIndexInLevel = 0;

        hud?.SetupDay(levelCfg.trials);
        hud?.SetTrialsDone(0);

        poolSize = Mathf.Clamp(levelCfg.boxes, 3, 12);

        for (int i = 0; i < pool.Count; i++)
            if (pool[i]) Destroy(pool[i].gameObject);
        pool.Clear();
        chestId.Clear();

        pool = spawner.SpawnPool(poolSize, this);

        for (int i = 0; i < pool.Count; i++)
            if (pool[i]) chestId[pool[i]] = i;

        StartNextTrial();
    }

    public void StartNextTrial()
    {
        if (trialsCompleteInLevel >= levelCfg.trials)
        {
            CompleteLevelAfterTrials();
            return;
        }

        trialComplete = false;
        firstClickTime = -1f;

        numBoxes = Mathf.Clamp(levelCfg.boxes, 3, poolSize);
        goalCollected = Mathf.Clamp(levelCfg.treasures, 1, numBoxes);

        collectedFound = 0;
        betweenErrors = 0;
        withinErrors = 0;
        totalSelections = 0;

        hud?.SetupTrial(goalCollected);
        hud?.SetCollectedFound(0);

        spawner.Reposition(pool, numBoxes);

        treasureIndices = PickUniqueIndices(numBoxes, goalCollected);

        for (int i = 0; i < numBoxes; i++)
        {
            if (!pool[i]) continue;
            bool hasTreasure = treasureIndices.Contains(i);
            pool[i].ResetForTrial(hasTreasure);
        }

        trialStartTime = Time.time;
        currentData = NewTrialDataSkeleton();
    }

    public void OnChestPressed(SWMChest chest)
    {
        if (trialComplete || chest == null) return;
        if (!chest.gameObject.activeInHierarchy) return;

        if (firstClickTime < 0f) firstClickTime = Time.time;

        totalSelections++;
        int tMs = Mathf.RoundToInt((Time.time - trialStartTime) * 1000f);

        int id = GetChestId(chest);

        if (chest.State == SWMChest.ChestState.Unopened)
        {
            chest.RevealFirstTime();
            AudioManager.Instance.Play("ChestOpen");
            if (chest.HasTreasure)
            {
                collectedFound++;
                hud?.SetCollectedFound(collectedFound);
                RecordSelection(id, "treasure", tMs);
                AudioManager.Instance.Play("Coin");
                if (collectedFound >= goalCollected)
                    CompleteTrial();
            }
            else
            {
                RecordSelection(id, "empty", tMs);
                AudioManager.Instance.Play("ChestClose");
            }

            return;
        }

        chest.RevealAgain();
        hud?.AddErrorAndWarn();

        if (chest.State == SWMChest.ChestState.Empty)
        {
            betweenErrors++;
            RecordSelection(id, "between_error", tMs);
        }
        else if (chest.State == SWMChest.ChestState.Treasure)
        {
            withinErrors++;
            RecordSelection(id, "within_error", tMs);
        }
    }

    private int GetChestId(SWMChest chest)
    {
        if (chestId.TryGetValue(chest, out int id)) return id;
        return pool.IndexOf(chest);
    }

    private void CompleteTrial()
    {
        trialComplete = true;

        int completionMs = Mathf.RoundToInt((Time.time - trialStartTime) * 1000f);
        int firstClickLatencyMs = (firstClickTime < 0f) ? 0 : Mathf.RoundToInt((firstClickTime - trialStartTime) * 1000f);

        if (currentData == null) currentData = NewTrialDataSkeleton();

        currentData.between_errors = betweenErrors;
        currentData.within_errors = withinErrors;
        currentData.total_selections = totalSelections;
        currentData.completion_time_ms = completionMs;
        currentData.first_click_latency_ms = firstClickLatencyMs;

        int wrongAttempts = betweenErrors + withinErrors;
        int span = goalCollected;

        // Evaluate this trial for scoring
        var trialResult = ProgressionManager.Instance.EvaluateTrial(
            "SWM",
            isCorrect: collectedFound == goalCollected,
            wrongAttempts: wrongAttempts,
            completionTimeMs: completionMs,
            span: span,
            consecutiveFails: consecutiveFailsOnLevel
        );

        var targets = new List<int>(treasureIndices);
        targets.Sort();

        // Record trial
        RecordTrial("SWM", trialResult, collectedFound == goalCollected, wrongAttempts, completionMs, targets);

        trialsCompleteInLevel++;
        hud?.SetTrialsDone(trialsCompleteInLevel);

        if (trialsCompleteInLevel >= levelCfg.trials)
        {
            CompleteLevelAfterTrials();
        }
        else
        {
            hud?.ShowTrialComplete();
        }

        currentData = null;
    }

    void CompleteLevelAfterTrials()
    {
        // Compute aggregate level performance
        int levelCompletionMs = Mathf.RoundToInt((Time.time - levelStartTime) * 1000f);
        float avgSpan = levelCfg.treasures;

        var levelResult = ProgressionManager.Instance.EvaluateTrial(
            "SWM",
            isCorrect: true,
            wrongAttempts: consecutiveFailsOnLevel,
            completionTimeMs: levelCompletionMs,
            span: Mathf.RoundToInt(avgSpan),
            consecutiveFails: 0
        );

        // Commit the level completion
        var finalResult = ProgressionManager.Instance.CompleteLevel("SWM", levelResult);
       
        Debug.Log($"[SWM] Level {currentLevel} completed. Score: {finalResult.score:F1}, Stars: {finalResult.stars}. " +
                  $"Cap: {finalResult.levelCapReached}");

        hud?.ShowDayComplete();

        if (finalResult.levelCapReached)
        {
            Debug.Log("[SWM] Session cap reached. No more levels available this session.");
        }

        if (finalResult.programCompletable)
        {
            Debug.Log("[SWM] Program is now completable! (All three minigames at gate level.)");
        }
    }

    
    void RecordTrial(string minigameId, ProgressionManager.LevelResult result, bool isCorrect, int wrongAttempts, int completionMs, List<int> targets)
    {
        sessionData.Add(new TrialRecord
        {
            minigame_id = minigameId,
            day = currentLevel,           // Legacy: store level as day
            level_number = currentLevel,
            trial_index = trialIndexInLevel + 1,

            span = goalCollected,
            target_sequence = targets,
            sequence_recalled = new List<int>(),  // SWM doesn't track this separately

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
            trial_id = $"treasure_hunt_level{currentLevel}_trial{trialIndexInLevel + 1}",
            day = currentLevel,
            trial_index = trialIndexInLevel + 1,
            boxes = numBoxes,
            treasures = goalCollected,
        };

        for (int i = 0; i < numBoxes; i++)
        {
            if (!pool[i]) continue;
            var rt = pool[i].GetComponent<RectTransform>();
            if (!rt) continue;

            data.box_positions.Add(new SWMBoxPos
            {
                box_id = i,
                x = rt.anchoredPosition.x,
                y = rt.anchoredPosition.y
            });
        }

        return data;
    }

    private void RecordSelection(int boxId, string outcome, int tMs)
    {
        if (currentData == null) currentData = NewTrialDataSkeleton();

        currentData.search_sequence.Add(new SWMSelection
        {
            box_id = boxId,
            outcome = outcome,
            timestamp_ms = tMs
        });
    }

    private static HashSet<int> PickUniqueIndices(int n, int k)
    {
        var pool = new List<int>(n);
        for (int i = 0; i < n; i++) pool.Add(i);

        for (int i = 0; i < k; i++)
        {
            int j = UnityEngine.Random.Range(i, n);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var result = new HashSet<int>();
        for (int i = 0; i < k; i++) result.Add(pool[i]);
        return result;
    }
}
