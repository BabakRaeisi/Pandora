using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstellationGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ConstellationController controller;
    [SerializeField] private ConstellationHUD hud;
    [SerializeField] private ConstellationConfigSO config;

    [Header("Failure UI")]
    [SerializeField] private TimedPanel wrongPatternPanel;  // your existing message box
    [SerializeField] private float wrongPanelSeconds = 1.5f;

    [Header("Protocol Day (1..7)")]
    [SerializeField, Range(1, 7)] private int day = 1;

    private ConstellationConfigSO.DayConfig dayCfg;

    // counts ONLY successful trials
    private int successCount;

    // prevents double finish firing
    private bool busy;

    void Start()
    {
        if (!controller || !hud || !config)
        {
            Debug.LogError("ConstellationGameManager: Assign controller, hud, and config in Inspector.");
            return;
        }

        controller.OnTrialFinished += HandleTrialFinished;
        StartDay(day);
    }

    public void StartDay(int dayNumber)
    {
        day = Mathf.Clamp(dayNumber, 1, 7);

        dayCfg = config.GetDay(day);
        if (dayCfg == null)
        {
            Debug.LogError($"ConstellationGameManager: No DayConfig found for day {day}.");
            return;
        }

        successCount = 0;
        busy = false;

        hud.SetupDay(dayCfg.trials);
        hud.SetTrialsDone(0);

        StartNewTrial();
    }

    private void StartNewTrial()
    {
        hud.SetupTrial();

        int[] sequence = GenerateUniqueSequence(dayCfg.span);
        controller.BeginTrial(sequence, dayCfg.starOnSeconds, dayCfg.gapSeconds);
    }

    private void HandleTrialFinished(bool success)
    {
        if (busy) return;
        busy = true;

        if (success)
        {
            successCount++;
            hud.SetTrialsDone(successCount);

            if (successCount >= dayCfg.trials)
            {
                hud.ShowDayComplete();
                busy = false;
                return;
            }

            // success: wait for player to press Next
            hud.ShowTrialComplete();
            busy = false;
        }
        else
        {
            // fail: show panel briefly then reset the trial automatically
            StartCoroutine(FailRoutine());
        }
    }

    private IEnumerator FailRoutine()
    {
        // show your error message box briefly
        if (wrongPatternPanel)
        {
            // Try common APIs:
            // 1) wrongPatternPanel.Show(wrongPanelSeconds);
            // 2) wrongPatternPanel.Show();
            // If #1 doesn't compile, switch to #2.

            wrongPatternPanel.Show( );
        }

        // small pause so user sees the feedback
        yield return new WaitForSeconds(wrongPanelSeconds);

        // reset trial immediately
        controller.ResetAll();
        StartNewTrial();

        busy = false;
    }

    // Hook this to your Footer Next Trial button OnClick (only used on success)
    public void OnNextTrialButton()
    {
        if (busy) return;
        if (dayCfg != null && successCount >= dayCfg.trials) return;

        StartNewTrial();
    }

    private static int[] GenerateUniqueSequence(int span)
    {
        span = Mathf.Clamp(span, 1, 9);

        List<int> pool = new List<int>(9);
        for (int i = 1; i <= 9; i++) pool.Add(i);

        for (int i = 0; i < span; i++)
        {
            int j = Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int[] seq = new int[span];
        for (int i = 0; i < span; i++) seq[i] = pool[i];
        return seq;
    }
}
