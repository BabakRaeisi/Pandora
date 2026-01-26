// SWMChest.cs
// Document-aligned behavior for Between Errors:
// - UNOPENED: clickable -> reveals treasure/empty
// - TREASURE: NOT clickable (no need to re-tap)
// - EMPTY: clickable (so tapping it again counts a Between Error in GameManager)
//
// Attach this to the same GameObject that has:
// - Image
// - Button

using UnityEngine;
using UnityEngine.UI;

public class SWMChest : MonoBehaviour
{
    public enum ChestState { Unopened, Treasure, Empty }

    [Header("Sprites")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite treasureSprite;
    [SerializeField] private Sprite emptySprite;

    [Header("Optional")]
    [SerializeField] private bool preserveAspect = true;

    public int Index { get; private set; }
    public ChestState State { get; private set; } = ChestState.Unopened;
    public bool HasTreasure { get; private set; }

    private Image img;
    private Button btn;
    private SWMGameManager manager;

    void Awake()
    {
        img = GetComponent<Image>();
        btn = GetComponent<Button>();

        if (img == null)
            Debug.LogError($"{name}: Missing Image component.");

        if (btn == null)
            Debug.LogError($"{name}: Missing Button component.");

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);

            // Avoid tint/transition issues
            btn.transition = Selectable.Transition.None;

            if (img != null)
                btn.targetGraphic = img;
        }

        ApplyUISafeDefaults();
        UpdateVisual();
        UpdateInteractivity();
    }

    public void Init(int index, SWMGameManager gm)
    {
        Index = index;
        manager = gm;
    }

    public void ResetForTrial(bool hasTreasure)
    {
        HasTreasure = hasTreasure;
        State = ChestState.Unopened;

        ApplyUISafeDefaults();
        UpdateVisual();
        UpdateInteractivity();
    }

    public void Reveal()
    {
        if (State != ChestState.Unopened) return;

        State = HasTreasure ? ChestState.Treasure : ChestState.Empty;

        ApplyUISafeDefaults();
        UpdateVisual();
        UpdateInteractivity();
    }

    // Optional feedback hook you can expand later (shake/glow)
    public void FlashEmptyFeedback()
    {
        // intentionally empty
    }

    private void OnClick()
    {
        if (manager == null) return;
        manager.OnChestPressed(this);
    }

    private void UpdateInteractivity()
    {
        if (btn == null) return;

        // Key rule:
        // - Keep EMPTY clickable so GameManager can count Between Errors.
        // - Disable TREASURE to reduce pointless taps.
        // - UNOPENED must be clickable.
        btn.interactable = (State == ChestState.Unopened) || (State == ChestState.Empty);
    }

    private void UpdateVisual()
    {
        if (img == null) return;

        ApplyUISafeDefaults();

        Sprite s = State switch
        {
            ChestState.Unopened => closedSprite,
            ChestState.Treasure => treasureSprite,
            ChestState.Empty => emptySprite,
            _ => closedSprite
        };

        if (s == null)
        {
            Debug.LogError($"{name}: Missing sprite for state {State}. Assign sprites in inspector.");
            return;
        }

        img.sprite = s;
    }

    private void ApplyUISafeDefaults()
    {
        if (img == null) return;

        img.material = null;
        img.color = Color.white;
        img.type = Image.Type.Simple;
        img.preserveAspect = preserveAspect;
        img.raycastTarget = true;
    }
}
