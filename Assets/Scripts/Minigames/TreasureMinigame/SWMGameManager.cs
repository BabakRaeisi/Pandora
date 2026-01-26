// SWMGameManager.cs
// COMPLETE version (Day -> multiple Trials -> footer trial stars -> return to main map)
// Works with:
// - SWMChest.cs (the version that: RevealFirstTime(), RevealAgain(), closes after brief reveal)
// - SWMHUD.cs (the version that has: SetupDay(), SetupTrial(), SetTrialsProgress(), ShowTrialComplete(), ShowDayComplete(), AddBetweenError())
// - SWMSessionRecorder.cs (optional, saves day JSON)
// - SWMConfig.cs ScriptableObject (day table)
//
// Behavior you requested:
// - Chests open briefly then close (even treasure)
// - Re-clicking an already-opened chest (EMPTY or TREASURE):
//      -> chest briefly opens again
//      -> warning panel/icon shows (through HUD)
//      -> error icon added
//      -> adds error count (EMPTY => BetweenErrors, TREASURE => WithinErrors)
// - Footer stars show trial progress (gray remaining, gold done)
// - After last trial of the day:
//      -> Next Trial button is replaced by Return-to-Map button (HUD.ShowDayComplete())
//      -> day JSON saved (if recorder assigned)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private int goalTreasures;

    // --- runtime ---
    private readonly List<SWMChest> chests = new();
    private HashSet<int> treasureIndices = new();

    private float trialStartTime;
    private float firstClickTime = -1f;
    private bool trialComplete;

    // --- metrics (current trial) ---
    private int treasuresFound;
    private int betweenErrors;
    private int withinErrors;
    private int totalSelections;

    // --- data (current trial) ---
    private SWMTrialData currentData;

    void Start()
    {
        StartDay(day);
    }

    // Called at scene start or from menu
    public void StartDay(int dayNumber)
    {
        if (config == null)
        {
            Debug.LogError("SWMGameManager: SWMConfig is not assigned.");
            return;
        }
        if (spawner == null)
        {
            Debug.LogError("SWMGameManager: ChestSpawnerRandom is not assigned.");
            return;
        }

        day = Mathf.Clamp(dayNumber, 1, 7);
        dayCfg = config.GetDay(day);

        trialIndex = 0;

        hud?.SetupDay(dayCfg.trials);
        hud?.SetTrialsProgress(0);

        StartNextTrial();
    }

    // Button: Next Trial
    public void StartNextTrial()
    {
        // If day finished: save + show "Return to Map" button
        if (trialIndex >= dayCfg.trials)
        {
            recorder?.SaveDay(day);
            hud?.ShowDayComplete();
            return;
        }

        trialComplete = false;
        firstClickTime = -1f;

        numBoxes = Mathf.Clamp(dayCfg.boxes, 3, 12);
        goalTreasures = Mathf.Clamp(dayCfg.treasures, 1, numBoxes);

        // Reset trial metrics
        treasuresFound = 0;
        betweenErrors = 0;
        withinErrors = 0;
        totalSelections = 0;

        // HUD
        hud?.SetupTrial(goalTreasures);
        hud?.SetTreasuresFound(0);

        // Spawn chests
        chests.Clear();
        chests.AddRange(spawner.Spawn(numBoxes, this));

        // Pick treasure placements
        treasureIndices = PickUniqueIndices(numBoxes, goalTreasures);

        // Assign chest state
        for (int i = 0; i < chests.Count; i++)
        {
            if (!chests[i]) continue;
            bool hasTreasure = treasureIndices.Contains(chests[i].Index);
            chests[i].ResetForTrial(hasTreasure);
        }

        // Start trial time
        trialStartTime = Time.time;

        // Create trial data skeleton (for logging)
        currentData = NewTrialDataSkeleton();
    }

    public void OnChestPressed(SWMChest chest)
    {
        if (trialComplete || chest == null) return;

        if (firstClickTime < 0f) firstClickTime = Time.time;

        totalSelections++;

        int tMs = Mathf.RoundToInt((Time.time - trialStartTime) * 1000f);

        // UNOPENED: reveal first time
        if (chest.State == SWMChest.ChestState.Unopened)
        {
            chest.RevealFirstTime();

            if (chest.HasTreasure)
            {
                treasuresFound++;
                hud?.SetTreasuresFound(treasuresFound);
                RecordSelection(chest.Index, "treasure", tMs);

                if (treasuresFound >= goalTreasures)
                {
                    CompleteTrial();
                }
            }
            else
            {
                RecordSelection(chest.Index, "empty", tMs);
            }

            return;
        }

        // ALREADY OPENED (EMPTY or TREASURE):
        // - brief open again
        // - show warning + add error icon
        chest.RevealAgain();
        hud?.AddBetweenError(); // this also shows your warning panel/icon

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

        // finalize trial data
        if (currentData == null) currentData = NewTrialDataSkeleton();

        currentData.between_errors = betweenErrors;
        currentData.within_errors = withinErrors;
        currentData.total_selections = totalSelections;
        currentData.completion_time_ms = completionMs;
        currentData.first_click_latency_ms = firstClickLatencyMs;

        recorder?.AddTrial(currentData);
        currentData = null;

        // Update footer stars: (trialIndex is 0-based, so done = trialIndex+1)
        hud?.SetTrialsProgress(trialIndex + 1);

        // Show completion indicator + Next Trial button
        hud?.ShowTrialComplete();

        // Advance to next trial index (next click on Next Trial starts it)
        trialIndex++;
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
            treasures = goalTreasures,
        };

        // record positions (RectTransform anchored positions)
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
