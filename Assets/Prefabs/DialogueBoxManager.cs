using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // Needed for UnityEvent
using RTLTMPro;

public class DialogueBoxManager : MonoBehaviour
{
    [System.Serializable]
    public struct DialogueStep
    {
        [TextArea(3, 5)]
        public string message;
        public Sprite characterFace; 
        public bool showKey;         
    }

    [Header("Dialogue Data")]
    [SerializeField] private DialogueStep[] dialogues; 

    [Header("UI Components")]
    [SerializeField] private RectTransform panelRect;       
    [SerializeField] private RTLTextMeshPro messageText;    
    [SerializeField] private Image characterImage;          
    [SerializeField] private Button nextButton;             
    [SerializeField] private Image keyImage;               

    [Header("Sizing Settings")]
    [SerializeField] private float paddingHeight = 100f;    
    [SerializeField] private float minPanelHeight = 200f;   

    // ─── ASSIGN YOUR CUSTOM FINAL FUNCTION HERE ──────────────────────────────
    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueComplete;
    // ─────────────────────────────────────────────────────────────────────────

    private int currentIndex = 0;

    private void Awake()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }
        
        gameObject.SetActive(false);
    }

    public void StartLevelIntroduction()
    {
        if (dialogues == null || dialogues.Length == 0)
        {
            gameObject.SetActive(false);
            if (keyImage != null) keyImage.gameObject.SetActive(false);
            return;
        }

        currentIndex = 0;
        
        gameObject.SetActive(true);
        if (nextButton != null) nextButton.gameObject.SetActive(true);

        DisplayCurrentStep();
    }

    private void OnNextClicked()
    {
        currentIndex++;

        if (currentIndex < dialogues.Length)
        {
            DisplayCurrentStep();
        }
        else
        {
            EndDialogue();
        }
    }

    private void DisplayCurrentStep()
    {
        DialogueStep currentStep = dialogues[currentIndex];

        if (messageText != null)
        {
            messageText.text = currentStep.message;
            messageText.ForceMeshUpdate();
        }

        if (characterImage != null)
        {
            if (currentStep.characterFace != null)
            {
                characterImage.gameObject.SetActive(true);
                characterImage.sprite = currentStep.characterFace;
            }
            else
            {
                characterImage.gameObject.SetActive(false);
            }
        }

        if (keyImage != null)
        {
            keyImage.gameObject.SetActive(currentStep.showKey);
        }

        if (panelRect != null && messageText != null)
        {
            float textHeight = messageText.preferredHeight;
            float calculatedHeight = Mathf.Max(minPanelHeight, textHeight + paddingHeight);
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, calculatedHeight);
        }
    }

    private void EndDialogue()
    {
        gameObject.SetActive(false);
        if (keyImage != null) keyImage.gameObject.SetActive(false);

        // Triggers your custom manual function set in the Inspector
        if (onDialogueComplete != null)
        {
            onDialogueComplete.Invoke();
        }
    }
}