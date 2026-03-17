// ProfileSetupUI.cs
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

    void Start()
    {
        submitButton.interactable = false;

        nameInput.onValueChanged.AddListener(_ => Validate());
        ageInput.onValueChanged.AddListener(_ => Validate());
        phoneInput.onValueChanged.AddListener(_ => Validate());

        maleToggle.onValueChanged.AddListener(_ => Validate());
        femaleToggle.onValueChanged.AddListener(_ => Validate());
    }

    void Validate()
    {
        bool nameValid = !string.IsNullOrWhiteSpace(nameInput.text);
        bool ageValid = !string.IsNullOrWhiteSpace(ageInput.text);
        bool phoneValid = !string.IsNullOrWhiteSpace(phoneInput.text);
        bool genderValid = maleToggle.isOn || femaleToggle.isOn;

        submitButton.interactable = nameValid && ageValid && phoneValid && genderValid;
    }

    public void SubmitProfile()
    {
        int.TryParse(ageInput.text, out int parsedAge);

        PlayerProfile profile = new PlayerProfile
        {
            playerId = System.Guid.NewGuid().ToString(),
            playerName = nameInput.text.Trim(),
            age = parsedAge,
            phoneNumber = phoneInput.text.Trim(),
            avatarIndex = avatarCarousel.CurrentIndex,
            gender = GetGender()
        };

        PlayerDataManager.Instance.SetProfile(profile);

        mainMenuUI.BackToMainFromSetup();
       
    }

    public void Cancel()
    {
        mainMenuUI.BackToMainFromSetup();
    }

    string GetGender()
    {
        if (maleToggle.isOn) return "Male";
        if (femaleToggle.isOn) return "Female";
        return "";
    }
}
