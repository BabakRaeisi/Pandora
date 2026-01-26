using System;
using System.Collections.Generic;

[Serializable]
public class SWMTrialData
{
    public string trial_id;
    public int day;
    public int trial_index;

    public int boxes;
    public int treasures;

    public int between_errors;
    public int within_errors;
    public int total_selections;
    public int completion_time_ms;
    public int first_click_latency_ms;

    public List<SWMSelection> search_sequence = new();
    public List<SWMBoxPos> box_positions = new();
}

[Serializable]
public class SWMSelection
{
    public int box_id;
    public string outcome; // "treasure" | "empty" | "between_error" | "within_error"
    public int timestamp_ms;
}

[Serializable]
public class SWMBoxPos
{
    public int box_id;
    public float x;
    public float y;
}
