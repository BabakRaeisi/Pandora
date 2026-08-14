using System;

[Serializable]
public class LevelCompletionRecord
{
    // Unique ID so retries cannot create duplicate database/sheet rows.
    public string eventId;

    public string playerId;

    public string minigame;
    public int levelNumber;

    // Example: 7 / 8
    public int successfulTrials;
    public int requiredTrials;

    public bool normalPass;
    public bool assistedPass;

    // Actual active gameplay time, not menu/app-open time.
    public int activeDurationMs;

    public string startedAtUtc;
    public string completedAtUtc;
}