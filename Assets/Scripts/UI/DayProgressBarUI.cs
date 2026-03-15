// DayProgressBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTLTMPro;

public class DayProgressBarUI : MonoBehaviour
{
    [Header("Progress")]
    public Slider progressBar;

    [Header("Text")]
    public RTLTextMeshPro remainingDaysText;

 

    const int totalDays = 7;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        var data = PlayerDataManager.Instance.Data;

        int currentDay =  Mathf.Clamp(data.currentDay, 1, totalDays);

        // progress fill
        float progress = (float)(currentDay - 1) / totalDays;
        progressBar.value = progress;

        // remaining days
        int remaining = totalDays - (currentDay - 1);

        if (remaining > 0)
            remainingDaysText.text = remaining + " days remaining";
        else
            remainingDaysText.text = "Final day";

        // key visual only when finished
     
    }
}
