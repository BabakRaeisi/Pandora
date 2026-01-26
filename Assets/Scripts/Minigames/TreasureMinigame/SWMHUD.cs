// SWMHUD.cs
// Adds footer "trial progress" stars (gray = remaining, gold = done)
// Also switches buttons:
// - During day: show NextTrialButtonRoot after each trial completes
// - After day complete: hide NextTrialButtonRoot and show ReturnToMenuButtonRoot
// NO TEXT used.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SWMHUD : MonoBehaviour
{
    [Header("Top Containers")]
    [SerializeField] private RectTransform treasuresContainer;
    [SerializeField] private RectTransform errorsContainer;

    [Header("Footer Trial Progress")]
    [SerializeField] private RectTransform trialsContainer;   // your footer container
    [SerializeField] private Image trialStarPrefab;           // Image prefab for stars (give it a size!)
    [SerializeField] private Sprite trialStarEmpty;           // gray star
    [SerializeField] private Sprite trialStarFilled;          // gold star

    [Header("Prefabs (UI Image)")]
    [SerializeField] private Image slotPrefab;                // treasure slots
    [SerializeField] private Image errorIconPrefab;           // error icons (optional)

    [Header("Treasure Slot Sprites")]
    [SerializeField] private Sprite treasureEmpty;
    [SerializeField] private Sprite treasureFilled;

    [Header("Between Error Icon Sprite")]
    [SerializeField] private Sprite betweenErrorIcon;

    [Header("Panels (NO TEXT)")]
    [SerializeField] private GameObject errorMessageRoot;     // your Farsi-plugin object/icon
    [SerializeField] private float errorMessageSeconds = 1.5f;
    [SerializeField] private GameObject completionRoot;       // trial complete indicator/icon/panel
    [SerializeField] private GameObject nextTrialButtonRoot;  // contains Next Trial button
    [SerializeField] private GameObject returnToMenuButtonRoot; // contains Return to Menu button

    private readonly List<Image> treasureSlots = new();
    private readonly List<Image> errorIcons = new();
    private readonly List<Image> trialStars = new();

    private int treasureGoal = 0;
    private int trialsTotal = 0;

    // Call once when a DAY starts (before trial 1)
    public void SetupDay(int totalTrials)
    {
        trialsTotal = Mathf.Max(0, totalTrials);
        BuildTrialStars(trialsTotal);
        SetTrialsProgress(0);

        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(false);
    }

    // Call at the start of EVERY trial
    public void SetupTrial(int goalTreasures)
    {
        treasureGoal = Mathf.Max(0, goalTreasures);

        BuildTreasureSlots(treasureGoal);
        ClearErrors();
        SetTreasuresFound(0);

        HideErrorMessage();

        if (completionRoot) completionRoot.SetActive(false);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);
        // return-to-menu is only for day complete; keep hidden here
        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(false);
    }

    public void SetTrialsProgress(int trialsDone)
    {
        trialsDone = Mathf.Clamp(trialsDone, 0, trialsTotal);

        for (int i = 0; i < trialStars.Count; i++)
        {
            if (!trialStars[i]) continue;
            trialStars[i].sprite = (i < trialsDone) ? trialStarFilled : trialStarEmpty;
            trialStars[i].color = Color.white;
            trialStars[i].gameObject.SetActive(true);
        }

        ForceRebuild(trialsContainer);
    }

    public void SetTreasuresFound(int found)
    {
        found = Mathf.Clamp(found, 0, treasureGoal);

        for (int i = 0; i < treasureSlots.Count; i++)
        {
            if (!treasureSlots[i]) continue;
            treasureSlots[i].sprite = (i < found) ? treasureFilled : treasureEmpty;
            treasureSlots[i].color = Color.white;
            treasureSlots[i].gameObject.SetActive(true);
        }

        ForceRebuild(treasuresContainer);
    }

    public void AddBetweenError()
    {
        if (!errorsContainer || !betweenErrorIcon) return;

        Image prefab = errorIconPrefab ? errorIconPrefab : slotPrefab;
        if (!prefab) return;

        var icon = Instantiate(prefab, errorsContainer);
        icon.sprite = betweenErrorIcon;
        icon.color = Color.white;

        var rt = icon.rectTransform;
        rt.localScale = Vector3.one;
        if (rt.sizeDelta == Vector2.zero) rt.sizeDelta = new Vector2(40, 40);

        icon.gameObject.SetActive(true);
        errorIcons.Add(icon);

        ForceRebuild(errorsContainer);
        ShowErrorMessage();
    }

    public void ShowTrialComplete()
    {
        if (completionRoot) completionRoot.SetActive(true);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(true);

        // Ensure return-to-menu stays hidden until day end
        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(false);
    }

    // Call when ALL trials for the day are done
    public void ShowDayComplete()
    {
        // Keep completion visible if you want; or turn it off. Your choice:
        if (completionRoot) completionRoot.SetActive(true);

        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);
        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(true);
    }

    void BuildTreasureSlots(int count)
    {
        ClearList(treasureSlots);
        if (!treasuresContainer || !slotPrefab) return;

        for (int i = 0; i < count; i++)
        {
            var img = Instantiate(slotPrefab, treasuresContainer);
            img.name = $"TreasureSlot_{i}";
            img.sprite = treasureEmpty;
            img.color = Color.white;

            var rt = img.rectTransform;
            rt.localScale = Vector3.one;
            if (rt.sizeDelta == Vector2.zero) rt.sizeDelta = new Vector2(40, 40);

            img.gameObject.SetActive(true);
            treasureSlots.Add(img);
        }

        ForceRebuild(treasuresContainer);
    }

    void BuildTrialStars(int count)
    {
        ClearList(trialStars);
        if (!trialsContainer || !trialStarPrefab) return;

        for (int i = 0; i < count; i++)
        {
            var img = Instantiate(trialStarPrefab, trialsContainer);
            img.name = $"TrialStar_{i}";
            img.sprite = trialStarEmpty;
            img.color = Color.white;

            var rt = img.rectTransform;
            rt.localScale = Vector3.one;
            if (rt.sizeDelta == Vector2.zero) rt.sizeDelta = new Vector2(32, 32);

            img.gameObject.SetActive(true);
            trialStars.Add(img);
        }

        ForceRebuild(trialsContainer);
    }

    void ClearErrors() => ClearList(errorIcons);

    static void ClearList(List<Image> list)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i]) Destroy(list[i].gameObject);
        list.Clear();
    }

    void ShowErrorMessage()
    {
        if (!errorMessageRoot) return;
        errorMessageRoot.SetActive(true);
        CancelInvoke(nameof(HideErrorMessage));
        Invoke(nameof(HideErrorMessage), Mathf.Max(0.1f, errorMessageSeconds));
    }

    void HideErrorMessage()
    {
        if (errorMessageRoot) errorMessageRoot.SetActive(false);
    }

    static void ForceRebuild(RectTransform rt)
    {
        if (!rt) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
