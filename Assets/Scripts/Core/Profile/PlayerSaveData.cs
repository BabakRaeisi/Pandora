using System;

[Serializable]
public class PlayerSaveData
{
    public PlayerProfile profile;

    public int currentDay = 1;
    public int miniGamesCompletedToday = 0;
    public int trialsCompletedInCurrentGame = 0;

    public bool profileCompleted = false;

    public string lastDayCompletionTime;   // ← new field
}