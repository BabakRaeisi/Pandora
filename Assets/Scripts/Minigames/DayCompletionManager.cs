using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DayCompletionManager : MonoBehaviour
{
    [Header("Session Data")]
    [SerializeField] private SessionDataSO sessionData;

    [Header("UI")]
    [SerializeField] private GameObject sendingPanel;
    [SerializeField] private GameObject finishedButton;
    [SerializeField] private Button proceedButton;
    [SerializeField] private RectTransform proceedButtonVisual;
    [SerializeField] private RectTransform sendingSpinner;

    [Header("Progress UI")]
    [SerializeField] private DayProgressBarUI dayProgressBarUI;

    [Header("Final Day (Key)")]
    [SerializeField] private CanvasGroup keyUI;
    [SerializeField] private float keyFadeDuration = 0.5f;
    [SerializeField] private float keyStayDuration = 1.5f;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string finalAnimationScene = "FinalAnimationScene";

    [Header("Timeout")]
    [Tooltip("Must be greater than DataUploader.requestTimeoutSeconds (default 10s).")]
    [SerializeField] private float sendTimeout = 12f;

    private bool isSending = false;
    private Tween proceedPulseTween;
    private Tween spinnerTween;

    void Start()
    {
        InitializeUI();
        SetupButtonListeners();
        UpdateProceedButtonState();
    }

    void OnDestroy()
    {
        CleanupTweens();
        if (proceedButton != null)
            proceedButton.onClick.RemoveListener(OnProceedClicked);
    }

    #region Initialization

    void InitializeUI()
    {
        if (sendingPanel != null)
            sendingPanel.SetActive(false);

        if (keyUI != null)
        {
            keyUI.alpha = 0f;
            keyUI.gameObject.SetActive(false);
        }
    }

    void SetupButtonListeners()
    {
        if (proceedButton != null)
            proceedButton.onClick.AddListener(OnProceedClicked);
    }

    #endregion

    #region Proceed Button Logic

    void OnProceedClicked()
    {
        if (isSending || !CanProceed())
            return;

        StartCoroutine(CompleteDayRoutine());
    }

    bool CanProceed()
    {
        return PlayerDataManager.Instance.Data.miniGamesCompletedToday >= 3;
    }

    public void UpdateProceedButtonState()
    {
        bool canProceed = CanProceed();

        if (finishedButton != null)
            finishedButton.SetActive(canProceed);

        if (canProceed)
            StartProceedPulse();
        else
            StopProceedPulse();
    }

        #endregion

    #region Day Completion Flow

    IEnumerator CompleteDayRoutine()
    {
        isSending = true;
        DisableProceedButton();

        // Prepare data BEFORE any UI animations or server calls
        SessionUploadRequest uploadRequest = PrepareDataForUpload();
        bool isFinalDay = PlayerDataManager.Instance.Data.currentDay >= 7;

        // Show sending UI
        ShowSendingUI();

        // Animate progress bar
        yield return AnimateProgressBar(isFinalDay);

        // Send data to server
        bool uploadSuccess = false;
        yield return SendDataToServer(uploadRequest, result => uploadSuccess = result);

        if (!uploadSuccess)
        {
            HideSendingUI();
            RestoreProceedButtonAfterFailure();
            isSending = false;
            yield break;
        }

        // Update local data after successful send
        UpdatePlayerDataAfterCompletion(isFinalDay);

        // Clear session data
        ClearSessionData();

        // Hide sending UI
        HideSendingUI();

        // Final day transition or return to menu
        if (isFinalDay)
            yield return ShowKeyAndTransition();
        else
            LoadingScreenController.Instance.LoadScene(mainMenuScene);

        isSending = false;
    }

    SessionUploadRequest PrepareDataForUpload()
    {
        var playerData = PlayerDataManager.Instance.Data;

        // Set completion timestamp FIRST
        playerData.lastDayCompletionTime = DateTime.UtcNow.ToString("o");
        
        // Save immediately to ensure data is persisted
        PlayerDataManager.Instance.Save();

        // Build and return request
        return new SessionUploadRequest(
            playerData.profile,
            playerData,
            sessionData != null ? sessionData.trials : null
        );
    }

    IEnumerator AnimateProgressBar(bool isFinalDay)
    {
        if (dayProgressBarUI != null)
        {
            Tween progressTween = dayProgressBarUI.AnimateDayAdvance(isFinalDay);
            if (progressTween != null)
                yield return progressTween.WaitForCompletion();
        }
    }

    IEnumerator SendDataToServer(SessionUploadRequest request, Action<bool> onComplete)
    {
        if (DataUploader.Instance == null)
        {
            Debug.LogError("[DayCompletionManager] DataUploader not in scene. Queuing for retry.");
            onComplete?.Invoke(EnqueueFallback(request));
            yield break;
        }

        Debug.Log($"[DayCompletionManager] Sending session data — trials: {request.trials?.Count ?? 0}, day: {request.saveData?.currentDay}");

        var uploadTask = DataUploader.Instance.SendAsync(request);
        float timer = 0f;
        float effectiveTimeout = Mathf.Max(sendTimeout, DataUploader.Instance.RequestTimeoutSeconds + 1f);

        // Wait for completion or timeout (sendTimeout must exceed DataUploader.requestTimeoutSeconds)
        while (!uploadTask.IsCompleted && timer < effectiveTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        bool timedOut = !uploadTask.IsCompleted;
        bool success = !timedOut && uploadTask.Result;

        if (!success)
        {
            Debug.LogWarning(timedOut
                ? "[DayCompletionManager] Upload timed out on client side. Adding to offline queue."
                : $"[DayCompletionManager] Upload failed (HTTP {DataUploader.Instance.LastResponseCode}). Adding to offline queue.");
            success = EnqueueFallback(request);
        }

        onComplete?.Invoke(success);
    }

    bool EnqueueFallback(SessionUploadRequest request)
    {
        if (OfflineQueue.Instance == null)
        {
            Debug.LogError("[DayCompletionManager] OfflineQueue not in scene. Session data cannot be queued — data lost!");
            return false;
        }

        OfflineQueue.Instance.Enqueue(JsonUtility.ToJson(request));
        return true;
    }

    void UpdatePlayerDataAfterCompletion(bool isFinalDay)
    {
        var playerData = PlayerDataManager.Instance.Data;

        if (!isFinalDay)
            playerData.currentDay++;
        else
            playerData.programCompleted = true;

        PlayerDataManager.Instance.Save();
    }

    void ClearSessionData()
    {
        if (sessionData != null)
            sessionData.Clear();
    }

    #endregion

    #region UI Animation

    void ShowSendingUI()
    {
        if (sendingPanel != null)
            sendingPanel.SetActive(true);

        StartSpinner();
    }

    void HideSendingUI()
    {
        StopSpinner();

        if (sendingPanel != null)
            sendingPanel.SetActive(false);
    }

    void DisableProceedButton()
    {
        StopProceedPulse();

        if (proceedButton != null)
            proceedButton.interactable = false;

        if (finishedButton != null)
            finishedButton.SetActive(false);
    }

    void RestoreProceedButtonAfterFailure()
    {
        if (proceedButton != null)
            proceedButton.interactable = true;

        UpdateProceedButtonState();
    }

    void StartProceedPulse()
    {
        if (proceedButtonVisual == null || (proceedPulseTween != null && proceedPulseTween.IsActive()))
            return;

        proceedButtonVisual.localScale = Vector3.one;

        proceedPulseTween = proceedButtonVisual
            .DOScale(1.06f, 0.45f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void StopProceedPulse()
    {
        proceedPulseTween?.Kill();
        proceedPulseTween = null;

        if (proceedButtonVisual != null)
            proceedButtonVisual.localScale = Vector3.one;
    }

    void StartSpinner()
    {
        if (sendingSpinner == null)
            return;

        spinnerTween?.Kill();

        spinnerTween = sendingSpinner
            .DORotate(new Vector3(0f, 0f, -360f), 0.8f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    void StopSpinner()
    {
        spinnerTween?.Kill();
        spinnerTween = null;

        if (sendingSpinner != null)
            sendingSpinner.rotation = Quaternion.identity;
    }

    IEnumerator ShowKeyAndTransition()
    {
        if (keyUI == null)
        {
            LoadingScreenController.Instance.LoadScene(finalAnimationScene);
            yield break;
        }

        keyUI.gameObject.SetActive(true);
        keyUI.alpha = 0f;

        yield return keyUI.DOFade(1f, keyFadeDuration).WaitForCompletion();
        yield return new WaitForSeconds(keyStayDuration);

        LoadingScreenController.Instance.LoadScene(finalAnimationScene);
    }

    void CleanupTweens()
    {
        proceedPulseTween?.Kill();
        spinnerTween?.Kill();
    }

    #endregion
}