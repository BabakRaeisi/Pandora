using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConstellationMapManager : MonoBehaviour
{
    private const string SelectedLevelKey = "ConstellationSelectedLevel";
    private const string GatewayShownKeyPrefix = "ConstellationGatewayShown_";
    private const int MinLevel = 1;

    [Header("References")]
    [SerializeField] private ConstellationConfigSO config;
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private RTLTextMeshPro[] levelLabels;

    [Header("Gateway Panel")]
    [SerializeField] private GameObject gatewayPassedPanel;

    [Header("Colors")]
    [SerializeField] private Color currentTop = new(0.65f, 0.88f, 1f);
    [SerializeField] private Color currentBottom = new(0.55f, 0.80f, 1f);
    [SerializeField] private Color unlockedTop = Color.white;
    [SerializeField] private Color unlockedBottom = Color.white;
    [SerializeField] private Color lockedTop = new(0.80f, 0.80f, 0.80f);
    [SerializeField] private Color lockedBottom = new(0.72f, 0.72f, 0.72f);

    [Header("Gateway Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite gatewayPassedBackground;

    private int unlockedLevel;
    private int selectedLevel;

    private void Start()
    {
        if (PlayerDataManager.Instance == null ||
            PlayerDataManager.Instance.Data == null)
        {
            return;
        }

        PlayerSaveData data = PlayerDataManager.Instance.Data;

        ApplyGatewayBackground(data.constellationGateReached);

        unlockedLevel = Mathf.Clamp(
            data.constellationLevel,
            MinLevel,
            GetTotalLevels()
        );

        selectedLevel = Mathf.Clamp(
            PlayerPrefs.GetInt(SelectedLevelKey, unlockedLevel),
            MinLevel,
            unlockedLevel
        );

        BuildButtons();
        TryShowGatewayPassedPanel();
    }

    private int GetTotalLevels()
    {
        if (config != null &&
            config.levels != null &&
            config.levels.Length > 0)
        {
            return config.levels.Length;
        }

        return levelButtons != null && levelButtons.Length > 0
            ? levelButtons.Length
            : MinLevel;
    }

    private void TryShowGatewayPassedPanel()
    {
        if (gatewayPassedPanel != null)
            gatewayPassedPanel.SetActive(false);

        if (config == null ||
            config.levels == null ||
            PlayerDataManager.Instance == null ||
            PlayerDataManager.Instance.Data == null)
        {
            return;
        }

        int maxPassedLevel = unlockedLevel - 1;

        for (int passedLevel = MinLevel;
             passedLevel <= maxPassedLevel;
             passedLevel++)
        {
            ConstellationConfigSO.LevelConfig level =
                config.GetLevel(passedLevel);

            if (!config.IsGatewayLevel(level))
                continue;

            string shownKey = GatewayShownKeyPrefix + passedLevel;

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
            Button button = levelButtons[i];

            if (button == null)
                continue;

            int levelNumber = i + 1;
            bool locked = levelNumber > unlockedLevel;

            button.onClick.RemoveAllListeners();
            button.interactable = !locked;

            int capturedLevel = levelNumber;
            button.onClick.AddListener(
                () => SelectLevel(capturedLevel)
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

            if (locked)
                SetGradient(gradient, lockedTop, lockedBottom);
            else if (levelNumber == selectedLevel)
                SetGradient(gradient, currentTop, currentBottom);
            else
                SetGradient(gradient, unlockedTop, unlockedBottom);
        }
    }

    private void SelectLevel(int levelNumber)
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

    public void ApplyGatewayBackground(int gateReached)
    {
        if (gateReached > 0)
        {
            if (gatewayPassedPanel != null)
            {
                gatewayPassedPanel.SetActive(true);
                gatewayPassedPanel.GetComponent<Image>().sprite = gatewayPassedBackground;
            }
        }
    }

    private void ApplyGatewayBackground(bool gatewayPassed)
    {
        if (!gatewayPassed || backgroundImage == null || gatewayPassedBackground == null)
            return;

        backgroundImage.sprite = gatewayPassedBackground;
    }
}
