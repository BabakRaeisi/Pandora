using UnityEngine;

public class PlayerDataLoader : MonoBehaviour
{
    public PlayerSaveData Data { get; private set; }

    void Awake()
    {
        if (SaveSystem.HasSave())
            Data = SaveSystem.Load();
    }
}