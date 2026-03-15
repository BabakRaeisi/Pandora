// ProfileDetailsUI.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProfileDetailsUI : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text ageText;
    public TMP_Text phoneText;
    public TMP_Text genderText;
    public Image avatarImage;
    public Sprite[] avatarSprites;

    public void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        var data = PlayerDataManager.Instance.Data;

        if (data == null || data.profile == null)
            return;

        playerNameText.text = data.profile.playerName;
        ageText.text = data.profile.age.ToString();
        phoneText.text = data.profile.phoneNumber;
        genderText.text = data.profile.gender;

        int index = data.profile.avatarIndex;
        if (index >= 0 && index < avatarSprites.Length)
            avatarImage.sprite = avatarSprites[index];
    }

    public void ClosePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);
    }
}