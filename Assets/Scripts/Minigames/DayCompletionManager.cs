using System.Collections;
using UnityEngine;

public class DayCompletionManager : MonoBehaviour
{
    [Header("Session Data")]
    [SerializeField] private SessionDataSO sessionData;

    [Header("UI")]
    [SerializeField] private GameObject sendingPanel;
    [SerializeField] private GameObject finishedButton; // button to go to final animation

    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string finalAnimationScene = "FinalAnimationScene";

    bool sending = false;

    void Start()
    {
        var data = PlayerDataManager.Instance.Data;

        // Player already finished the 7-day program
        if (data.currentDay > 7)
        {
            ShowCompletedState();
        }
    }

    void Update()
    {
        if (sending) return;

        var data = PlayerDataManager.Instance.Data;

        if (data.miniGamesCompletedToday >= 3)
        {
            StartCoroutine(SendDayData());
        }
    }

    void ShowCompletedState()
    {
        if (finishedButton != null)
            finishedButton.SetActive(true);
    }

    IEnumerator SendDayData()
    {
        sending = true;

        if (sendingPanel)
            sendingPanel.SetActive(true);

        var data = PlayerDataManager.Instance.Data;

        // store completion time
        data.lastDayCompletionTime = System.DateTime.UtcNow.ToString("o");

        bool finalDay = data.currentDay >= 7;

        if (!finalDay)
            data.currentDay += 1;
        else
            data.currentDay = 8; // mark program finished

        PlayerDataManager.Instance.Save();

        // simulate server upload
        yield return new WaitForSeconds(3f);

        if (sessionData != null)
            sessionData.Clear();

        if (finalDay)
        {
            // go to final animation
            LoadingScreenController.Instance.LoadScene(finalAnimationScene);
        }
        else
        {
            LoadingScreenController.Instance.LoadScene(mainMenuScene);
        }
    }

    public void GoToFinalAnimation()
    {
        LoadingScreenController.Instance.LoadScene(finalAnimationScene);
    }
}