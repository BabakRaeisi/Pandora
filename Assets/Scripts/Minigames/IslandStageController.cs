using UnityEngine;
using UnityEngine.UI;

public class IslandStageController : MonoBehaviour
{
    [Header("Map Buttons")]
    [Tooltip("Order: Constellation map, Bridge map, Treasure map.")]
    [SerializeField] private Button[] minigameMapButtons;

    [Header("Map Button Icons")]
    [Tooltip("Same order as minigameMapButtons. Shown when the level is locked.")]
    [SerializeField] private GameObject[] lockIcons;

    [Tooltip("Same order as minigameMapButtons. Shown when the level is unlocked/playable.")]
    [SerializeField] private GameObject[] playIcons;

    [Header("Island Backgrounds")]
    [Tooltip("0 = no gems, 1 = Constellation gateway passed, 2 = Bridge gateway passed, 3 = SWM gateway passed.")]
    [SerializeField] private GameObject[] islandBackgrounds;

    [Header("Gem Progress")]
    [SerializeField] private Image gemProgressImage;

    [Tooltip("0 = no gems, 1 = one gem, 2 = two gems, 3 = three gems.")]
    [SerializeField] private Sprite[] gemProgressSprites;

    private void Start()
    {
        ApplyStageImmediate();
    }

    public void ApplyStageImmediate()
    {
        if (PlayerDataManager.Instance == null ||
            PlayerDataManager.Instance.Data == null)
        {
             return;
        }

        PlayerSaveData data = PlayerDataManager.Instance.Data;

        // All button GameObjects stay enabled.
        // Only their Button components become clickable or disabled.
        SetButtonInteractable(0, true);
        SetButtonInteractable(1, data.bridgeUnlocked);
        SetButtonInteractable(2, data.swmUnlocked);

        int gemCount = CountEarnedGems(
            data.constellationGateReached,
            data.bridgeGateReached,
            data.swmGateReached
        );

        SetGemSprite(gemCount);
        SetIslandBackground(gemCount);
    }

    private void SetButtonInteractable(int index, bool isInteractable)
    {
        if (minigameMapButtons == null ||
            index < 0 ||
            index >= minigameMapButtons.Length ||
            minigameMapButtons[index] == null)
        {
            return;
        }

        minigameMapButtons[index].interactable = isInteractable;

        SetIconActive(lockIcons, index, !isInteractable);
        SetIconActive(playIcons, index, isInteractable);
    }

    private static void SetIconActive(GameObject[] icons, int index, bool isActive)
    {
        if (icons == null ||
            index < 0 ||
            index >= icons.Length ||
            icons[index] == null)
        {
            return;
        }

        icons[index].SetActive(isActive);
    }

    private static int CountEarnedGems(
        bool constellationGatewayPassed,
        bool bridgeGatewayPassed,
        bool swmGatewayPassed)
    {
        int gemCount = 0;

        if (constellationGatewayPassed)
            gemCount++;

        if (bridgeGatewayPassed)
            gemCount++;

        if (swmGatewayPassed)
            gemCount++;

        return gemCount;
    }

    private void SetGemSprite(int gemCount)
    {
        if (gemProgressImage == null ||
            gemProgressSprites == null ||
            gemProgressSprites.Length == 0)
        {
            return;
        }

        int spriteIndex = Mathf.Clamp(
            gemCount,
            0,
            gemProgressSprites.Length - 1
        );

        gemProgressImage.sprite = gemProgressSprites[spriteIndex];
    }

    private void SetIslandBackground(int gemCount)
    {
        if (islandBackgrounds == null ||
            islandBackgrounds.Length == 0)
        {
            return;
        }

        int backgroundIndex = Mathf.Clamp(
            gemCount,
            0,
            islandBackgrounds.Length - 1
        );

        for (int i = 0; i < islandBackgrounds.Length; i++)
        {
            GameObject background = islandBackgrounds[i];

            if (background == null)
                continue;

            CanvasGroup canvasGroup =
                background.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
               continue;
            }

            // All backgrounds remain active. Only the selected stage is visible.
            bool isSelected = i == backgroundIndex;

            canvasGroup.alpha = isSelected ? 1f : 0f;
            canvasGroup.interactable = isSelected;
            canvasGroup.blocksRaycasts = isSelected;
        }
    }
}