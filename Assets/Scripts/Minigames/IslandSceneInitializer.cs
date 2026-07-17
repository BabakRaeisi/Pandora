using UnityEngine;

public class IslandSceneInitializer : MonoBehaviour
{
    [SerializeField] private IslandStageController stageController;

    [Header("Ambient Audio")]
    [SerializeField] private string dayAmbientName;
    [SerializeField] private string nightAmbientName;

    private void Start()
    {
        // This now updates island background, gem sprite,
        // and enables/disables the simple minigame buttons.
        stageController?.ApplyStageImmediate();

        SetupAmbient();
    }

    private void SetupAmbient()
    {
        if (PlayerDataManager.Instance == null ||
            PlayerDataManager.Instance.Data == null ||
            AudioManager.Instance == null)
        {
            return;
        }

        int completed = PlayerDataManager.Instance.Data.miniGamesCompletedToday;

        AudioManager.Instance.StopAmbient();

        if (completed >= 3)
            AudioManager.Instance.Play(dayAmbientName);
        else
            AudioManager.Instance.Play(nightAmbientName);

        AudioManager.Instance.Play("MusicLoop");
    }
}