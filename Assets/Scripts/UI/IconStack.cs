// IconStack.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IconStack : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform container;
    [SerializeField] private Image iconPrefab;

    [Header("Sprite")]
    [SerializeField] private Sprite iconSprite;

    [Header("Defaults")]
    [SerializeField] private Vector2 fallbackSize = new Vector2(40, 40);

    private readonly List<Image> icons = new();
    private bool errorShown = false;

    public void Clear()
    {
        for (int i = 0; i < icons.Count; i++)
            if (icons[i]) Destroy(icons[i].gameObject);

        icons.Clear();
        errorShown = false;

        if (container) LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }

    public void AddOne()
    {
        if (errorShown) return;   // prevents accumulating errors

        if (!container || !iconPrefab || !iconSprite) return;

        var img = Instantiate(iconPrefab, container);
        img.sprite = iconSprite;
        img.color = Color.white;

        var rt = img.rectTransform;
        rt.localScale = Vector3.one;
        if (rt.sizeDelta == Vector2.zero) rt.sizeDelta = fallbackSize;

        img.gameObject.SetActive(true);
        icons.Add(img);

        errorShown = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }
}