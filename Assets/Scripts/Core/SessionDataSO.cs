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
    // ── Identity ──────────────────────────────────────────────────────────────
    public string minigame_id;      // "Constellation", "Bridge", "SWM"
    public int    day;              // legacy; kept for server compat
    public int    level_number;     // 1-16
    public int    trial_index;      // trial within this level

    // ── Stimulus ──────────────────────────────────────────────────────────────
    public int          span;
    public List<int>    target_sequence;
    public List<int>    sequence_recalled;  // what the player actually entered

    // ── Raw performance ───────────────────────────────────────────────────────
    public bool is_correct;             // full correct recall in order
    public int  wrong_attempts;         // total wrong inputs this trial
    public int  completion_time_ms;     // time from stimulus end to last input
    public int  consecutive_fails;      // how many times this level was failed before this pass

    // ── Derived performance ───────────────────────────────────────────────────
    public float level_score;   // 0-100  (60% accuracy + 25% speed + 15% error control)
    public int   stars;         // 0-3
    public bool  passed;        // true if stars >= 1
    public bool  strong_pass;   // passed + 0 wrong + within expected time
    public bool  assisted_pass; // system granted pass after struggle threshold

    public string timestamp_iso;
}