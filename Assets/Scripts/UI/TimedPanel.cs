using System.Collections;
using DG.Tweening;
using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TimedPanelMode
{
	Fade,
	Slide
}

public class TimedPanel : MonoBehaviour
{
	[Header("Refs")]
	[SerializeField] private GameObject root;
	[SerializeField] private RectTransform panel;
	[SerializeField] private CanvasGroup canvasGroup;

	[Header("Optional UI")]
	[SerializeField] private RTLTextMeshPro titleText;
	[SerializeField] private RTLTextMeshPro messageText;
	[SerializeField] private Image iconImage;

	[Header("Defaults (Used when Show() has null args)")]
	[SerializeField] private string defaultTitle;
	[SerializeField] private string defaultMessage;
	[SerializeField] private Sprite defaultIcon;

	[Header("Behavior")]
	[SerializeField] private TimedPanelMode mode = TimedPanelMode.Fade;
	[SerializeField] private float transitionDuration = 0.22f;
	[SerializeField] private float visibleSeconds = 1.5f;
	[SerializeField] private float minVisibleSeconds = 0.1f;

	[Header("Slide Positions")]
	[SerializeField] private Vector2 shownAnchoredPos;
	[SerializeField] private Vector2 hiddenAnchoredPos = new Vector2(0f, -220f);

	private Tween transitionTween;
	private Coroutine autoHideRoutine;

	void Awake()
	{
		ResolveReferences();

		if (mode == TimedPanelMode.Slide && panel != null)
			panel.anchoredPosition = hiddenAnchoredPos;

		if (canvasGroup != null)
			canvasGroup.alpha = 0f;

		if (root != null)
			root.SetActive(false);
	}

	void OnValidate()
	{
		ResolveReferences();
	}

	public void Show()
	{
		Show(null, null, null);
	}

	public void Show(string message)
	{
		Show(message, null, null);
	}

	public void Show(string message, Sprite icon, string title)
	{
		ResolveReferences();
		if (root == null)
			return;

		ApplyContent(
			title: title ?? defaultTitle,
			message: message ?? defaultMessage,
			icon: icon != null ? icon : defaultIcon);

		if (autoHideRoutine != null)
		{
			StopCoroutine(autoHideRoutine);
			autoHideRoutine = null;
		}

		transitionTween?.Kill();
		root.SetActive(true);

		if (mode == TimedPanelMode.Slide)
		{
			if (panel == null)
			{
				Debug.LogWarning("[TimedPanel] Slide mode requires a Panel RectTransform.");
				return;
			}

			if (canvasGroup != null)
				canvasGroup.alpha = 1f;

			panel.anchoredPosition = hiddenAnchoredPos;
			transitionTween = panel.DOAnchorPos(shownAnchoredPos, transitionDuration).SetEase(Ease.OutCubic);
		}
		else
		{
			if (canvasGroup == null)
			{
				canvasGroup = root.GetComponent<CanvasGroup>();
				if (canvasGroup == null)
					canvasGroup = root.AddComponent<CanvasGroup>();
			}

			canvasGroup.alpha = 0f;
			transitionTween = canvasGroup.DOFade(1f, transitionDuration).SetEase(Ease.OutCubic);
		}

		autoHideRoutine = StartCoroutine(AutoHide());
	}

	public void Hide()
	{
		ResolveReferences();
		if (root == null)
			return;

		if (autoHideRoutine != null)
		{
			StopCoroutine(autoHideRoutine);
			autoHideRoutine = null;
		}

		transitionTween?.Kill();

		if (mode == TimedPanelMode.Slide)
		{
			if (panel == null)
			{
				root.SetActive(false);
				return;
			}

			transitionTween = panel
				.DOAnchorPos(hiddenAnchoredPos, transitionDuration)
				.SetEase(Ease.InCubic)
				.OnComplete(() =>
				{
					if (root != null)
						root.SetActive(false);
				});
			return;
		}

		if (canvasGroup == null)
		{
			root.SetActive(false);
			return;
		}

		transitionTween = canvasGroup
			.DOFade(0f, transitionDuration)
			.SetEase(Ease.InCubic)
			.OnComplete(() =>
			{
				if (root != null)
					root.SetActive(false);
			});
	}

	private IEnumerator AutoHide()
	{
		yield return new WaitForSeconds(Mathf.Max(minVisibleSeconds, visibleSeconds));
		autoHideRoutine = null;
		Hide();
	}

	private void ApplyContent(string title, string message, Sprite icon)
	{
		if (titleText != null)
		{
			bool hasTitle = !string.IsNullOrEmpty(title);
			titleText.gameObject.SetActive(hasTitle);
			titleText.text = hasTitle ? title : string.Empty;
		}

		if (messageText != null)
		{
			bool hasMessage = !string.IsNullOrEmpty(message);
			messageText.gameObject.SetActive(hasMessage);
			messageText.text = hasMessage ? message : string.Empty;
		}

		if (iconImage != null)
		{
			bool hasIcon = icon != null;
			iconImage.gameObject.SetActive(hasIcon);
			if (hasIcon)
				iconImage.sprite = icon;
		}
	}

	private void ResolveReferences()
	{
		if (root == null)
			root = gameObject;

		if (panel == null)
			panel = GetComponent<RectTransform>();

		if (canvasGroup == null && root != null)
			canvasGroup = root.GetComponent<CanvasGroup>();
	}

	void OnDestroy()
	{
		transitionTween?.Kill();
	}
}
