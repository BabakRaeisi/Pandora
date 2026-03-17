using UnityEngine;

public class ConstellationHUD : MonoBehaviour
{
    [Header("Footer Widget")]
    [SerializeField] private IconProgressBar trialsBar;

    [Header("Completion UI")]
    [SerializeField] private GameObject completionRoot;
    [SerializeField] private GameObject nextTrialButtonRoot;

    [Header("Quit Button")]
    [SerializeField] private GameObject quitButtonRoot;

    [Header("Panels With Fade")]
    [SerializeField] private UIPanelFader quitConfirmPanel;
    [SerializeField] private UIPanelFader returnToMapPanel;

   

    public void SetupDay(int totalTrials)
    {
        trialsBar?.Setup(totalTrials);
        trialsBar?.SetFilled(0);

        AudioManager.Instance.StopAll();
        
        if (completionRoot) completionRoot.SetActive(false);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);

        if (quitButtonRoot) quitButtonRoot.SetActive(true);

        if (quitConfirmPanel) quitConfirmPanel.gameObject.SetActive(false);
        if (returnToMapPanel) returnToMapPanel.gameObject.SetActive(false);
    }

    public void SetupTrial()
    {
        
        if (completionRoot) completionRoot.SetActive(false);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);

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
      
        quitConfirmPanel?.FadeOut();

        if (completionRoot) completionRoot.SetActive(true);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(true);

        if (quitButtonRoot) quitButtonRoot.SetActive(false);
       

    }

    public void ShowDayComplete()
    {
       
        quitConfirmPanel?.FadeOut();

        if (completionRoot) completionRoot.SetActive(true);
        if (nextTrialButtonRoot) nextTrialButtonRoot.SetActive(false);

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