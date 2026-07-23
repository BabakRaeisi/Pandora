using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

public class SWMMapManager : MonoBehaviour
{
    private const string SelectedLevelKey = "TreasureSelectedLevel";
    private const string GatewayShownKeyPrefix = "TreasureGatewayShown_";
    private const int MinLevel = 1;

    [Header("References")]
    [SerializeField] private SWMConfig config;
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

    private Sprite defaultBackground;

    private int unlockedLevel;
    private int selectedLevel;

    private void Awake()
    {
        if (backgroundImage != null)
            defaultBackground = backgroundImage.sprite;
    }

    private void Start()
    {
        if (PlayerDataManager.Instance == null ||
            PlayerDataManager.Instance.Data == null)
        {
            Debug.LogError(
                "[SWMMapManager] PlayerDataManager or player data is missing.",
                this
            );
            return;
        }

        int totalLevels = GetTotalLevels();
        var data = PlayerDataManager.Instance.Data;

        Debug.Log(
            $"[SWMMapManager] swmGateReached = {data.swmGateReached}",
            this
        );

        ApplyGatewayBackground(data.swmGateReached);
        ShowPendingGatewayPanel(data.swmGateReached);

        unlockedLevel = Mathf.Clamp(data.swmLevel, MinLevel, totalLevels);

        int savedSelectedLevel = PlayerPrefs.GetInt(
            SelectedLevelKey,
            unlockedLevel
        );

        selectedLevel = Mathf.Clamp(
            savedSelectedLevel,
            MinLevel,
            unlockedLevel
        );

        BuildButtons();
    }

    private int GetTotalLevels()
    {
        if (levelButtons != null && levelButtons.Length > 0)
            return levelButtons.Length;

        if (config != null &&
            config.levels != null &&
            config.levels.Length > 0)
        {
            return config.levels.Length;
        }

        return MinLevel;
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
            button.onClick.AddListener(
                () => OnLevelClicked(capturedLevel)
            );

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
        selectedLevel = Mathf.Clamp(
            levelNumber,
            MinLevel,
            unlockedLevel
        );

        PlayerPrefs.SetInt(SelectedLevelKey, selectedLevel);
        PlayerPrefs.Save();

        BuildButtons();
    }

    private static void SetGradient(
        UIGradient gradient,
        Color top,
        Color bottom)
    {
        gradient.colorTop = top;
        gradient.colorBottom = bottom;
        gradient.enabled = true;
    }

    public void ApplyGatewayBackground(bool swmGatewayReached)
    {
        if (backgroundImage == null)
            return;

        // Normal SWM map background before the SWM gateway is completed.
        if (!swmGatewayReached)
        {
            backgroundImage.sprite = defaultBackground;
            return;
        }

        // Gateway-completed SWM map background.
        if (gatewayPassedBackground != null)
            backgroundImage.sprite = gatewayPassedBackground;
    }

    private void ShowPendingGatewayPanel(bool swmGatewayReached)
    {
        if (gatewayPassedPanel == null)
            return;

        // Never show by default when entering SWM map from Main Menu.
        gatewayPassedPanel.SetActive(false);

        const string gatewayPanelPendingKey = "SWMGatewayPanelPending";

        bool returnedFromCompletedGateway =
            PlayerPrefs.GetInt(gatewayPanelPendingKey, 0) == 1;

        if (!swmGatewayReached || !returnedFromCompletedGateway)
            return;

        gatewayPassedPanel.SetActive(true);

        // Consume it immediately. Later Menu -> SWM Map visits will not show it.
        PlayerPrefs.DeleteKey(gatewayPanelPendingKey);
        PlayerPrefs.Save();
    }
}