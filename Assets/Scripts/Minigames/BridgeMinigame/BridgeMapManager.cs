using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

public class BridgeMapManager : MonoBehaviour
{
    private const string SelectedLevelKey = "BridgeSelectedLevel";
    private const string GatewayShownKeyPrefix = "BridgeGatewayShown_";
    private const int MinLevel = 1;

    [Header("References")]
    [SerializeField] private BridgeConfig config;
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private RTLTextMeshPro[] levelLabels;

    [Header("Gateway Panel")]
    [SerializeField] private GameObject gatewayPassedPanel;

    [Header("Colors")]
    [SerializeField] private Color currentTop = new Color(0.65f, 0.88f, 1f);
    [SerializeField] private Color currentBottom = new Color(0.55f, 0.80f, 1f);
    [SerializeField] private Color unlockedTop = Color.white;
    [SerializeField] private Color unlockedBottom = Color.white;
    [SerializeField] private Color lockedTop = new Color(0.80f, 0.80f, 0.80f);
    [SerializeField] private Color lockedBottom = new Color(0.72f, 0.72f, 0.72f);

    [Header("Gateway Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite gatewayPassedBackground;

    private int unlockedLevel;
    private int selectedLevel;

    private void Start()
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.Data == null)
        {
            Debug.LogError("[BridgeMapManager] PlayerDataManager or player data is missing.", this);
            return;
        }

        int totalLevels = GetTotalLevels();
        var data = PlayerDataManager.Instance.Data;

        // Persistent visual change after gateway completion.
        ApplyGatewayBackground(data.bridgeGateReached);

        unlockedLevel = Mathf.Clamp(data.bridgeLevel, MinLevel, totalLevels);

        int savedSelectedLevel = PlayerPrefs.GetInt(SelectedLevelKey, unlockedLevel);
        selectedLevel = Mathf.Clamp(savedSelectedLevel, MinLevel, unlockedLevel);

        BuildButtons();

        // One-time gateway popup, matching ConstellationMapManager.
        TryShowGatewayPassedPanel();
    }

    private int GetTotalLevels()
    {
        if (levelButtons != null && levelButtons.Length > 0)
            return levelButtons.Length;

        if (config != null && config.levels != null && config.levels.Length > 0)
            return config.levels.Length;

        return MinLevel;
    }

    private void TryShowGatewayPassedPanel()
    {
        if (gatewayPassedPanel != null)
            gatewayPassedPanel.SetActive(false);

        if (config == null || config.levels == null || config.levels.Length == 0)
            return;

        // A passed level is any level before the current unlocked level.
        int maxPassedLevel = unlockedLevel - 1;

        for (int passedLevel = MinLevel; passedLevel <= maxPassedLevel; passedLevel++)
        {
            BridgeConfig.LevelConfig level = config.GetLevel(passedLevel);

            if (!config.IsGatewayLevel(level))
                continue;

            string shownKey = GatewayShownKeyPrefix + passedLevel;

            // Show the gateway popup once for this specific gateway level.
            if (PlayerPrefs.GetInt(shownKey, 0) == 1)
                continue;

            if (gatewayPassedPanel != null)
                gatewayPassedPanel.SetActive(true);

            PlayerPrefs.SetInt(shownKey, 1);
            PlayerPrefs.Save();
            break;
        }
    }

    public void CloseGatewayPassedPanel()
    {
        if (gatewayPassedPanel != null)
            gatewayPassedPanel.SetActive(false);
    }

    private void BuildButtons()
    {
        if (levelButtons == null)
            return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNumber = i + 1;
            Button button = levelButtons[i];

            if (button == null)
                continue;

            bool isLocked = levelNumber > unlockedLevel;
            bool isSelected = levelNumber == selectedLevel;
            bool isPreviouslyCompleted = levelNumber < unlockedLevel;

            button.onClick.RemoveAllListeners();
            button.interactable = !isLocked;

            int capturedLevel = levelNumber;
            button.onClick.AddListener(() => OnLevelClicked(capturedLevel));

            if (levelLabels != null &&
                i < levelLabels.Length &&
                levelLabels[i] != null)
            {
                levelLabels[i].text = levelNumber.ToString();
            }

            UIGradient gradient = button.GetComponent<UIGradient>();
            if (gradient == null)
                continue;

            if (isLocked)
                SetGradient(gradient, lockedTop, lockedBottom);
            else if (isSelected)
                SetGradient(gradient, currentTop, currentBottom);
            else if (isPreviouslyCompleted)
                SetGradient(gradient, unlockedTop, unlockedBottom);
            else
                SetGradient(gradient, currentTop, currentBottom);
        }
    }

    private void OnLevelClicked(int levelNumber)
    {
        selectedLevel = Mathf.Clamp(levelNumber, MinLevel, unlockedLevel);

        PlayerPrefs.SetInt(SelectedLevelKey, selectedLevel);
        PlayerPrefs.Save();

        BuildButtons();
    }

    private static void SetGradient(UIGradient gradient, Color top, Color bottom)
    {
        gradient.colorTop = top;
        gradient.colorBottom = bottom;
        gradient.enabled = true;
    }

    private void ApplyGatewayBackground(bool gatewayPassed)
    {
        if (!gatewayPassed || backgroundImage == null || gatewayPassedBackground == null)
            return;

        backgroundImage.sprite = gatewayPassedBackground;
    }
}