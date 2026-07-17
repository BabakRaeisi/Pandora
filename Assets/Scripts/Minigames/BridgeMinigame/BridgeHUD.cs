using UnityEngine;
using TMPro;
using RTLTMPro;
using DG.Tweening;
using System.Collections;

public class BridgeHUD : MonoBehaviour
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

        if (completionRoot) completionRoot.SetActive(false);
        if (quitButtonRoot) quitButtonRoot.SetActive(true);

        quitConfirmPanel?.FadeOut();
        returnToMapPanel?.FadeOut();
    }

    public void SetupTrial()
    {
        if (completionRoot) completionRoot.SetActive(false);
        if (quitButtonRoot) quitButtonRoot.SetActive(true);

        quitConfirmPanel?.FadeOut();
        returnToMapPanel?.FadeOut();
    }

    public void SetTrialsDone(int done)
    {
        trialsBar?.SetFilled(done);
    }

    public void ShowTrialComplete()
    {
        if (completionRoot) completionRoot.SetActive(true);
    }

    public void HideTrialComplete()
    {
        if (completionRoot) completionRoot.SetActive(false);
    }

    public void ShowDayComplete()
    {
        if (completionRoot) completionRoot.SetActive(true);
        if (quitButtonRoot) quitButtonRoot.SetActive(false);
        returnToMapPanel?.FadeIn();
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
        returnToMapPanel?.FadeOut();
        AudioManager.Instance.Play("Button");
    }
}