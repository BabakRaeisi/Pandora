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
    public int            miniGamesCompletedToday;
    public int            trialsCompletedInCurrentGame;
    public bool           bridgeCompletedToday;
    public bool           constellationCompletedToday;
    public bool           swmCompletedToday;
    public bool           programCompleted;
    public bool           profileCompleted;
    public string         lastDayCompletionTime;
    public List<TrialRecord> trials;
}
