using System.Collections.Generic;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerSaveData Data;

    [ContextMenu("Clear")]
    public void ClearSave()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "player_save.json");
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        Data = new PlayerSaveData();
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (SaveSystem.HasSave())
            Data = SaveSystem.Load();
        else
            Data = new PlayerSaveData();
    }

    public void Save()
    {
        SaveSystem.Save(Data);
    }

    public void SetProfile(PlayerProfile profile)
    {
        Data.profile = profile;
        Data.profileCompleted = true;
        Save();
    }

    /// <summary>
    /// Called after a successful POST /api/analytics/profile response.
    /// For existing players, restores progress from the latest session.
    /// For new players, initialises a fresh save.
    /// Phone number is always the identity key — if the returned phone number
    /// differs from any cached save, the local save is replaced.
    /// </summary>
    public void ApplyServerResponse(ProfileRestoreResponse response)
    {
        if (response == null) return;

        PlayerFullData serverData = response.playerData;
        if (serverData == null) return;

        // Build a PlayerProfile from the server's returned ProfileDetail.
        ProfileDetail pd = serverData.profile;
        var profile = new PlayerProfile
        {
            phoneNumber = pd?.phoneNumber ?? Data?.profile?.phoneNumber ?? "",
            playerName  = pd?.playerName  ?? "",
            age         = pd?.age         ?? 0,
            avatarIndex = pd?.avatarIndex ?? 0,
            gender      = pd?.gender      ?? ""
        };

        if (!response.isExistingPlayer || serverData.sessions == null || serverData.sessions.Count == 0)
        {
            // Brand-new player: start fresh.
            Data = new PlayerSaveData
            {
                profile          = profile,
                profileCompleted = true
            };
            Save();
            return;
        }

        // Existing player: restore from the latest session.
        SessionDetail latest = FindLatestSession(serverData.sessions);
        int restoredDay = ComputeRestoredCurrentDay(latest);

        Data = new PlayerSaveData
        {
            profile                      = profile,
            currentDay                   = restoredDay,
            miniGamesCompletedToday      = latest.miniGamesCompletedToday,
            trialsCompletedInCurrentGame = latest.trialsCompletedInCurrentGame,
            bridgeCompletedToday         = latest.bridgeCompletedToday,
            constellationCompletedToday  = latest.constellationCompletedToday,
            swmCompletedToday            = latest.swmCompletedToday,
            programCompleted             = latest.programCompleted,
            profileCompleted             = true,
            lastDayCompletionTime        = latest.lastDayCompletionTime
        };

        Save();
    }

    private static int ComputeRestoredCurrentDay(SessionDetail latest)
    {
        if (latest == null)
            return 1;

        if (latest.programCompleted)
            return 7;

        // Session payloads are uploaded before local day increment.
        // If the day was completed (3 mini-games), restore into the next day.
        bool completedDay = latest.miniGamesCompletedToday >= 3;
        int day = completedDay ? latest.day + 1 : latest.day;

        return Mathf.Clamp(day, 1, 7);
    }

    private static SessionDetail FindLatestSession(List<SessionDetail> sessions)
    {
        SessionDetail latest = sessions[0];
        for (int i = 1; i < sessions.Count; i++)
        {
            if (sessions[i].day > latest.day)
                latest = sessions[i];
            else if (sessions[i].day == latest.day
                     && sessions[i].sessionId > latest.sessionId)
                latest = sessions[i];
        }
        return latest;
    }
}