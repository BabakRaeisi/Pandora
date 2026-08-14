using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OfflineQueue : MonoBehaviour
{
    public static OfflineQueue Instance { get; private set; }

    private string PathToQueue =>
        Path.Combine(Application.persistentDataPath, "upload_queue.json");

    private List<string> queue = new();

    private bool isSending;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadQueue();

        StartCoroutine(ProcessQueueCoroutine());
    }

    // ─────────────────────────────────────────────────────────────
    // OLD SESSION UPLOAD
    // ─────────────────────────────────────────────────────────────

    public void Enqueue(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        queue.Add(json);

        Save();

        Debug.Log("[OfflineQueue] Session upload queued.");
    }

    // ─────────────────────────────────────────────────────────────
    // LEVEL REPORT
    // ─────────────────────────────────────────────────────────────

    public void EnqueueLevelReport(LevelCompletionRecord record)
    {
        if (record == null)
            return;

        string payloadJson = JsonUtility.ToJson(record);

        var queuedUpload = new QueuedUpload
        {
            type = "level-completed",
            payload = payloadJson
        };

        string queuedJson = JsonUtility.ToJson(queuedUpload);

        queue.Add(queuedJson);

        // IMPORTANT:
        // Save to disk immediately before any networking happens.
        Save();

        Debug.Log(
            $"[OfflineQueue] Level report queued | " +
            $"{record.playerId} | " +
            $"{record.minigame} L{record.levelNumber} | " +
            $"{record.successfulTrials}/{record.requiredTrials}"
        );
    }

    // ─────────────────────────────────────────────────────────────
    // STATE
    // ─────────────────────────────────────────────────────────────

    public bool HasPending()
    {
        return queue.Count > 0;
    }

    [ContextMenu("Clear Queue")]
    public void ClearQueue()
    {
        queue.Clear();

        if (File.Exists(PathToQueue))
            File.Delete(PathToQueue);

        Debug.Log("[OfflineQueue] Queue cleared.");
    }

    // ─────────────────────────────────────────────────────────────
    // DISK
    // ─────────────────────────────────────────────────────────────

    private void Save()
    {
        var wrapper = new Wrapper
        {
            items = queue
        };

        string json = JsonUtility.ToJson(wrapper);

        File.WriteAllText(PathToQueue, json);
    }

    private void LoadQueue()
    {
        if (!File.Exists(PathToQueue))
            return;

        try
        {
            string json = File.ReadAllText(PathToQueue);

            Wrapper wrapper =
                JsonUtility.FromJson<Wrapper>(json);

            queue = wrapper?.items ?? new List<string>();

            Debug.Log(
                $"[OfflineQueue] Loaded {queue.Count} queued uploads."
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[OfflineQueue] Failed to load queue: {ex.Message}"
            );

            queue = new List<string>();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // PROCESSING
    // ─────────────────────────────────────────────────────────────

    private IEnumerator ProcessQueueCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (isSending)
                continue;

            if (queue.Count == 0)
                continue;

            if (DataUploader.Instance == null)
                continue;

            if (DataUploader.Instance.OfflineMode)
                continue;

            if (Application.internetReachability ==
                NetworkReachability.NotReachable)
            {
                continue;
            }

            string rawQueuedJson = queue[0];

            if (string.IsNullOrWhiteSpace(rawQueuedJson))
            {
                RemoveFirstItem();
                continue;
            }

            // Check whether this is one of the new typed queue items.
            QueuedUpload typedUpload = null;

            try
            {
                typedUpload =
                    JsonUtility.FromJson<QueuedUpload>(rawQueuedJson);
            }
            catch
            {
                // Old session records are not wrapped.
            }

            if (typedUpload.type == "level-completed")
{
    LevelCompletionRecord record;

    try
    {
        record = JsonUtility.FromJson<LevelCompletionRecord>(
            typedUpload.payload
        );
    }
    catch
    {
        RemoveFirstItem();
        continue;
    }

    if (record == null)
    {
        RemoveFirstItem();
        continue;
    }

    isSending = true;

    var leveltask =
        DataUploader.Instance.SendLevelCompletionAsync(record);

    while (!leveltask.IsCompleted)
        yield return null;

    if (leveltask.Result)
    {
        Debug.Log(
            $"[OfflineQueue] Level report uploaded | " +
            $"{record.minigame} L{record.levelNumber}"
        );

        RemoveFirstItem();
    }
    else
    {
        Debug.LogWarning(
            $"[OfflineQueue] Level report upload failed. " +
            $"Keeping it queued."
        );
    }

    isSending = false;

    continue;
}

            // Existing / legacy SessionUploadRequest behavior.
            SessionUploadRequest request;

            try
            {
                request =
                    JsonUtility.FromJson<SessionUploadRequest>(
                        rawQueuedJson
                    );
            }
            catch
            {
                RemoveFirstItem();
                continue;
            }

            if (request == null)
            {
                RemoveFirstItem();
                continue;
            }

            isSending = true;

            var task =
                DataUploader.Instance.SendAsync(request);

            while (!task.IsCompleted)
                yield return null;

            if (task.Result)
            {
                Debug.Log(
                    "[OfflineQueue] Session upload sent successfully."
                );

                RemoveFirstItem();
            }
            else
            {
                Debug.LogWarning(
                    "[OfflineQueue] Session upload failed. " +
                    "Keeping it in queue."
                );
            }

            isSending = false;
        }
    }

    private void RemoveFirstItem()
    {
        if (queue.Count == 0)
            return;

        queue.RemoveAt(0);
        Save();
    }

    // ─────────────────────────────────────────────────────────────
    // SERIALIZATION
    // ─────────────────────────────────────────────────────────────

    [System.Serializable]
    private class QueuedUpload
    {
        public string type;
        public string payload;
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<string> items = new();
    }
}