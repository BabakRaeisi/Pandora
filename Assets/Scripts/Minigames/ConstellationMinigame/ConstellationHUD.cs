using UnityEngine;

public class ConstellationHUD : MonoBehaviour
{
    [Header("Footer Widget")]
    [SerializeField] private IconProgressBar trialsBar;    // footer stars

    [Header("Panels / Buttons")]
    [SerializeField] private GameObject completionRoot;
    [SerializeField] private GameObject nextTrialButtonRoot;
    [SerializeField] private GameObject returnToMenuButtonRoot;

    public void SetupDay(int totalTrials)
    {
        trialsBar?.Setup(totalTrials);
        trialsBar?.SetFilled(0);

        if (completionRoot) completionRoot.SetActive(false);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);
        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(false);
    }

    public void SetupTrial()
    {
        if (completionRoot) completionRoot.SetActive(false);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);
        if (returnToMenuButtonRoot) returnToMenuButtonRoot.SetActive(false);
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
