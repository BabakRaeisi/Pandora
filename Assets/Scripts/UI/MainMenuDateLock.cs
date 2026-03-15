using RTLTMPro;
using System;
using TMPro;
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
    }

    void CheckLock()
    {
        var data = PlayerDataManager.Instance.Data;

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

        if (string.IsNullOrEmpty(data.lastDayCompletionTime))
            return;

        DateTime last = DateTime.Parse(data.lastDayCompletionTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
        DateTime next = last.AddDays(1);

        TimeSpan remaining = next - DateTime.UtcNow;

        if (remaining.TotalSeconds <= 0)
        {
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
}
