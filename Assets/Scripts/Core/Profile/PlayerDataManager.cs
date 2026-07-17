using System.Collections.Generic;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerSaveData Data;

    [ContextMenu("Clear")]
    public void ClearSave()
    {
        string path = System.IO.Path.Combine(
            Application.persistentDataPath,
            "player_save.json"
        );

        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

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

    public PlayerProfile GetProfile()
    {
        if (Data == null || !Data.profileCompleted)
            return null;

        return Data.profile;
    }

    public void ApplyServerResponse(ProfileRestoreResponse response)
    {
        if (response == null)
            return;

        PlayerFullData serverData = response.playerData;

        if (serverData == null)
            return;

        ProfileDetail pd = serverData.profile;

        var profile = new PlayerProfile
        {
            phoneNumber = pd?.phoneNumber ?? Data?.profile?.phoneNumber ?? "",
            playerName = pd?.playerName ?? "",
            age = pd?.age ?? 0,
            avatarIndex = pd?.avatarIndex ?? 0,
            gender = pd?.gender ?? ""
        };

        if (!response.isExistingPlayer ||
            serverData.sessions == null ||
            serverData.sessions.Count == 0)
        {
            Data = new PlayerSaveData
            {
                profile = profile,
                profileCompleted = true
            };

            Save();
            return;
        }

        SessionDetail latest = FindLatestSession(serverData.sessions);
        int restoredDay = ComputeRestoredCurrentDay(latest);

        Data = new PlayerSaveData
        {
            profile = profile,
            currentDay = restoredDay,
            miniGamesCompletedToday = latest.miniGamesCompletedToday,
            trialsCompletedInCurrentGame = latest.trialsCompletedInCurrentGame,
            bridgeCompletedToday = latest.bridgeCompletedToday,
            constellationCompletedToday = latest.constellationCompletedToday,
            swmCompletedToday = latest.swmCompletedToday,
            programCompleted = latest.programCompleted,
            profileCompleted = true,
            lastDayCompletionTime = latest.lastDayCompletionTime,

            constellationLevel = latest.constellationLevel > 0
                ? latest.constellationLevel
                : 1,

            bridgeLevel = latest.bridgeLevel > 0
                ? latest.bridgeLevel
                : 1,

            swmLevel = latest.swmLevel > 0
                ? latest.swmLevel
                : 1,

            bridgeUnlocked = latest.bridgeUnlocked,
            swmUnlocked = latest.swmUnlocked,

            constellationGateReached = latest.constellationGateReached,
            bridgeGateReached = latest.bridgeGateReached,
            swmGateReached = latest.swmGateReached,

            constellationPerfect = latest.constellationPerfect,
            bridgePerfect = latest.bridgePerfect,
            swmPerfect = latest.swmPerfect,

            constellationLevelsPlayedThisSession =
                latest.constellationLevelsPlayedThisSession,

            bridgeLevelsPlayedThisSession =
                latest.bridgeLevelsPlayedThisSession,

            swmLevelsPlayedThisSession =
                latest.swmLevelsPlayedThisSession,

            constellationLastLevelCompletionTime =
                latest.constellationLastLevelCompletionTime,

            constellationLockUntilTime =
                latest.constellationLockUntilTime,

            constellationLockLevel =
                latest.constellationLockLevel
        };

        Save();
    }

    private static int ComputeRestoredCurrentDay(SessionDetail latest)
    {
        if (latest == null)
            return 1;

        if (latest.programCompleted)
            return 7;

        bool completedDay = latest.miniGamesCompletedToday >= 3;
        int day = completedDay ? latest.day + 1 : latest.day;

        return Mathf.Clamp(day, 1, 7);
    }

    private static SessionDetail FindLatestSession(
        List<SessionDetail> sessions)
    {
        SessionDetail latest = sessions[0];

        for (int i = 1; i < sessions.Count; i++)
        {
            if (sessions[i].day > latest.day)
                latest = sessions[i];
            else if (sessions[i].day == latest.day &&
                     sessions[i].sessionId > latest.sessionId)
                latest = sessions[i];
        }

        return latest;
    }
}