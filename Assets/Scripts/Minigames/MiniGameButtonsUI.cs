using UnityEngine;
using UnityEngine.UI;
using System;
using System.Globalization;
using TMPro;

public class MiniGameButtonsUI : MonoBehaviour
{
    private const int LevelsPerMinigame = 16;
    private const int Star1Level = 4;
    private const int GateLevel = 8;

    [System.Serializable]
    public class MiniGameButton
    {
        public Button button;
        public Image icon;
        public TextMeshProUGUI countdownLabel;
        public Slider progressBar;
        public Image[] stars;
    }

    public MiniGameButton[] buttons;
    public Sprite playSprite;
    public Sprite lockSprite;
    public Sprite emptyStarSprite;
    public Sprite filledStarSprite;

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        var data = PlayerDataManager.Instance.Data;
        int completed = data.miniGamesCompletedToday;

        for (int i = 0; i < buttons.Length; i++)
        {
            int currentLevel = GetCurrentLevelByIndex(data, i);
            int completedLevels = Mathf.Clamp(currentLevel - 1, 0, LevelsPerMinigame);

            TimeSpan remaining = TimeSpan.Zero;
            float countdownFill = 1f;
            bool activeCountdown = i == 0 && TryGetConstellationCountdown(data, out remaining, out countdownFill);
            bool unlocked = (i <= completed) && completed < 3;

            if (activeCountdown)
                unlocked = false;

            if (buttons[i].button != null)
                buttons[i].button.interactable = unlocked;

            if (buttons[i].icon != null)
                buttons[i].icon.sprite = unlocked ? playSprite : lockSprite;

            UpdateCountdownOrLevelText(buttons[i], activeCountdown, remaining, currentLevel);
            UpdateCountdownProgress(buttons[i], activeCountdown, countdownFill);
            UpdateStars(buttons[i], completedLevels);
        }

        if (completed >= 3 || data.programCompleted)
            ForceLockAll();
    }

    int GetCurrentLevelByIndex(PlayerSaveData data, int index)
    {
        if (index == 0) return Mathf.Clamp(data.constellationLevel, 1, ProgressionManager.MAX_LEVEL);
        if (index == 1) return Mathf.Clamp(data.bridgeLevel, 1, ProgressionManager.MAX_LEVEL);
        return Mathf.Clamp(data.swmLevel, 1, ProgressionManager.MAX_LEVEL);
    }

    void UpdateCountdownOrLevelText(MiniGameButton btn, bool activeCountdown, TimeSpan remaining, int currentLevel)
    {
        if (btn?.countdownLabel == null)
            return;

        if (activeCountdown && remaining.TotalSeconds > 1f)
        {
            btn.countdownLabel.text =
                remaining.Hours.ToString("00") + ":" +
                remaining.Minutes.ToString("00") + ":" +
                remaining.Seconds.ToString("00");
        }
        else
        {
            btn.countdownLabel.text = "Level " + currentLevel;
        }
    }

    void UpdateCountdownProgress(MiniGameButton btn, bool activeCountdown, float countdownFill)
    {
        if (btn?.progressBar == null)
            return;

        btn.progressBar.gameObject.SetActive(true);
        btn.progressBar.normalizedValue = activeCountdown ? Mathf.Clamp01(countdownFill) : 1f;
    }

    void UpdateStars(MiniGameButton btn, int completedLevels)
    {
        if (btn?.stars == null || btn.stars.Length == 0)
            return;

        int earnedStars = 0;
        if (completedLevels >= Star1Level) earnedStars = 1;
        if (completedLevels >= GateLevel) earnedStars = 2;
        if (completedLevels >= LevelsPerMinigame) earnedStars = 3;

        int count = Mathf.Min(3, btn.stars.Length);
        for (int i = 0; i < count; i++)
        {
            if (btn.stars[i] == null)
                continue;

            btn.stars[i].gameObject.SetActive(true);

            if (i < earnedStars)
            {
                if (filledStarSprite != null)
                    btn.stars[i].sprite = filledStarSprite;
            }
            else
            {
                if (emptyStarSprite != null)
                    btn.stars[i].sprite = emptyStarSprite;
            }
        }
    }

    bool TryGetConstellationCountdown(PlayerSaveData data, out TimeSpan remaining, out float fill)
    {
        remaining = TimeSpan.Zero;
        fill = 1f;

        if (data == null)
            return false;

        if (data.constellationLockLevel != data.constellationLevel)
            return false;

        if (string.IsNullOrWhiteSpace(data.constellationLockUntilTime))
            return false;

        if (!DateTime.TryParse(data.constellationLockUntilTime, null, DateTimeStyles.RoundtripKind, out DateTime lockUntilUtc))
        {
            ClearConstellationLock(data);
            return false;
        }

        DateTime now = DateTime.UtcNow;
        remaining = lockUntilUtc - now;
        if (remaining.TotalSeconds <= 1f)
        {
            ClearConstellationLock(data);
            return false;
        }

        if (DateTime.TryParse(data.constellationLastLevelCompletionTime, null, DateTimeStyles.RoundtripKind, out DateTime lockStartUtc))
        {
            double total = (lockUntilUtc - lockStartUtc).TotalSeconds;
            double elapsed = (now - lockStartUtc).TotalSeconds;
            if (total > 0)
                fill = Mathf.Clamp01((float)(elapsed / total));
            else
                fill = 0f;
        }
        else
        {
            fill = 0f;
        }

        return true;
    }

    void ClearConstellationLock(PlayerSaveData data)
    {
        if (data == null)
            return;

        data.constellationLockLevel = 0;
        data.constellationLockUntilTime = "";
        PlayerDataManager.Instance.Save();
    }

    public void ForceLockAll()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].button != null)
                buttons[i].button.interactable = false;

            if (buttons[i].icon != null)
                buttons[i].icon.sprite = lockSprite;

            if (buttons[i].progressBar != null)
                buttons[i].progressBar.normalizedValue = 1f;
        }
    }
}