// SWMGameManager.cs
// Updated to match the NEW thin SWMHUD (widgets-based):
// SWMHUD methods used now:
// - SetupDay(int totalTrials)
// - SetupTrial(int goalCollected)
// - SetTrialsDone(int done)
// - SetCollectedFound(int found)
// - AddErrorAndWarn()
// - ShowTrialComplete()
// - ShowDayComplete()
//
// Works with:
// - SWMChest.cs (RevealFirstTime(), RevealAgain())
// - SWMConfig.cs
// - SWMSessionRecorder.cs (optional)
// NO SceneManager stuff here; your SceneFlow handles return-to-menu button.

using System.Collections.Generic;
using UnityEngine;

public class SWMGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ChestSpawnerRandom spawner;
    [SerializeField] private SWMHUD hud;
    [SerializeField] private SWMSessionRecorder recorder;
    [SerializeField] private SWMConfig config;

    [Header("Protocol Day (1..7)")]
    [SerializeField, Range(1, 7)] private int day = 1;

    // --- current day config ---
    private SWMConfig.DayConfig dayCfg;
    private int trialIndex; // 0-based (0..trials-1)

    // --- current trial settings ---
    private int numBoxes;
    private int goalCollected; // SWM: treasures required

    // --- runtime ---
    private readonly List<SWMChest> chests = new();
    private HashSet<int> treasureIndices = new();

    private float trialStartTime;
    private float firstClickTime = -1f;
    private bool trialComplete;

    // --- metrics (current trial) ---
    private int collectedFound;   // SWM: treasuresFound
    private int betweenErrors;
    private int withinErrors;
    private int totalSelections;

    // --- data (current trial) ---
    private SWMTrialData currentData;

    void Start()
    {
        StartDay(day);
    }

    public void StartDay(int dayNumber)
    {
        if (!config)
        {
            Debug.LogError("SWMGameManager: SWMConfig is not assigned.");
            return;
        }
        if (!spawner)
        {
            Debug.LogError("SWMGameManager: ChestSpawnerRandom is not assigned.");
            return;
        }

        day = Mathf.Clamp(dayNumber, 1, 7);
        dayCfg = config.GetDay(day);

        trialIndex = 0;

        hud?.SetupDay(dayCfg.trials);
        hud?.SetTrialsDone(0);

        StartNextTrial();
    }

    // Button: Next Trial
    public void StartNextTrial()
    {
        // Day finished: save + show return-to-menu button (HUD side)
        if (trialIndex >= dayCfg.trials)
        {
            recorder?.SaveDay(day);
            hud?.ShowDayComplete();
            return;
        }

        trialComplete = false;
        firstClickTime = -1f;

        numBoxes = Mathf.Clamp(dayCfg.boxes, 3, 12);
        goalCollected = Mathf.Clamp(dayCfg.treasures, 1, numBoxes);

        // Reset trial metrics
        collectedFound = 0;
        betweenErrors = 0;
        withinErrors = 0;
        totalSelections = 0;

        // HUD (generic collected bar)
        hud?.SetupTrial(goalCollected);
        hud?.SetCollectedFound(0);

        // Spawn chests
        chests.Clear();
        chests.AddRange(spawner.Spawn(numBoxes, this));

        // Pick treasure placements
        treasureIndices = PickUniqueIndices(numBoxes, goalCollected);

        // Assign chest state
        for (int i = 0; i < chests.Count; i++)
        {
            if (!chests[i]) continue;
            bool hasTreasure = treasureIndices.Contains(chests[i].Index);
            chests[i].ResetForTrial(hasTreasure);
        }

        trialStartTime = Time.time;
        currentData = NewTrialDataSkeleton();
    }

    public void OnChestPressed(SWMChest chest)
    {
        if (trialComplete || chest == null) return;

        if (firstClickTime < 0f) firstClickTime = Time.time;

        totalSelections++;
        int tMs = Mathf.RoundToInt((Time.time - trialStartTime) * 1000f);

        // UNOPENED
        if (chest.State == SWMChest.ChestState.Unopened)
        {
            chest.RevealFirstTime();

            if (chest.HasTreasure)
            {
                collectedFound++;
                hud?.SetCollectedFound(collectedFound);
                RecordSelection(chest.Index, "treasure", tMs);

                if (collectedFound >= goalCollected)
                    CompleteTrial();
            }
            else
            {
                RecordSelection(chest.Index, "empty", tMs);
            }

            return;
        }

        // ALREADY OPENED (EMPTY or TREASURE):
        // - brief open again
        // - warning panel + error icon
        chest.RevealAgain();
        hud?.AddErrorAndWarn();

        if (chest.State == SWMChest.ChestState.Empty)
        {
            betweenErrors++;
            RecordSelection(chest.Index, "between_error", tMs);
        }
        else if (chest.State == SWMChest.ChestState.Treasure)
        {
            withinErrors++;
            RecordSelection(chest.Index, "within_error", tMs);
        }
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

        recorder?.AddTrial(currentData);
        currentData = null;

        //  MOVE THIS UP
        trialIndex++;

        // NOW update footer stars
        hud?.SetTrialsDone(trialIndex);

        // Completion + Next Trial button
        hud?.ShowTrialComplete();
    }
    // ----------------- Data helpers -----------------

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

        for (int i = 0; i < chests.Count; i++)
        {
            if (!chests[i]) continue;
            var rt = chests[i].GetComponent<RectTransform>();
            if (!rt) continue;

            data.box_positions.Add(new SWMBoxPos
            {
                box_id = chests[i].Index,
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

    // ----------------- Random selection helper -----------------

    private static HashSet<int> PickUniqueIndices(int n, int k)
    {
        var pool = new List<int>(n);
        for (int i = 0; i < n; i++) pool.Add(i);

        for (int i = 0; i < k; i++)
        {
            int j = Random.Range(i, n);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var result = new HashSet<int>();
        for (int i = 0; i < k; i++) result.Add(pool[i]);
        return result;
    }
}
