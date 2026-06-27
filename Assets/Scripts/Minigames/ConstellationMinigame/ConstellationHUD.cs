using UnityEngine;
using TMPro;
using RTLTMPro;
using DG.Tweening;
using System.Collections;

public class ConstellationHUD : MonoBehaviour
{
    [Header("Footer Widget")]
    [SerializeField] private IconProgressBar trialsBar;

    [Header("Completion UI")]
    [SerializeField] private GameObject completionRoot;
    // REMOVE this line:
    // [SerializeField] private GameObject nextTrialButtonRoot;

    

    [Header("Quit Button")]
    [SerializeField] private GameObject quitButtonRoot;

    [Header("Panels With Fade")]
    [SerializeField] private UIPanelFader quitConfirmPanel;
    [SerializeField] private UIPanelFader returnToMapPanel;

    private int lastShownLevel = -1;
    private Tween levelTextTween;
    private Coroutine restoreLevelTextRoutine;

   

    public void SetupDay(int totalTrials)
    {
        Debug.Log($"[ConstellationHUD] SetupDay({totalTrials}) trialsBar={(trialsBar ? trialsBar.name : "NULL")}");
        trialsBar?.Setup(totalTrials);
    }

    public void SetupTrial()
    {
        
        if (completionRoot) completionRoot.SetActive(false);
        // REMOVE:
        // if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);

        if (quitButtonRoot) quitButtonRoot.SetActive(true);

        quitConfirmPanel?.FadeOut();
        returnToMapPanel?.FadeOut();
    }

    public void SetTrialsDone(int done)
    {
        Debug.Log($"[ConstellationHUD] SetTrialsDone({done}) trialsBar={(trialsBar ? trialsBar.name : "NULL")}");
        trialsBar?.SetFilled(done);
    }

    public void ShowTrialComplete()
    {
        if (completionRoot != null) completionRoot.SetActive(true);
        // REMOVE:
        // if (nextTrialButtonRoot != null) nextTrialButtonRoot.SetActive(true);
    }

    public void HideTrialComplete()
    {
        if (completionRoot != null) completionRoot.SetActive(false);
        // REMOVE:
        // if (nextTrialButtonRoot != null) nextTrialButtonRoot.SetActive(false);
    }

    public void SetTrialsDoneAndShowComplete(int done)
    {
        trialsBar?.SetFilled(done);
        ShowTrialComplete();
    }

    public void ShowLevelComplete(int completedLevel, int currentLevel)
    {
        quitConfirmPanel?.FadeOut();

        if (completionRoot) completionRoot.SetActive(true);
     
        if (quitButtonRoot) quitButtonRoot.SetActive(false);

        returnToMapPanel?.FadeIn();
    }

    
 
 

    void OnDestroy()
    {
        if (restoreLevelTextRoutine != null)
            StopCoroutine(restoreLevelTextRoutine);
        levelTextTween?.Kill();
    }

    public void ShowDayComplete()
    {
        quitConfirmPanel?.FadeOut();

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