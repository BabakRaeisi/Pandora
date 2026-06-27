using System;
using System.Collections.Generic;

// ── Request ────────────────────────────────────────────────────────────────

[Serializable]
public class ProfileRegisterRequest
{
    public string phoneNumber;
    public string playerName;
    public int    age;
    public int    avatarIndex;
    public string gender;

    public ProfileRegisterRequest(PlayerProfile p)
    {
        phoneNumber = p.phoneNumber;
        playerName  = p.playerName;
        age         = p.age;
        avatarIndex = p.avatarIndex;
        gender      = p.gender;
    }
}

// ── Response ───────────────────────────────────────────────────────────────

[Serializable]
public class ProfileRestoreResponse
{
    public bool            isExistingPlayer;
    public PlayerFullData  playerData;
}

[Serializable]
public class PlayerFullData
{
    public ProfileDetail       profile;
    public List<SessionDetail> sessions;
}

[Serializable]
public class ProfileDetail
{
    public string phoneNumber;
    public string playerName;
    public int    age;
    public int    avatarIndex;
    public string gender;
}

[Serializable]
public class SessionDetail
{
    public int            sessionId;
    public int            day;
    public string         completedAt;

    // ── Legacy fields (kept for server backward compatibility) ────────────────
    public int            miniGamesCompletedToday;
    public int            trialsCompletedInCurrentGame;
    public bool           bridgeCompletedToday;
    public bool           constellationCompletedToday;
    public bool           swmCompletedToday;

    // ── Program state ─────────────────────────────────────────────────────────
    public bool           programCompleted;
    public bool           profileCompleted;
    public string         lastDayCompletionTime;
    public string         constellationLastLevelCompletionTime;
    public string         constellationLockUntilTime;
    public int            constellationLockLevel;

    // ── Per-minigame level progress ───────────────────────────────────────────
    public int            constellationLevel;
    public int            bridgeLevel;
    public int            swmLevel;

    // ── Unlock gates ──────────────────────────────────────────────────────────
    public bool           bridgeUnlocked;
    public bool           swmUnlocked;

    // ── Completion tiers ──────────────────────────────────────────────────────
    public bool           constellationGateReached;
    public bool           bridgeGateReached;
    public bool           swmGateReached;
    public bool           constellationPerfect;
    public bool           bridgePerfect;
    public bool           swmPerfect;

    // ── Session level play counters ───────────────────────────────────────────
    public int            constellationLevelsPlayedThisSession;
    public int            bridgeLevelsPlayedThisSession;
    public int            swmLevelsPlayedThisSession;

    public List<TrialRecord> trials;
}
