// SWMHUD.cs
using UnityEngine;

public class SWMHUD : MonoBehaviour
{
    [Header("Top Widgets")]
    [SerializeField] private IconProgressBar collectedBar;
    [SerializeField] private IconStack errorsStack;

    [Header("Footer Widget")]
    [SerializeField] private IconProgressBar trialsBar;

    [Header("Warning")]
    [SerializeField] private TimedPanel openedBeforePanel;

    [Header("Completion UI")]
    [SerializeField] private GameObject completionRoot;
    [SerializeField] private GameObject nextTrialButtonRoot;

    [Header("Quit Button")]
    [SerializeField] private GameObject quitButtonRoot;

    [Header("Panels With Fade")]
    [SerializeField] private UIPanelFader quitConfirmPanel;
    [SerializeField] private UIPanelFader returnToMapPanel;

    void Awake()
    {
        if (quitConfirmPanel) quitConfirmPanel.gameObject.SetActive(false);
        if (returnToMapPanel) returnToMapPanel.gameObject.SetActive(false);
    }

    public void SetupDay(int totalTrials)
    {
        trialsBar?.Setup(totalTrials);
        trialsBar?.SetFilled(0);

        if (completionRoot) completionRoot.SetActive(false);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);

        if (quitButtonRoot) quitButtonRoot.SetActive(true);

        if (quitConfirmPanel) quitConfirmPanel.gameObject.SetActive(false);
        if (returnToMapPanel) returnToMapPanel.gameObject.SetActive(false);
    }

    public void SetupTrial(int goalCollected)
    {
        collectedBar?.Setup(goalCollected);
        collectedBar?.SetFilled(0);

        errorsStack?.Clear();
        openedBeforePanel?.Hide();

        if (completionRoot) completionRoot.SetActive(false);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);

        if (quitButtonRoot) quitButtonRoot.SetActive(true);

        if (quitConfirmPanel) quitConfirmPanel.gameObject.SetActive(false);
        if (returnToMapPanel) returnToMapPanel.gameObject.SetActive(false);
    }

    public void SetCollectedFound(int found)
    {
        collectedBar?.SetFilled(found);
    }

    public void AddErrorAndWarn()
    {
        errorsStack?.AddOne();
        openedBeforePanel?.Show();
        AudioManager.Instance.Play("StarError");
    }

    public void SetTrialsDone(int done)
    {
        trialsBar?.SetFilled(done);
    }

    public void ShowTrialComplete()
    {
        if (quitConfirmPanel) quitConfirmPanel.gameObject.SetActive(false);

        if (completionRoot) completionRoot.SetActive(true);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(true);

        if (quitButtonRoot) quitButtonRoot.SetActive(false);
        AudioManager.Instance.Play("SuccessDing");
    }

    public void ShowDayComplete()
    {
        if (quitConfirmPanel) quitConfirmPanel.gameObject.SetActive(false);

        if (completionRoot) completionRoot.SetActive(true);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);

        if (quitButtonRoot) quitButtonRoot.SetActive(false);

        returnToMapPanel?.FadeIn();
        AudioManager.Instance.Play("SuccessDing2");
    }

    public void ShowQuitConfirm()
    {
        quitConfirmPanel?.FadeIn();
        AudioManager.Instance.Play("Button");
    }

    public void HideQuitConfirm()
    {
        quitConfirmPanel?.FadeOut();
        AudioManager.Instance.Play("Button");
    }

    public void HideReturnPanel()
    {
        AudioManager.Instance.Play("Button");
        returnToMapPanel?.FadeOut();
    }
}