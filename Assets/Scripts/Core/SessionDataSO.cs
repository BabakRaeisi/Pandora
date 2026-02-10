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