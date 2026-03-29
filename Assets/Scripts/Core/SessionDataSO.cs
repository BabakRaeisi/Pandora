using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Session/Session Data", fileName = "SessionData")]
public class SessionDataSO : ScriptableObject
{
    public List<TrialRecord> trials = new();

    public void Clear()
    {
        trials.Clear();
    }

    public void Add(TrialRecord record)
    {
        if (record == null)
            return;

        if (record.target_sequence == null)
            record.target_sequence = new List<int>();

        if (string.IsNullOrWhiteSpace(record.timestamp_iso))
            record.timestamp_iso = DateTime.UtcNow.ToString("o");

        trials.Add(record);
    }
}


[Serializable]
public class TrialRecord
{
    public string minigame_id;      // "Constellation", "SWM", etc.
    public int day;
    public int trial_index;

    public int span;
    public List<int> target_sequence;

    public int wrong_attempts;
    public int completion_time_ms;

    public string timestamp_iso;
}