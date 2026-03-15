// IslandSceneInitializer.cs
using UnityEngine;

public class IslandSceneInitializer : MonoBehaviour
{
    public IslandStageController stageController;
    public MiniGameButtonsUI buttonsUI;

    void Start()
    {
        if (stageController != null)
            stageController.ApplyStageImmediate();

        if (buttonsUI != null)
            buttonsUI.Refresh();
    }
}