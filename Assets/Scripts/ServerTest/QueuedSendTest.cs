using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to a GameObject that also has OfflineQueue and DataUploader in the scene.
/// In Play Mode, right-click this component to enqueue test data and watch the queue
/// drain automatically via OfflineQueue's 5-second retry loop.
///
/// Test cases:
///   "Enqueue Single Item"   — adds one request and lets the queue process it.
///   "Enqueue Multiple Items" — stress-tests the queue with 3 sequential items.
///   "Check Queue Status"    — logs how many items are still pending.
/// </summary>
public class QueuedSendTest : MonoBehaviour
{
    [Header("Test result (read-only)")]
    [SerializeField] private string lastResult = "Not run yet";

    [Header("Batch")]
    [Min(1)]
    [SerializeField] private int multipleItemCount = 3;

    [Header("Live Gameplay Data")]
    [SerializeField] private SessionDataSO sessionData;

    [Header("Timeout")]
    [Tooltip("Must be greater than DataUploader request timeout.")]
    [SerializeField] private float sendTimeout = 12f;

    // ── ContextMenu entry points ────────────────────────────────────────────

    [ContextMenu("Enqueue Single Item")]
    public void EnqueueSingleItem()
    {
        if (!CheckQueueDependencies())
            return;

        EnqueueRequest(BuildTestRequest(tag: "single"));
        lastResult = $"Enqueued 1 item. Queue pending: {OfflineQueue.Instance.HasPending()}";
    }

    [ContextMenu("Enqueue Multiple Items")]
    public void EnqueueMultipleItems()
    {
        if (!CheckQueueDependencies())
            return;

        for (int i = 0; i < multipleItemCount; i++)
            EnqueueRequest(BuildTestRequest(tag: $"batch-{i + 1}", dayOffset: i));

        lastResult = $"Enqueued {multipleItemCount} items. Queue pending: {OfflineQueue.Instance.HasPending()}";
    }

    [ContextMenu("Enqueue Live Gameplay Payload")]
    public void EnqueueLiveGameplayPayload()
    {
        if (!CheckQueueDependencies())
            return;

        if (!TryBuildLiveRequest(out var request))
            return;

        EnqueueRequest(request);
        lastResult = $"Enqueued live gameplay payload. Queue pending: {OfflineQueue.Instance.HasPending()}";
    }

    [ContextMenu("Send Single Item Now")]
    public void SendSingleItemNow()
    {
        if (!CheckSendDependencies())
            return;

        StartCoroutine(SendRequestsWithTimeout(BuildRequests(1)));
    }

    [ContextMenu("Send Multiple Items Now")]
    public void SendMultipleItemsNow()
    {
        if (!CheckSendDependencies())
            return;

        StartCoroutine(SendRequestsWithTimeout(BuildRequests(multipleItemCount)));
    }

    [ContextMenu("Send Live Gameplay Payload Now")]
    public void SendLiveGameplayPayloadNow()
    {
        if (!CheckSendDependencies())
            return;

        if (!TryBuildLiveRequest(out var request))
            return;

        StartCoroutine(SendRequestsWithTimeout(new List<SessionUploadRequest> { request }));
    }

    [ContextMenu("Check Queue Status")]
    public void CheckQueueStatus()
    {
        if (OfflineQueue.Instance == null)
        {
            lastResult = "FAIL - OfflineQueue not found in scene.";
            return;
        }

        lastResult = OfflineQueue.Instance.HasPending()
            ? "Queue still has pending items."
            : "Queue is empty — all items sent.";
    }

