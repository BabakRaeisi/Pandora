// UIPanelFader.cs
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelFader : MonoBehaviour
{
    public float fadeDuration = 0.25f;

    CanvasGroup cg;
    Tween activeTween;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
    }

    public void FadeIn()
    {
        activeTween?.Kill();

        gameObject.SetActive(true);
        cg.interactable = true;
        cg.blocksRaycasts = true;

        cg.alpha = 0f;
        activeTween = cg.DOFade(1f, fadeDuration)
            .OnComplete(() => activeTween = null);
    }

    public void FadeOut()
    {
        activeTween?.Kill();

        cg.interactable = false;
        cg.blocksRaycasts = false;

        activeTween = cg.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            activeTween = null;
            gameObject.SetActive(false);
        });
    }
}
