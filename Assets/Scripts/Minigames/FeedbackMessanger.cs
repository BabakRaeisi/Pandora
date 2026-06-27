using System;
using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackMessanger : MonoBehaviour
{
    [Header("Toast Ref")]
    [SerializeField] private ToastPanel toastPanel;

 
    [SerializeField] private RTLTextMeshPro LevelMessage;
    [SerializeField] private RTLTextMeshPro TitleMessage;
    [SerializeField] private Image loomakImage;
    [SerializeField] private Image keyImage;

    [Header("Default Sprites")]
    [SerializeField] public Sprite Loomak;
    [SerializeField] public Sprite key;

    [Header("Title Colors")]
    [SerializeField] private Color successTitleColor = new Color(0.20f, 0.85f, 0.35f);
    [SerializeField] private Color assistedTitleColor = new Color(1.00f, 0.45f, 0.65f);

    [Header("Common Error Messages (FA)")]
    [TextArea] [SerializeField] private string wrongPatternMessageFa = "الگو اشتباه بود";
    [TextArea] [SerializeField] private string genericErrorMessageFa = "مشکلی پیش آمد. دوباره تلاش کنید.";

    private void Awake()
    {
        Debug.Log($"[FeedbackMessanger] Awake on '{name}' | toastPanel={(toastPanel ? toastPanel.name : "NULL")}");
        // root activation is managed elsewhere
        // if (outcomePanelRoot != null) outcomePanelRoot.SetActive(false);
    }

    public void HideOutcomePanel()
    {
        // root activation is managed elsewhere
        // if (outcomePanelRoot != null) outcomePanelRoot.SetActive(false);
    }

    public void ShowOutcomePanel(
        string title,
        string message,
        bool assisted,
        bool showKey,
        Sprite loomakOverride = null,
        Sprite keyOverride = null)
    {
        if (TitleMessage != null)
        {
            TitleMessage.text = title ?? string.Empty;
            TitleMessage.color = assisted ? assistedTitleColor : successTitleColor;
        }

        if (LevelMessage != null)
            LevelMessage.text = message ?? string.Empty;

        if (loomakImage != null)
        {
            var s = loomakOverride != null ? loomakOverride : Loomak;
            loomakImage.sprite = s;
            loomakImage.gameObject.SetActive(s != null);
        }

        if (keyImage != null)
        {
            keyImage.gameObject.SetActive(showKey);
            if (showKey)
            {
                var s = keyOverride != null ? keyOverride : key;
                keyImage.sprite = s;
            }
        }

        // root activation is managed elsewhere
        // if (outcomePanelRoot != null) outcomePanelRoot.SetActive(true);
    }

    public void ShowWrongPattern() =>
        toastPanel?.Show(wrongPatternMessageFa, ToastPanel.ToastType.Warning);

    public void ShowWrongPattern(string customMessageFa) =>
        toastPanel?.Show(
            string.IsNullOrWhiteSpace(customMessageFa) ? wrongPatternMessageFa : customMessageFa,
            ToastPanel.ToastType.Warning
        );

    public void ShowGenericError() =>
        toastPanel?.Show(genericErrorMessageFa, ToastPanel.ToastType.Warning);

    // Keep for trial-level toasts
    public void ShowSuccess(string title, string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            toastPanel?.Show(title ?? string.Empty, message, ToastPanel.ToastType.Success);
    }

    public void ShowInfo(string title, string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            toastPanel?.Show(title ?? string.Empty, message, ToastPanel.ToastType.Info);
    }
}