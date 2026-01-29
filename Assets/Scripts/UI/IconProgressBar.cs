using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IconProgressBar : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private RectTransform container;
    [SerializeField] private Image iconPrefab;

    [Header("Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite filledSprite;

    private readonly List<Image> icons = new();
    private int total = 0;

    // Call once (e.g. day start)
    public void Setup(int totalCount)
    {
        total = totalCount;

        // Clear old
        for (int i = 0; i < icons.Count; i++)
        {
            if (icons[i])
                Destroy(icons[i].gameObject);
        }
        icons.Clear();

        // Build new icons
        for (int i = 0; i < total; i++)
        {
            Image img = Instantiate(iconPrefab, container);
            img.sprite = emptySprite;
            img.color = Color.white;
            img.gameObject.SetActive(true);
            icons.Add(img);
        }
    }

    // Call to update progress
    public void SetFilled(int filledCount)
    {
        for (int i = 0; i < icons.Count; i++)
        {
            if (!icons[i]) continue;

            icons[i].sprite = (i < filledCount)
                ? filledSprite
                : emptySprite;
        }
    }

    // ---- TESTS ----
    [ContextMenu("TEST Setup 7")]
    private void TestSetup7()
    {
        Setup(7);
    }

    [ContextMenu("TEST Fill 3")]
    private void TestFill3()
    {
        SetFilled(3);
    }

    [ContextMenu("TEST Fill All")]
    private void TestFillAll()
    {
        SetFilled(total);
    }
}