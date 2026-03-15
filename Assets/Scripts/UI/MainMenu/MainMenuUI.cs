// MainMenuUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTLTMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public UIPanelFader mainMenuPanel;
    public UIPanelFader profileSetupPanel;
    public UIPanelFader profileDetailsPanel;

    [Header("Buttons")]
    public Button startButton;
    public GameObject defaultProfileButton;
    public GameObject userProfileButton;

    [Header("User Display")]
    public Image userAvatarImage;
    public RTLTextMeshPro usernameText;
    public Sprite[] avatarSprites;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        var data = PlayerDataManager.Instance.Data;

        bool hasProfile = data != null && data.profileCompleted && data.profile != null;

        startButton.interactable = hasProfile;

        defaultProfileButton.SetActive(!hasProfile);
        userProfileButton.SetActive(hasProfile);

        if (!hasProfile)
            return;

        usernameText.text = data.profile.playerName;
        Debug.Log(data.profile.playerName); 
        int index = data.profile.avatarIndex;
        if (index >= 0 && index < avatarSprites.Length)
            userAvatarImage.sprite = avatarSprites[index];
    }

    public void OpenProfileSetup()
    {
        mainMenuPanel.FadeOut();
        profileSetupPanel.FadeIn();
    }

    public void OpenProfileDetails()
    {
        mainMenuPanel.FadeOut();
        profileDetailsPanel.FadeIn();
    }

    public void BackToMainFromSetup()
    {
        profileSetupPanel.FadeOut();
        mainMenuPanel.FadeIn();
        Refresh();
    }

    public void BackToMainFromDetails()
    {
        profileDetailsPanel.FadeOut();
       
        mainMenuPanel.FadeIn();
    }
}
