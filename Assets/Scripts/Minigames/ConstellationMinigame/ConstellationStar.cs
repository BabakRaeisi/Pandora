using UnityEngine;
using UnityEngine.UI;

public class ConstellationStar : MonoBehaviour
{
    public int id;            // 1..9
    public Button button;
    public Image image;
    public RectTransform Rect;

    [Range(0f, 1f)] public float dimAlpha = 0.3f;

    void Awake()
    {
          Rect = (RectTransform)transform;
        if (!button) button = GetComponent<Button>();
        if (!image) image = GetComponent<Image>();
        SetDim();
    }

    public void SetDim()
    {
        Color c = image.color;
        c.a = dimAlpha;
        image.color = c;
    }

    public void SetBright()
    {
        Color c = image.color;
        c.a = 1f;
        image.color = c;
    }
}
