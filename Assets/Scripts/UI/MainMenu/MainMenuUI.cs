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

    [Header("User Display (Main Menu)")]
    public Image userAvatarImage;
    public RTLTextMeshPro usernameText;
    public Sprite[] avatarSprites;

    [Header("Profile Details Panel")]
    public Image userAvatarDetailImage;
    public RTLTextMeshPro detailsNameText;
    public RTLTextMeshPro detailsPhoneText;
    public RTLTextMeshPro detailsAgeText;

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

        defaultProfileButton.SetActive(!hasProfile);
        userProfileButton.SetActive(hasProfile);

        if (!hasProfile)
            return;

        usernameText.text = data.profile.playerName;

        int index = data.profile.avatarIndex;
        if (index >= 0 && index < avatarSprites.Length)
            userAvatarImage.sprite = avatarSprites[index];
    }

    void FillProfileDetails()
    {
        var data = PlayerDataManager.Instance.Data;

        if (data == null || data.profile == null)
            return;
        int index = data.profile.avatarIndex;
        if (index >= 0 && index < avatarSprites.Length)
            userAvatarDetailImage.sprite = avatarSprites[index];
        detailsNameText.text = data.profile.playerName;
        detailsPhoneText.text = data.profile.phoneNumber;
        detailsAgeText.text = data.profile.age.ToString();
    }

    public void OpenProfileSetup()
    {
        mainMenuPanel.FadeOut();
        profileSetupPanel.FadeIn();
        AudioManager.Instance.Play("Button");
    }

    public void OpenProfileDetails()
    {
        FillProfileDetails();

        mainMenuPanel.FadeOut();
        profileDetailsPanel.FadeIn();
        AudioManager.Instance.Play("Button");
    }

    public void BackToMainFromSetup()
    {
        profileSetupPanel.FadeOut();
        mainMenuPanel.FadeIn();
        Refresh();
        AudioManager.Instance.Play("Button");
    }

    public void BackToMainFromDetails()
    {
        profileDetailsPanel.FadeOut();
        mainMenuPanel.FadeIn();
        AudioManager.Instance.Play("Button");
    }
}