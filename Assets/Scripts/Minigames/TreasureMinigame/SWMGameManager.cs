// SWMGameManager.cs (only the parts you need to change/add)
// 1) Call hud.SetupDay(dayCfg.trials) when the day starts
// 2) Update footer stars after each trial completes
// 3) When day ends: hud.ShowDayComplete() and show ReturnToMenu button instead of Next Trial
// 4) Add ReturnToMainMenu() method for the button to call

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SWMGameManager : MonoBehaviour
{
    [System.Serializable]
    public struct DayConfig
    {
        public int day;
        public int boxes;
        public int treasures;
        public int trials;
    }

    [Header("Refs")]
    [SerializeField] private ChestSpawnerRandom spawner;
    [SerializeField] private SWMHUD hud;
    [SerializeField] private SWMSessionRecorder recorder;

    [Header("Main Menu Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Week 1 Day (1..7)")]
    [SerializeField, Range(1, 7)] private int day = 1;

    [Header("Day Table")]
    [SerializeField]
    private DayConfig[] dayConfigs = new DayConfig[7]
    {
        new DayConfig{ day=1, boxes=3, treasures=2, trials=5 },
        new DayConfig{ day=2, boxes=4, treasures=2, trials=5 },
        new DayConfig{ day=3, boxes=4, treasures=3, trials=6 },
        new DayConfig{ day=4, boxes=6, treasures=3, trials=6 },
        new DayConfig{ day=5, boxes=6, treasures=4, trials=7 },
        new DayConfig{ day=6, boxes=6, treasures=4, trials=7 },
        new DayConfig{ day=7, boxes=8, treasures=4, trials=8 },
    };

    private DayConfig dayCfg;
    private int trialIndex;     // 0-based
    private int goalTreasures;

    private readonly List<SWMChest> chests = new();
    private HashSet<int> treasureIndices = new();

    private float trialStartTime;
    private bool trialComplete;

    private int treasuresFound;
    private int betweenErrors;
    private int withinErrors;
    private int totalSelections;

    void Start()
    {
        StartDay(day);
    }

    public void StartDay(int dayNumber)
    {
        day = Mathf.Clamp(dayNumber, 1, 7);
        dayCfg = GetDayCfg(day);

        trialIndex = 0;

        hud?.SetupDay(dayCfg.trials);
        hud?.SetTrialsProgress(0);

        StartNextTrial();
    }

    public void StartNextTrial()
    {
        // Day finished
        if (trialIndex >= dayCfg.trials)
        {
            recorder?.SaveDay(day);
            hud?.ShowDayComplete();
            return;
        }

        trialComplete = false;

        int boxes = Mathf.Clamp(dayCfg.boxes, 3, 12);
        goalTreasures = Mathf.Clamp(dayCfg.treasures, 1, boxes);

        treasuresFound = 0;
        betweenErrors = 0;
        withinErrors = 0;
        totalSelections = 0;

        hud?.SetupTrial(goalTreasures);

        chests.Clear();
        chests.AddRange(spawner.Spawn(boxes, this));

        treasureIndices = PickUniqueIndices(boxes, goalTreasures);

        for (int i = 0; i < chests.Count; i++)
        {
            if (!chests[i]) continue;
            bool hasTreasure = treasureIndices.Contains(chests[i].Index);
            chests[i].ResetForTrial(hasTreasure);
        }

        trialStartTime = Time.time;
    }

    public void OnChestPressed(SWMChest chest)
    {
        if (trialComplete || chest == null) return;

        totalSelections++;

        if (chest.State == SWMChest.ChestState.Unopened)
        {
            chest.Reveal();

            if (chest.HasTreasure)
            {
                treasuresFound++;
                hud?.SetTreasuresFound(treasuresFound);

                if (treasuresFound >= goalTreasures)
                    CompleteTrial();
            }

            return;
        }

        if (chest.State == SWMChest.ChestState.Empty)
        {
            betweenErrors++;
            hud?.AddBetweenError();
            return;
        }

        if (chest.State == SWMChest.ChestState.Treasure)
        {
            withinErrors++;
            return;
        }
    }

    void CompleteTrial()
    {
        trialComplete = true;

        // Update footer stars: trialIndex is current trial, so "done" = trialIndex+1
        hud?.SetTrialsProgress(trialIndex + 1);

        // show completion + next trial button
        hud?.ShowTrialComplete();

        // record/save trial here if you're already doing that in your version...

        trialIndex++;
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    DayConfig GetDayCfg(int dayNumber)
    {
        for (int i = 0; i < dayConfigs.Length; i++)
            if (dayConfigs[i].day == dayNumber) return dayConfigs[i];

        return dayConfigs[Mathf.Clamp(dayNumber - 1, 0, dayConfigs.Length - 1)];
    }

    static HashSet<int> PickUniqueIndices(int n, int k)
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
