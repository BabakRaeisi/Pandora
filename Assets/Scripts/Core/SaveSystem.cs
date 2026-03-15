using System.IO;
using UnityEngine;

public static class SaveSystem
{
    static string SavePath => Path.Combine(Application.persistentDataPath, "player_save.json");

    public static void Save(PlayerSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public static PlayerSaveData Load()
    {
        if (!File.Exists(SavePath))
            return null;

        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<PlayerSaveData>(json);
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }
}