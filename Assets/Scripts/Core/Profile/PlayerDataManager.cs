using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerSaveData Data;


    [ContextMenu("Clear")]
    public void ClearSave() { string path = System.IO.Path.Combine(Application.persistentDataPath, "player_save.json"); if (System.IO.File.Exists(path)) System.IO.File.Delete(path); Data = new PlayerSaveData(); }

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
}