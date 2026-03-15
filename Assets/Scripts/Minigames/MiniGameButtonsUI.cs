// MiniGameButtonsUI.cs
using UnityEngine;
using UnityEngine.UI;

public class MiniGameButtonsUI : MonoBehaviour
{
    [System.Serializable]
    public class MiniGameButton
    {
        public Button button;
        public Image icon;
    }

    public MiniGameButton[] buttons;
    public Sprite playSprite;
    public Sprite lockSprite;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        int completed = PlayerDataManager.Instance.Data.miniGamesCompletedToday;

        for (int i = 0; i < buttons.Length; i++)
        {
            bool unlocked = (i <= completed) && completed < 3;

            if (buttons[i].button != null)
                buttons[i].button.interactable = unlocked;

            if (buttons[i].icon != null)
                buttons[i].icon.sprite = unlocked ? playSprite : lockSprite;
        }

        if (completed >= 3)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].button != null)
                    buttons[i].button.interactable = false;

                if (buttons[i].icon != null)
                    buttons[i].icon.sprite = lockSprite;
            }
        }
    }
}