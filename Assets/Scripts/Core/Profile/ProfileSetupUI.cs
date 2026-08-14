// ProfileSetupUI.cs
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using RTLTMPro;

public class ProfileSetupUI : MonoBehaviour
{
    [Header("Inputs")]
    public RTLTMP_InputField nameInput;  
    public RTLTMP_InputField ageInput;
    public RTLTMP_InputField phoneInput;

    [Header("Input Row Roots (assign parent GameObjects)")]
    public GameObject nameInputRoot;
    public GameObject ageInputRoot;
    public GameObject phoneInputRoot;

    [Header("Gender")]
    public Toggle maleToggle;
    public Toggle femaleToggle;

    [Header("Avatar")]
    public AvatarCarousel avatarCarousel;
    public Button avatarPrevButton;
    public Button avatarNextButton;

    [Header("Buttons")]
    public Button submitButton;
    public Button editButton;

    [Header("Menu Ref")]
    public MainMenuUI mainMenuUI;

    [Header("Feedback (optional)")]
    public GameObject loadingIndicator;
    public TMP_Text errorText;

    [Header("Profile Labels (read-only view, optional)")]
    public RTLTextMeshPro nameLabel;
    public RTLTextMeshPro ageLabel;
    public RTLTextMeshPro phoneLabel;

    [Header("Label Row Roots (assign parent GameObjects)")]
    public GameObject nameLabelRoot;
    public GameObject ageLabelRoot;
    public GameObject phoneLabelRoot;

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

        if (editButton != null)
            editButton.onClick.AddListener(BeginEditProfile);
    }

    // Call this from the Main Menu Profile button after opening the panel
    public void RefreshProfilePanel()
    {
        StopAllCoroutines();
        StartCoroutine(InitProfileViewWhenReady());
    }

    private IEnumerator InitProfileViewWhenReady()
    {
        int frames = 0;
        while (PlayerDataManager.Instance == null && frames < 30)
        {
            frames++;
            yield return null;
        }

        bool hasProfile = PopulateProfileLabels();

        if (hasProfile) ShowReadOnlyMode();
        else ShowEditMode();

        if (editButton != null)
            editButton.interactable = hasProfile;

        Validate();
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
        playerName = nameInput.text.Trim(),
        age = parsedAge,
        avatarIndex = avatarCarousel.CurrentIndex,
        gender = GetGender()
    };

    SetUIBusy(true);

    if (ProfileApiClient.Instance == null)
    {
        SetUIBusy(false);
        ShowError("Profile service is unavailable.");
        yield break;
    }

    var task =
        ProfileApiClient.Instance.RegisterOrRestoreAsync(profile);

    while (!task.IsCompleted)
        yield return null;

    SetUIBusy(false);

    if (task.Result == null)
    {
        ShowError("Could not connect to server. Please try again.");
        yield break;
    }

    PlayerDataManager.Instance.ApplyServerResponse(task.Result);

    PopulateProfileLabels();

    if (errorText != null)
        errorText.gameObject.SetActive(false);

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

    bool PopulateProfileLabels()
    {
        if (PlayerDataManager.Instance == null) return false;

        var profile = PlayerDataManager.Instance.GetProfile();
        if (profile == null) return false;

        if (nameLabel  != null) nameLabel.text  = string.IsNullOrWhiteSpace(profile.playerName) ? "-" : profile.playerName;
        if (ageLabel   != null) ageLabel.text   = profile.age > 0 ? profile.age.ToString() : "-";
        if (phoneLabel != null) phoneLabel.text = string.IsNullOrWhiteSpace(profile.phoneNumber) ? "-" : profile.phoneNumber;

        // use toggles for gender
        if (maleToggle != null)   maleToggle.SetIsOnWithoutNotify(string.Equals(profile.gender, "Male", System.StringComparison.OrdinalIgnoreCase));
        if (femaleToggle != null) femaleToggle.SetIsOnWithoutNotify(string.Equals(profile.gender, "Female", System.StringComparison.OrdinalIgnoreCase));

        // use avatarCarousel for avatar
        ApplyAvatarIndex(profile.avatarIndex);

        return true;
    }

    void ApplyAvatarIndex(int index)
    {
        if (avatarCarousel == null) return;

        // Try common APIs without hard dependency on one method name.
        avatarCarousel.SendMessage("SetIndex", index, SendMessageOptions.DontRequireReceiver);
        avatarCarousel.SendMessage("GoTo", index, SendMessageOptions.DontRequireReceiver);

        var p = avatarCarousel.GetType().GetProperty("CurrentIndex");
        if (p != null && p.CanWrite)
            p.SetValue(avatarCarousel, index);
    }

    public void BeginEditProfile()
    {
        if (PlayerDataManager.Instance == null) return;
        if (PlayerDataManager.Instance.GetProfile() == null) return; // safety
        ShowEditMode();
    }

    void ShowReadOnlyMode()
    {
        SetFieldsVisibility(showInputs: false);
        SetSelectionLocked(true);

        if (editButton != null) editButton.interactable = true;
    }

    void ShowEditMode()
    {
        SetFieldsVisibility(showInputs: true);
        SetSelectionLocked(false);

        Validate();
    }

    void SetSelectionLocked(bool locked)
    {
        if (maleToggle != null) maleToggle.interactable = !locked;
        if (femaleToggle != null) femaleToggle.interactable = !locked;

        if (avatarPrevButton != null) avatarPrevButton.interactable = !locked;
        if (avatarNextButton != null) avatarNextButton.interactable = !locked;
    }

    void SetFieldsVisibility(bool showInputs)
    {
        // Inputs
        SetActiveSmart(nameInputRoot, nameInput != null ? nameInput.gameObject : null, showInputs);
        SetActiveSmart(ageInputRoot, ageInput != null ? ageInput.gameObject : null, showInputs);
        SetActiveSmart(phoneInputRoot, phoneInput != null ? phoneInput.gameObject : null, showInputs);

        // Labels
        SetActiveSmart(nameLabelRoot, nameLabel != null ? nameLabel.gameObject : null, !showInputs);
        SetActiveSmart(ageLabelRoot, ageLabel != null ? ageLabel.gameObject : null, !showInputs);
        SetActiveSmart(phoneLabelRoot, phoneLabel != null ? phoneLabel.gameObject : null, !showInputs);
    }

    void SetActiveSmart(GameObject explicitRoot, GameObject fallbackChild, bool active)
    {
        if (explicitRoot != null)
        {
            explicitRoot.SetActive(active);
            return;
        }

        if (fallbackChild == null) return;
        fallbackChild.SetActive(active);
    }
}
