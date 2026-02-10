using System;
using System.Collections.Generic;
using UnityEngine;

public class SWMGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ChestSpawnerRandom spawner;
    [SerializeField] private SWMHUD hud;
    [SerializeField] private SWMConfig config;

    // REPLACED: recorder -> sessionData
    [Header("Session Data")]
    [SerializeField] private SessionDataSO sessionData;

    [Header("Protocol Day (1..7)")]
    [SerializeField, Range(1, 7)] private int day = 1;

    private SWMConfig.DayConfig dayCfg;
    private int trialIndex;

    // Pool
    private List<SWMChest> pool = new();
    private int poolSize;

    // Stable IDs for chests (since SetIndex/Index is not guaranteed)
    private readonly Dictionary<SWMChest, int> chestId = new();

    // Trial params
    private int numBoxes;
    private int goalCollected;
    private HashSet<int> treasureIndices = new();

    // Trial runtime
    private float trialStartTime;
    private float firstClickTime = -1f;
    private bool trialComplete;

    private int collectedFound;
    private int betweenErrors;
    private int withinErrors;
    private int totalSelections;

    // Keeping your SWMTrialData code in place (not used for session now)
    private SWMTrialData currentData;

    void Start() => StartDay(day);

    public void StartDay(int dayNumber)
    {
        day = Mathf.Clamp(dayNumber, 1, 7);
        dayCfg = config.GetDay(day);

       

        if (sessionData == null)
        {
            Debug.LogError("SWMGameManager: SessionDataSO is NOT assigned.");
            return;
        }

        trialIndex = 0;

        hud?.SetupDay(dayCfg.trials);
        hud?.SetTrialsDone(0);

        // Spawn ONCE for the day (max boxes for that day)
        poolSize = Mathf.Clamp(dayCfg.boxes, 3, 12);

        // Destroy previous pool (if restarting day)
        for (int i = 0; i < pool.Count; i++)
            if (pool[i]) Destroy(pool[i].gameObject);
        pool.Clear();
        chestId.Clear();

        pool = spawner.SpawnPool(poolSize, this);

        // Assign stable ids 0..poolSize-1 (NO SetIndex)
        for (int i = 0; i < pool.Count; i++)
            if (pool[i]) chestId[pool[i]] = i;

        StartNextTrial();
    }

    public void StartNextTrial()
    {
        if (trialIndex >= dayCfg.trials)
        {
            // recorder?.SaveDay(day);  <-- REMOVED
            hud?.ShowDayComplete();
            return;
        }

        trialComplete = false;
        firstClickTime = -1f;

        numBoxes = Mathf.Clamp(dayCfg.boxes, 3, poolSize);
        goalCollected = Mathf.Clamp(dayCfg.treasures, 1, numBoxes);

        collectedFound = 0;
        betweenErrors = 0;
        withinErrors = 0;
        totalSelections = 0;

        hud?.SetupTrial(goalCollected);
        hud?.SetCollectedFound(0);

        // Reposition the SAME chests each trial (and activate only first numBoxes)
        spawner.Reposition(pool, numBoxes);

        // Pick treasure indices among ACTIVE boxes only (0..numBoxes-1)
        treasureIndices = PickUniqueIndices(numBoxes, goalCollected);

        // Reset only active chests (0..numBoxes-1)
        for (int i = 0; i < numBoxes; i++)
        {
            if (!pool[i]) continue;
            bool hasTreasure = treasureIndices.Contains(i);
            pool[i].ResetForTrial(hasTreasure);
        }

        trialStartTime = Time.time;
        currentData = NewTrialDataSkeleton(); // kept, but no longer saved to recorder
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

            if (chest.HasTreasure)
            {
                collectedFound++;
                hud?.SetCollectedFound(collectedFound);
                RecordSelection(id, "treasure", tMs);

                if (collectedFound >= goalCollected)
                    CompleteTrial();
            }
            else
            {
                RecordSelection(id, "empty", tMs);
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

        // recorder?.AddTrial(currentData);  <-- REMOVED
        currentData = null;

        //   WRITE UNIVERSAL RECORD TO SessionDataSO (same as Constellation)
        // Define "wrong attempts" for SWM as total errors:
        int wrongAttempts = betweenErrors + withinErrors;

        // Define "span" for SWM as treasures to find (difficulty driver)
        int span = goalCollected;

        // Define "target_sequence" for SWM as the treasure indices (which boxes were correct targets)
        var targets = new List<int>(treasureIndices);
        targets.Sort();

        sessionData.Add(new TrialRecord
        {
            minigame_id = "SWM",
            day = day,
            trial_index = trialIndex + 1,
            span = span,
            target_sequence = targets,
            wrong_attempts = wrongAttempts,
            completion_time_ms = completionMs,
            timestamp_iso = DateTime.UtcNow.ToString("o")
        });

        trialIndex++;
        hud?.SetTrialsDone(trialIndex);
        hud?.ShowTrialComplete();
    }

    private SWMTrialData NewTrialDataSkeleton()
    {
        var data = new SWMTrialData
        {
            trial_id = $"treasure_hunt_day{day}_trial{trialIndex + 1}",
            day = day,
            trial_index = trialIndex + 1,
            boxes = numBoxes,
            treasures = goalCollected,
        };

        // record positions of active boxes only
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
            int j =   UnityEngine.Random.Range(i, n);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var result = new HashSet<int>();
        for (int i = 0; i < k; i++) result.Add(pool[i]);
        return result;
    }
}