    [ContextMenu("Clear Queue")]
    public void ClearQueue()
    {
        if (OfflineQueue.Instance == null)
        {
            lastResult = "FAIL - OfflineQueue not found in scene.";
            return;
        }

        OfflineQueue.Instance.ClearQueue();
        lastResult = "Queue cleared.";
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    bool CheckQueueDependencies()
    {
        if (OfflineQueue.Instance == null)
        {
            lastResult = "FAIL — OfflineQueue not found in scene.";
       
            return false;
        }
        if (DataUploader.Instance == null)
        {
            lastResult = "FAIL — DataUploader not found in scene.";
       
            return false;
        }
        return true;
    }

    bool CheckSendDependencies()
    {
        if (DataUploader.Instance == null)
        {
            lastResult = "FAIL — DataUploader not found in scene.";
      
            return false;
        }

        if (OfflineQueue.Instance == null)
        {
            lastResult = "FAIL — OfflineQueue not found in scene.";
           
            return false;
        }

        return true;
    }

    void EnqueueRequest(SessionUploadRequest request)
    {
        OfflineQueue.Instance.Enqueue(JsonUtility.ToJson(request));
    }

    List<SessionUploadRequest> BuildRequests(int count)
    {
        var requests = new List<SessionUploadRequest>(count);

        for (int i = 0; i < count; i++)
        {
            string tag = count == 1 ? "single" : $"batch-{i + 1}";
            requests.Add(BuildTestRequest(tag, i));
        }

        return requests;
    }

    bool TryBuildLiveRequest(out SessionUploadRequest request)
    {
        request = null;

        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.Data == null)
        {
            lastResult = "FAIL — PlayerDataManager not found in scene.";
     
            return false;
        }

        var playerData = PlayerDataManager.Instance.Data;
        playerData.lastDayCompletionTime = DateTime.UtcNow.ToString("o");
        PlayerDataManager.Instance.Save();

        List<TrialRecord> trials = sessionData != null
            ? new List<TrialRecord>(sessionData.trials)
            : new List<TrialRecord>();

        request = new SessionUploadRequest(playerData.profile, playerData, trials);
        return true;
    }

    void EnqueueFallback(SessionUploadRequest request)
    {
        OfflineQueue.Instance.Enqueue(JsonUtility.ToJson(request));
    }

    IEnumerator SendRequestsWithTimeout(List<SessionUploadRequest> requests)
    {
        int successCount = 0;
        int queuedCount = 0;

        foreach (var request in requests)
        {
            var task = DataUploader.Instance.SendAsync(request);
            float timer = 0f;

            while (!task.IsCompleted && timer < sendTimeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            bool timedOut = !task.IsCompleted;
            bool success = !timedOut && task.Result;

            if (success)
            {
                successCount++;
                continue;
            }

            EnqueueFallback(request);
            queuedCount++;
        }

        if (queuedCount == 0)
        {
            lastResult = $"SUCCESS — Sent {successCount}/{requests.Count} items. HTTP {DataUploader.Instance.LastResponseCode}.";
            yield break;
        }

        lastResult = $"PARTIAL — Sent {successCount}/{requests.Count} items, queued {queuedCount} for retry.";
    }

    static SessionUploadRequest BuildTestRequest(string tag, int dayOffset = 0)
    {
        var profile = new PlayerProfile
        {
            phoneNumber = "09129999999",
            playerName  = $"QueueTest_{tag}",
            age         = 30,
            avatarIndex = 2,
            gender      = "female"
        };

        var saveData = new PlayerSaveData
        {
            profile                       = profile,
            currentDay                    = 2 + dayOffset,
            miniGamesCompletedToday       = 3,
            trialsCompletedInCurrentGame  = 4,
            bridgeCompletedToday          = true,
            constellationCompletedToday   = true,
            swmCompletedToday             = false,
            programCompleted              = false,
            profileCompleted              = true,
            lastDayCompletionTime         = System.DateTime.UtcNow.ToString("o")
        };

        var trials = new List<TrialRecord>
        {
            new TrialRecord
            {
                minigame_id        = "Constellation",
                day                = 2 + dayOffset,
                trial_index        = 0,
                span               = 3,
                target_sequence    = new List<int> { 0, 2, 4 },
                wrong_attempts     = 0,
                completion_time_ms = 2100,
                timestamp_iso      = System.DateTime.UtcNow.ToString("o")
            },
            new TrialRecord
            {
                minigame_id        = "Bridge",
                day                = 2 + dayOffset,
                trial_index        = 1,
                span               = 4,
                target_sequence    = new List<int> { 1, 3, 5, 7 },
                wrong_attempts     = 1,
                completion_time_ms = 4800,
                timestamp_iso      = System.DateTime.UtcNow.AddSeconds(-15).ToString("o")
            }
        };

        return new SessionUploadRequest(profile, saveData, trials);
    }
}
