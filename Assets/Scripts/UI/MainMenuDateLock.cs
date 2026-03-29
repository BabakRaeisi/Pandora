using RTLTMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuDateLock : MonoBehaviour
{
    public Button startButton;
    public RTLTextMeshPro countdownText;

    void Start()
    {
        CheckLock();
    }

    void Update()
    {
        UpdateCountdown();
        CheckLock(); // ensure queue state is always respected
    }

    void CheckLock()
    {
        var data = PlayerDataManager.Instance.Data;

        bool hasProfile = data != null && data.profileCompleted && data.profile != null;

        if (!hasProfile)
        {
            startButton.interactable = false;
            countdownText.gameObject.SetActive(false);
            return;
        }

        // BLOCK if queue has pending data
        if (OfflineQueue.Instance != null && OfflineQueue.Instance.HasPending())
        {
            startButton.interactable = false;
            countdownText.gameObject.SetActive(true);
            countdownText.text = "Uploading previous session...";
            return;
        }

        if (string.IsNullOrEmpty(data.lastDayCompletionTime))
        {
            startButton.interactable = true;
            countdownText.gameObject.SetActive(false);
            return;
        }

        DateTime last = DateTime.Parse(data.lastDayCompletionTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
        DateTime next = last.AddDays(1);

        if (DateTime.UtcNow >= next)
        {
            ResetDailyProgress();
            startButton.interactable = true;
            countdownText.gameObject.SetActive(false);
        }
        else
        {
            startButton.interactable = false;
            countdownText.gameObject.SetActive(true);
        }
    }

    void UpdateCountdown()
    {
        var data = PlayerDataManager.Instance.Data;

        if (data.programCompleted)
        {
            startButton.interactable = true;
            return;
        }

        if (string.IsNullOrEmpty(data.lastDayCompletionTime))
            return;

        DateTime last = DateTime.Parse(data.lastDayCompletionTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
        DateTime next = last.AddDays(1);

        TimeSpan remaining = next - DateTime.UtcNow;

        if (remaining.TotalSeconds <= 0)
        {
            ResetDailyProgress();
            startButton.interactable = true;
            countdownText.gameObject.SetActive(false);
            return;
        }

        countdownText.text =
            "Come back in " +
            remaining.Hours.ToString("00") + ":" +
            remaining.Minutes.ToString("00") + ":" +
            remaining.Seconds.ToString("00");
    }

    void ResetDailyProgress()
    {
        var data = PlayerDataManager.Instance.Data;

        data.miniGamesCompletedToday = 0;
        data.trialsCompletedInCurrentGame = 0;

        data.constellationCompletedToday = false;
        data.swmCompletedToday = false;
        data.bridgeCompletedToday = false;

        data.lastDayCompletionTime = "";

        PlayerDataManager.Instance.Save();
    }
}