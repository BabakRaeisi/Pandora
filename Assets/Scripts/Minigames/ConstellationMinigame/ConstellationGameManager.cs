using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstellationGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ConstellationController controller;
    [SerializeField] private ConstellationHUD hud;
    [SerializeField] private ConstellationConfigSO config;
    [SerializeField] private TimedPanel wrongPatternPanel;

    [Header("Session Data")]
    [SerializeField] private SessionDataSO sessionData;

    [Header("Protocol Day")]
    [SerializeField, Range(1, 7)] private int day = 1;

    // Optional events (UI/debug/tools)
    public event Action TrialStarted;
    public event Action TrialFailed;
    public event Action TrialSucceeded;

    ConstellationConfigSO.DayConfig dayCfg;

    int successIndex;
    int wrongAttempts;
    bool busy;

    int[] currentSequence;
    HashSet<int> visibleSet;

    float trialStartTime;

    void Start()
    {
        controller.OnTrialFinished += HandleTrialFinished;
        StartDay(day);
    }

    public void StartDay(int dayNumber)
    {
        day = Mathf.Clamp(dayNumber, 1, 7);
        dayCfg = config.GetDay(day);

        if (dayCfg == null)
        {
            Debug.LogError($"No DayConfig for day {day}");
            return;
        }

        successIndex = 0;
        hud.SetupDay(dayCfg.trials);
        hud.SetTrialsDone(0);

        StartNewTrial();
    }

    void StartNewTrial()
    {
        wrongAttempts = 0;
        trialStartTime = Time.time;

        currentSequence = GenerateUniqueSequence(dayCfg.span);
        visibleSet = new HashSet<int>(currentSequence);

        TrialStarted?.Invoke();

        hud.SetupTrial();
        controller.ResetAll();
        controller.SetVisibleStars(visibleSet);
        controller.BeginTrial(currentSequence, dayCfg.starOnSeconds, dayCfg.gapSeconds);
    }

    public void ReplaySameTrial()
    {
        hud.SetupTrial();
        controller.ResetAll();
        controller.SetVisibleStars(visibleSet);
        controller.BeginTrial(currentSequence, dayCfg.starOnSeconds, dayCfg.gapSeconds);
    }

    void HandleTrialFinished(bool success)
    {
        if (busy) return;
        busy = true;

        int trialIndex1Based = successIndex + 1;

        if (success)
        {
            int durationMs = Mathf.RoundToInt((Time.time - trialStartTime) * 1000f);

          
            //   WRITE TO SESSION DATA
            sessionData.Add(new TrialRecord
            {
                minigame_id = "Constellation",
                day = day,
                trial_index = trialIndex1Based,
                span = dayCfg.span,
                target_sequence = new List<int>(currentSequence),
                wrong_attempts = wrongAttempts,
                completion_time_ms = durationMs,
                timestamp_iso = DateTime.UtcNow.ToString("o")
            });

            TrialSucceeded?.Invoke();

            successIndex++;
            hud.SetTrialsDone(successIndex);

            if (successIndex >= dayCfg.trials)
            {
                hud.ShowDayComplete();
                busy = false;
                return;
            }

            hud.ShowTrialComplete();
            busy = false;
        }
        else
        {
            wrongAttempts++;
            TrialFailed?.Invoke();

            if (wrongPatternPanel)
                wrongPatternPanel.Show();

            StartCoroutine(FailRoutine());
        }
    }

    IEnumerator FailRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        ReplaySameTrial();
        busy = false;
    }

    public void OnNextTrialButton()
    {
        if (busy) return;
        StartNewTrial();
    }

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
}
