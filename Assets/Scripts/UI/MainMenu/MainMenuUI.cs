using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTLTMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public UIPanelFader mainMenuPanel;
    public UIPanelFader profileSetupPanel;
    

    [Header("Buttons")]
    public Button startButton;
    
 

  
     
 

    void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopAmbient();
        Refresh();
        AudioManager.Instance.Play("MusicLoop");
    }

    public void Refresh()
    {
        var data = PlayerDataManager.Instance.Data;

        bool hasProfile = data != null && data.profileCompleted && data.profile != null;

        startButton.interactable = hasProfile;

 
         

        if (!hasProfile)
            return;
 

      
    }
 
    public void OpenProfileSetup()
    {
        mainMenuPanel.FadeOut();
        profileSetupPanel.FadeIn();
        AudioManager.Instance.Play("Button");
    }

    

    public void BackToMainFromSetup()
    {
        profileSetupPanel.FadeOut();
        mainMenuPanel.FadeIn();
        Refresh();
        AudioManager.Instance.Play("Button");
    }
 
}