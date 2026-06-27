using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    public PlayerProfile profile;

    // ── Legacy fields (kept for backward compatibility; removed in Phase 6/7) ──
    public int currentDay = 1;
    public int miniGamesCompletedToday = 0;
    public int trialsCompletedInCurrentGame = 0;
    public bool bridgeCompletedToday;
    public bool constellationCompletedToday;
    public bool swmCompletedToday;

    // ── Program completion ────────────────────────────────────────────────────
    public bool programCompleted;
    public bool profileCompleted = false;

    // ── Session timing ────────────────────────────────────────────────────────
    public string lastDayCompletionTime;

    // ── Constellation level lock timing ──────────────────────────────────────
    // Stored as ISO-8601 UTC timestamps for lock checks and countdown display.
    public string constellationLastLevelCompletionTime;
    public string constellationLockUntilTime;
    public int constellationLockLevel = 0;

    // ── Per-minigame level progress (1-based; 1 = not started, 16 = all done) ─
    public int constellationLevel = 1;
    public int bridgeLevel        = 1;
    public int swmLevel           = 1;

    // ── Minigame unlock gates ─────────────────────────────────────────────────
    // Bridge unlocks when constellationLevel >= 8 is completed.
    // SWM unlocks when bridgeLevel >= 8 is completed.
    public bool bridgeUnlocked = false;
    public bool swmUnlocked    = false;

    // ── Per-minigame completion tiers ─────────────────────────────────────────
    // "gate" = level 8 reached; "perfect" = all 16 levels completed.
    public bool constellationGateReached  = false;
    public bool bridgeGateReached         = false;
    public bool swmGateReached            = false;

    public bool constellationPerfect = false;
    public bool bridgePerfect        = false;
    public bool swmPerfect           = false;

    // ── Same-session daily slots ──────────────────────────────────────────────
    // Set to true when the player plays a level in that minigame this session.
    // Cleared on ResetDailyProgress (after 6-hour cooldown).
    public bool constellationPlayedToday = false;
    public bool bridgePlayedToday        = false;
    public bool swmPlayedToday           = false;

    // ── Bonus level slots (one extra level unlocked by strong performance) ─────
    public bool bonusLevelAvailableConstellation = false;
    public bool bonusLevelAvailableBridge        = false;
    public bool bonusLevelAvailableSwm           = false;

    // ── Session level play counters ───────────────────────────────────────────
    // Incremented each time a level is completed in a session.
    // Reset on the 6-hour cooldown. ProgressionManager enforces the per-session
    // cap (2 levels for Bridge and SWM; no hard cap on Constellation).
    public int constellationLevelsPlayedThisSession = 0;
    public int bridgeLevelsPlayedThisSession        = 0;
    public int swmLevelsPlayedThisSession           = 0;

    // ── Achievements ──────────────────────────────────────────────────────────
    public List<string> unlockedAchievementIds = new();
    
}