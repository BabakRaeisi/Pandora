using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RTLTMPro;

public class DayProgressBarUI : MonoBehaviour
{
    [Header("Progress")]
    public Slider progressBar;

    [Header("Text")]
    public RTLTextMeshPro remainingDaysText;

    [Header("Animation")]
    public float fillDuration = 0.8f;

    const int totalDays = 6;
    Tween fillTween;

    void Start()
    {
        Refresh();
    }

    void OnDestroy()
    {
        fillTween?.Kill();
    }

    public void Refresh()
    {
        var data = PlayerDataManager.Instance.Data;

        int currentDay = Mathf.Clamp(data.currentDay, 1, totalDays);

        float progress = (float)(currentDay - 1) / totalDays;
        progressBar.value = progress;

        int remaining = totalDays - (currentDay - 1);

        if (remaining > 1)
            remainingDaysText.text = remaining + " روز باقی مونده";
        else if (remaining == 1)
            remainingDaysText.text = "روز مانده پایانی";
        else
            remainingDaysText.text = "پایان";
    }

    public Tween AnimateDayAdvance(bool finalDay)
    {
        fillTween?.Kill();

        var data = PlayerDataManager.Instance.Data;

        int currentDay = Mathf.Clamp(data.currentDay, 1, totalDays);

        float from = (float)(currentDay - 1) / totalDays;
        float to = finalDay
            ? 1f
            : (float)currentDay / totalDays;

        progressBar.value = from;

        fillTween = DOTween.To(
            () => progressBar.value,
            x => progressBar.value = x,
            to,
            fillDuration
        ).SetEase(Ease.OutCubic);

        return fillTween;
    }
}