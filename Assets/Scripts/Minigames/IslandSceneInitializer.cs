using UnityEngine;

public class IslandSceneInitializer : MonoBehaviour
{
    public IslandStageController stageController;
    public MiniGameButtonsUI buttonsUI;

    [Header("Ambient Audio")]
    public string dayAmbientName;
    public string nightAmbientName;

    void Start()
    {
        if (stageController != null)
            stageController.ApplyStageImmediate();

        if (buttonsUI != null)
            buttonsUI.Refresh();

        SetupAmbient();
    }

    void SetupAmbient()
    {
        int completed = PlayerDataManager.Instance.Data.miniGamesCompletedToday;

        AudioManager.Instance.StopAmbient();

        if (completed >= 3)
        {
            AudioManager.Instance.Play(dayAmbientName);
            AudioManager.Instance.Play("MusicLoop");
        }
        else
        {
            AudioManager.Instance.Play(nightAmbientName);
            AudioManager.Instance.Play("MusicLoop");
        }
    }
}