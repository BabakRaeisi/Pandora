// IslandStageController.cs
using UnityEngine;
using System.Collections;

public class IslandStageController : MonoBehaviour
{
    public CanvasGroup[] stages;
    public float fadeDuration;

    void Start()
    {
        ApplyStageImmediate();
    }

    public void ApplyStageImmediate()
    {
        int completed = PlayerDataManager.Instance.Data.miniGamesCompletedToday;
        completed = Mathf.Clamp(completed, 0, stages.Length - 1);

        for (int i = 0; i < stages.Length; i++)
        {
            stages[i].alpha = (i == completed) ? 1f : 0f;
        }
    }

    public void UpdateIslandStage()
    {
        int completed = PlayerDataManager.Instance.Data.miniGamesCompletedToday;
        completed = Mathf.Clamp(completed, 0, stages.Length - 1);

        StartCoroutine(FadeToStage(completed));
    }

    IEnumerator FadeToStage(int targetStage)
    {
        for (int i = 0; i < stages.Length; i++)
        {
            float target = (i == targetStage) ? 1f : 0f;
            StartCoroutine(Fade(stages[i], target));
        }

        yield return null;
    }

    IEnumerator Fade(CanvasGroup cg, float target)
    {
        float start = cg.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }

        cg.alpha = target;
    }
}