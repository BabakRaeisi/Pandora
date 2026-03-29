// ProfileSetupUI.cs
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProfileSetupUI : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField nameInput;
    public TMP_InputField ageInput;
    public TMP_InputField phoneInput;

    [Header("Gender")]
    public Toggle maleToggle;
    public Toggle femaleToggle;

    [Header("Avatar")]
    public AvatarCarousel avatarCarousel;

    [Header("Buttons")]
    public Button submitButton;

    [Header("Menu Ref")]
    public MainMenuUI mainMenuUI;

    [Header("Feedback (optional)")]
    [Tooltip("GameObject shown while the server call is in flight.")]
    public GameObject loadingIndicator;
    [Tooltip("Text label shown when the server call fails.")]
    public TMP_Text   errorText;

    void Start()
    {
        submitButton.interactable = false;

        if (errorText != null) errorText.gameObject.SetActive(false);
        if (loadingIndicator != null) loadingIndicator.SetActive(false);

        nameInput.onValueChanged.AddListener(_ => Validate());
        ageInput.onValueChanged.AddListener(_ => Validate());
        phoneInput.onValueChanged.AddListener(_ => Validate());

        maleToggle.onValueChanged.AddListener(_ => Validate());
        femaleToggle.onValueChanged.AddListener(_ => Validate());
    }

    void Validate()
    {
        bool nameValid  = !string.IsNullOrWhiteSpace(nameInput.text);
        bool ageValid   = !string.IsNullOrWhiteSpace(ageInput.text);
        bool phoneValid = !string.IsNullOrWhiteSpace(phoneInput.text);
        bool genderValid = maleToggle.isOn || femaleToggle.isOn;

        submitButton.interactable = nameValid && ageValid && phoneValid && genderValid;
    }

    // Called by the Submit button via UnityEvent.
    public void SubmitProfile()
    {
        StartCoroutine(SubmitProfileCoroutine());
    }

    private IEnumerator SubmitProfileCoroutine()
    {
        int.TryParse(ageInput.text, out int parsedAge);

        var profile = new PlayerProfile
        {
            phoneNumber = phoneInput.text.Trim(),
            playerName  = nameInput.text.Trim(),
            age         = parsedAge,
            avatarIndex = avatarCarousel.CurrentIndex,
            gender      = GetGender()
        };

        SetUIBusy(true);

        if (ProfileApiClient.Instance == null)
        {
            Debug.LogError("[ProfileSetupUI] ProfileApiClient not in scene. Falling back to local-only save.");
            PlayerDataManager.Instance.SetProfile(profile);
            SetUIBusy(false);
            mainMenuUI.BackToMainFromSetup();
            yield break;
        }

        var task = ProfileApiClient.Instance.RegisterOrRestoreAsync(profile);

        while (!task.IsCompleted)
            yield return null;

        SetUIBusy(false);

        if (task.Result == null)
        {
            ShowError("Could not reach server. Please check your connection and try again.");
            yield break;
        }

        PlayerDataManager.Instance.ApplyServerResponse(task.Result);

        if (errorText != null) errorText.gameObject.SetActive(false);
        mainMenuUI.BackToMainFromSetup();
    }

    public void Cancel()
    {
        mainMenuUI.BackToMainFromSetup();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    string GetGender()
    {
        if (maleToggle.isOn)   return "Male";
        if (femaleToggle.isOn) return "Female";
        return "";
    }

    void SetUIBusy(bool busy)
    {
        submitButton.interactable = !busy;
        if (loadingIndicator != null) loadingIndicator.SetActive(busy);
    }

    void ShowError(string message)
    {
        if (errorText == null) return;
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }
}
