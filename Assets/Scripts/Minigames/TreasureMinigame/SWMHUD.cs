using UnityEngine;

public class SWMHUD : MonoBehaviour
{
    [Header("Top Widgets")]
    [SerializeField] private IconProgressBar collectedBar; // top left (collected items)
    [SerializeField] private IconStack errorsStack;        // top right (errors)

    [Header("Footer Widget")]
    [SerializeField] private IconProgressBar trialsBar;    // footer stars

    [Header("Warning (NO TEXT)")]
    [SerializeField] private TimedPanel openedBeforePanel; // your message icon/panel

    [Header("Panels / Buttons")]
    [SerializeField] private GameObject completionRoot;
    [SerializeField] private GameObject nextTrialButtonRoot;
    [SerializeField] private GameObject returnToMenuButtonRoot;

    public void SetupDay(int totalTrials)
    {
        trialsBar?.Setup(totalTrials);
        trialsBar?.SetFilled(0);

        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(false);
    }

    public void SetupTrial(int goalCollected)
    {
        collectedBar?.Setup(goalCollected);
        collectedBar?.SetFilled(0);

        errorsStack?.Clear();
        openedBeforePanel?.Hide();

        if (completionRoot) completionRoot.SetActive(false);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);
        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(false);
    }

    public void SetCollectedFound(int found) => collectedBar?.SetFilled(found);

    public void AddErrorAndWarn()
    {
        errorsStack?.AddOne();
        openedBeforePanel?.Show();
    }

    public void SetTrialsDone(int done) => trialsBar?.SetFilled(done);

    public void ShowTrialComplete()
    {
        if (completionRoot) completionRoot.SetActive(true);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(true);
        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(false);
    }

    public void ShowDayComplete()
    {
        if (completionRoot) completionRoot.SetActive(true);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);
        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(true);
    }
}
