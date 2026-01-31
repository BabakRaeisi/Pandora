// SWMChest.cs
// Reveal briefly then close (always).
// Re-clicking EMPTY or TREASURE also briefly reveals + lets GameManager show warning + add error icon.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SWMChest : MonoBehaviour
{
    public enum ChestState { Unopened, Treasure, Empty }

    [Header("Sprites")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite treasureOpenSprite; // open chest with gold
    [SerializeField] private Sprite emptyOpenSprite;    // open empty chest

    [Header("Reveal Timing")]
    [SerializeField, Range(0.1f, 2f)] private float revealSeconds = 0.6f;

    [Header("UI Safety")]
    [SerializeField] private bool preserveAspect = true;

    public int Index { get; private set; }
    public ChestState State { get; private set; } = ChestState.Unopened;
    public bool HasTreasure { get; private set; }

    private Image img;
    private Button btn;
   [SerializeField] private SWMGameManager manager;

    private Coroutine routine;
    private bool isRevealing;

    void Awake()
    {
        img = GetComponent<Image>();
        btn = GetComponent<Button>();

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);

            // prevent tinting / transitions
            btn.transition = Selectable.Transition.None;
            if (img != null) btn.targetGraphic = img;
        }

        ApplyUISafeDefaults();
        ForceClosedVisual();
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

        if (routine != null) StopCoroutine(routine);
        routine = null;
        isRevealing = false;

        ApplyUISafeDefaults();
        ForceClosedVisual();
        UpdateInteractivity();
    }

    void OnClick()
    {
        if (isRevealing) return;
        manager?.OnChestPressed(this);
    }

    /// <summary>
    /// First-time open: locks state + reveals briefly then closes.
    /// Call only when State == Unopened.
    /// </summary>
    public void RevealFirstTime()
    {
        if (State != ChestState.Unopened) return;

        State = HasTreasure ? ChestState.Treasure : ChestState.Empty;
        StartRevealRoutine(State);
    }

    /// <summary>
    /// Re-open briefly without changing state (EMPTY or TREASURE re-click).
    /// </summary>
    public void RevealAgain()
    {
        if (State == ChestState.Unopened) return;
        StartRevealRoutine(State);
    }

    void StartRevealRoutine(ChestState revealState)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(RevealThenCloseRoutine(revealState));
    }

    IEnumerator RevealThenCloseRoutine(ChestState revealState)
    {
        isRevealing = true;
        UpdateInteractivity(); // blocks spamming via isRevealing anyway

        ApplyUISafeDefaults();

        if (img != null)
        {
            img.sprite = (revealState == ChestState.Treasure) ? treasureOpenSprite : emptyOpenSprite;
            img.color = Color.white;
        }

        yield return new WaitForSeconds(Mathf.Max(0.1f, revealSeconds));

        ForceClosedVisual();

        isRevealing = false;
        UpdateInteractivity();
        routine = null;
    }

    void ForceClosedVisual()
    {
        ApplyUISafeDefaults();
        if (img != null)
        {
            img.sprite = closedSprite;
            img.color = Color.white;
        }
    }

    void UpdateInteractivity()
    {
        if (btn == null) return;

        // You want BOTH EMPTY and TREASURE to be clickable so re-click shows warning + adds error.
        btn.interactable = true;
    }

    void ApplyUISafeDefaults()
    {
        if (img == null) return;
        img.material = null;
        img.color = Color.white;
        img.type = Image.Type.Simple;
        img.preserveAspect = preserveAspect;
        img.raycastTarget = true;
    }
}
