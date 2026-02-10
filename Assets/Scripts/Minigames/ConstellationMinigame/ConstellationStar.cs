using UnityEngine;
using UnityEngine.UI;

public class ConstellationStar : MonoBehaviour
{
    public int id;                  // 1..9
    public Button button;
    public Image image;

    [Header("Sprites")]
    public Sprite dimSprite;
    public Sprite glowSprite;

    public RectTransform Rect => (RectTransform)transform;

    void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (!image) image = GetComponent<Image>();
        SetDim();
    }

    public void SetDim()
    {
        image.sprite = dimSprite;
        image.enabled = true;
    }

    public void SetGlow()
    {
        image.sprite = glowSprite;
        image.enabled = true;
    }
}
