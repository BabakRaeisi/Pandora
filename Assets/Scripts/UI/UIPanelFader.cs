// UIPanelFader.cs
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelFader : MonoBehaviour
{
    public float fadeDuration = 0.25f;

    CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
    }

    public void FadeIn()
    {
        gameObject.SetActive(true);
        cg.interactable = true;
        cg.blocksRaycasts = true;

        cg.alpha = 0f;
        cg.DOFade(1f, fadeDuration);
    }

    public void FadeOut()
    {
        cg.interactable = false;
        cg.blocksRaycasts = false;

        cg.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
