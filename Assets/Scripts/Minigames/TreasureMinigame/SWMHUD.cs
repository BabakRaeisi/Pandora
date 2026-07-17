// SWMHUD.cs
using UnityEngine;

public class SWMHUD : MonoBehaviour
{
    [Header("Footer Widget")]
    [SerializeField] private IconProgressBar trialsBar;

    [Header("Completion UI")]
    [SerializeField] private GameObject completionRoot;

    [Header("Quit Button")]
    [SerializeField] private GameObject quitButtonRoot;

    [Header("Panels With Fade")]
    [SerializeField] private UIPanelFader quitConfirmPanel;
    [SerializeField] private UIPanelFader returnToMapPanel;

    public void SetupDay(int totalTrials)
    {
        if (trialsBar != null)
        {
            trialsBar.Setup(totalTrials);
            trialsBar.SetFilled(0);
        }

        if (completionRoot != null)
            completionRoot.SetActive(false);

        if (quitButtonRoot != null)
            quitButtonRoot.SetActive(true);

        quitConfirmPanel?.FadeOut();
        returnToMapPanel?.FadeOut();
    }

    public void SetupTrial()
    {
        if (completionRoot != null)
            completionRoot.SetActive(false);

        if (quitButtonRoot != null)
            quitButtonRoot.SetActive(true);

        quitConfirmPanel?.FadeOut();
        returnToMapPanel?.FadeOut();
    }

    
    public void SetTrialsDone(int done)
    {
        trialsBar?.SetFilled(done);
    }

    public void ShowTrialComplete()
    {
        if (completionRoot != null)
            completionRoot.SetActive(true);
    }

    public void HideTrialComplete()
    {
        if (completionRoot != null)
            completionRoot.SetActive(false);
    }

    public void ShowDayComplete()
    {
        if (completionRoot != null)
            completionRoot.SetActive(true);

        if (quitButtonRoot != null)
            quitButtonRoot.SetActive(false);

        returnToMapPanel?.FadeIn();
    }

    public void ShowQuitConfirm()
    {
        quitConfirmPanel?.FadeIn();

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("Button");
    }

    public void HideQuitConfirm()
    {
        quitConfirmPanel?.FadeOut();

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("Button");
    }

    public void HideReturnPanel()
    {
        returnToMapPanel?.FadeOut();

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("Button");
    }
}