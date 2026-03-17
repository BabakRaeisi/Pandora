using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum BridgePieceState
{
    Idle,
    Highlighted,
    Built,
    Error
}

[DisallowMultipleComponent]
public class BridgePieceUI : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int id;
    public int Id => id;

    [Header("Grid (assigned by manager)")]
    [SerializeField] private int row = -1;
    [SerializeField] private int col = -1;
    public int Row => row;
    public int Col => col;

    [Header("UI")]
    [SerializeField] private Image image;
    [SerializeField] private Button button;

    [Header("Opacity")]
    [SerializeField, Range(0f, 1f)] private float idleOpacity = 0.70f;
    [SerializeField, Range(0f, 1f)] private float activeOpacity = 1.00f;

    [Header("Tints")]
    [SerializeField] private Color idleTint = Color.white;
    [SerializeField] private Color highlightTint = Color.white;
    [SerializeField] private Color builtTint = Color.white;
    [SerializeField] private Color errorTint = Color.red;

    [Header("Sprites (optional)")]
    [SerializeField] private Sprite baseSprite;
    [SerializeField] private Sprite highlightedSprite;
    [SerializeField] private Sprite builtSprite;

    public BridgePieceState State { get; private set; } = BridgePieceState.Idle;
    public event Action<BridgePieceUI> Clicked;

    private Coroutine errorRoutine;

    private void Reset()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (!image) image = GetComponent<Image>();
        if (!button) button = GetComponent<Button>();

        if (!image)
        {
            Debug.LogError($"BridgePieceUI '{name}' missing Image.");
            enabled = false;
            return;
        }

        if (!button)
        {
            Debug.LogError($"BridgePieceUI '{name}' missing Button.");
            enabled = false;
            return;
        }

        if (!baseSprite) baseSprite = image.sprite;

        button.onClick.AddListener(OnButtonClicked);
        ApplyVisual(State);
    }

    private void OnDestroy()
    {
        if (button) button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked() => Clicked?.Invoke(this);

    public void SetGrid(int newRow, int newCol)
    {
        row = newRow;
        col = newCol;
    }

    public void SetInteractable(bool value)
    {
        if (button) button.interactable = value;
    }

    public void SetState(BridgePieceState state)
    {
        if (errorRoutine != null)
        {
            StopCoroutine(errorRoutine);
            errorRoutine = null;
        }

        State = state;
        ApplyVisual(state);
    }

    public void FlashError(float seconds = 0.12f)
    {
        if (!gameObject.activeInHierarchy) return;

        if (errorRoutine != null) StopCoroutine(errorRoutine);
        errorRoutine = StartCoroutine(FlashErrorCo(seconds));
    }

    private IEnumerator FlashErrorCo(float seconds)
    {
        var prev = State;
        ApplyVisual(BridgePieceState.Error);
        yield return new WaitForSeconds(seconds);
        ApplyVisual(prev);
        errorRoutine = null;
    }

    private void ApplyVisual(BridgePieceState state)
    {
        Sprite s = baseSprite;
        if (state == BridgePieceState.Highlighted && highlightedSprite) s = highlightedSprite;
        if (state == BridgePieceState.Built && builtSprite) s = builtSprite;
        image.sprite = s;

        Color tint;
        float alpha;

        switch (state)
        {
            case BridgePieceState.Highlighted:
                tint = highlightTint; alpha = activeOpacity; break;
            case BridgePieceState.Built:
                tint = builtTint; alpha = activeOpacity; break;
            case BridgePieceState.Error:
                tint = errorTint; alpha = activeOpacity; break;
            default:
                tint = idleTint; alpha = idleOpacity; break;
        }

        tint.a = alpha;
        image.color = tint;
    }
}