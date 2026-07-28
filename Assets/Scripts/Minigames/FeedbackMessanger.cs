using System;
using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackMessanger : MonoBehaviour
{
    [Header("Toast Ref")]
    [SerializeField] private ToastPanel toastPanel;

    [Header("Outcome UI")]
    [SerializeField] private RTLTextMeshPro LevelMessage;
    [SerializeField] private RTLTextMeshPro TitleMessage;
    [SerializeField] private Image loomakImage;
    [SerializeField] private Image keyImage;

    [Header("Default Sprites")]
    [SerializeField] private Sprite successLoomak;
    [SerializeField] private Sprite assistedLoomak;
    [SerializeField] private Sprite key;

    [Header("Title Colors")]
    [SerializeField] private Color successTitleColor = new Color(0.20f, 0.85f, 0.35f);
    [SerializeField] private Color assistedTitleColor = new Color(1.00f, 0.45f, 0.65f);

    [Header("Common Error Messages (FA)")]
    [TextArea] [SerializeField] private string wrongPatternMessageFa = "الگو اشتباه بود";
    [TextArea] [SerializeField] private string genericErrorMessageFa = "مشکلی پیش آمد.دوباره تلاش کنید.";

    private void Awake()
    {
    }

    // Root panel activation/deactivation is handled elsewhere.
    // This just clears UI content/state.
    public void HideOutcomePanel()
    {
        if (TitleMessage != null)
        {
            TitleMessage.text = string.Empty;
            TitleMessage.gameObject.SetActive(false);
        }

        if (LevelMessage != null)
            LevelMessage.text = string.Empty;

        if (loomakImage != null)
            loomakImage.gameObject.SetActive(false);

        if (keyImage != null)
            keyImage.gameObject.SetActive(false);
    }

    public void ShowOutcomePanel(
        string title,
        string message,
        bool assisted,
        bool showKey,
        Sprite loomakOverride = null,
        Sprite keyOverride = null)
    {
        // Title (optional)
        if (TitleMessage != null)
        {
            bool hasTitle = !string.IsNullOrWhiteSpace(title);
            TitleMessage.gameObject.SetActive(hasTitle);

            if (hasTitle)
            {
                TitleMessage.text = title.Trim();
                TitleMessage.color = assisted ? assistedTitleColor : successTitleColor;
            }
            else
            {
                TitleMessage.text = string.Empty;
            }
        }

        // Main message (required)
        if (LevelMessage != null)
            LevelMessage.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();

        // Loomak image: fixed by state unless override provided
        if (loomakImage != null)
        {
            Sprite fallback = assisted ? assistedLoomak : successLoomak;
            Sprite selected = loomakOverride != null ? loomakOverride : fallback;

            loomakImage.sprite = selected;
            loomakImage.gameObject.SetActive(selected != null);
        }

        // Key image: shown only when requested
        if (keyImage != null)
        {
            keyImage.gameObject.SetActive(showKey);
            if (showKey)
                keyImage.sprite = keyOverride != null ? keyOverride : key;
        }
    }

    // Optional overload for message-only outcome
    public void ShowOutcomePanel(string message, bool assisted, bool showKey, Sprite loomakOverride = null, Sprite keyOverride = null)
    {
        ShowOutcomePanel(string.Empty, message, assisted, showKey, loomakOverride, keyOverride);
    }

    public void ShowWrongPattern() =>
        toastPanel?.Show(wrongPatternMessageFa, ToastPanel.ToastType.Warning);

    public void ShowWrongPattern(string title, string message)
    {
        if (toastPanel == null) return;

        // config-driven only
        if (string.IsNullOrWhiteSpace(message))
        {  return;
        }

        string t = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        string m = message.Trim();

        if (string.IsNullOrEmpty(t))
            toastPanel.Show(m, ToastPanel.ToastType.Warning);
        else
            toastPanel.Show(t, m, ToastPanel.ToastType.Warning);
    }

    public void ShowGenericError() =>
        toastPanel?.Show(genericErrorMessageFa, ToastPanel.ToastType.Warning);

    public void ShowSuccess(string title, string message)
    {
        string finalTitle = string.IsNullOrWhiteSpace(title) ? "آفرین" : title.Trim();
        string finalMessage = string.IsNullOrWhiteSpace(message) ? "عالی بود،ادامه بده." : message.Trim();

        if (toastPanel == null)
        {
             return;
        }

        toastPanel.Show(finalTitle, finalMessage, ToastPanel.ToastType.Success);
    }

    public void ShowInfo(string title, string message)
    {
        string finalTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        string finalMessage = string.IsNullOrWhiteSpace(message) ? "ادامه بده." : message.Trim();

        if (toastPanel == null)
        {
             return;
        }

        toastPanel.Show(finalTitle, finalMessage, ToastPanel.ToastType.Info);
    }
}