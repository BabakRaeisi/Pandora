using UnityEngine;

public class TimedPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private float seconds = 1.5f;

    public void Show()
    {
        if (!root) return;
        root.SetActive(true);
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), Mathf.Max(0.1f, seconds));
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
    }
}
