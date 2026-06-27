using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OfflineQueue : MonoBehaviour
{
    public static OfflineQueue Instance { get; private set; }

    string path => Path.Combine(Application.persistentDataPath, "upload_queue.json");

    private List<string> queue = new();
    private bool isSending;

    [ContextMenu("Clear Queue")]
    public void ClearQueue()
    {
        queue.Clear();

        if (File.Exists(path))
            File.Delete(path);

        Debug.Log("Offline queue cleared");
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            queue = JsonUtility.FromJson<Wrapper>(json)?.items ?? new List<string>();
        }

        StartCoroutine(ProcessQueueCoroutine());
    }

    public void Enqueue(string json)
    {
        queue.Add(json);
        Save();
        Debug.Log($"[OfflineQueue] Enqueued. Queue size: {queue.Count}");
    }

    public bool HasPending() => queue.Count > 0;

    void Save()
    {
        File.WriteAllText(path, JsonUtility.ToJson(new Wrapper { items = queue }));
    }

    IEnumerator ProcessQueueCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (isSending || queue.Count == 0 || DataUploader.Instance == null ||
                DataUploader.Instance.OfflineMode ||
                Application.internetReachability == NetworkReachability.NotReachable)
                continue;

            isSending = true;

            string rawQueuedJson = queue[0];
            if (string.IsNullOrWhiteSpace(rawQueuedJson))
            {
                Debug.LogError("[OfflineQueue] Found empty queue item. Removing it.");
                queue.RemoveAt(0);
                Save();
                isSending = false;
                continue;
            }

            var request = JsonUtility.FromJson<SessionUploadRequest>(rawQueuedJson);
            if (request == null)
            {
                Debug.LogError("[OfflineQueue] Failed to deserialize queued item. Removing invalid payload.");
                queue.RemoveAt(0);
                Save();
                isSending = false;
                continue;
            }

            var task = DataUploader.Instance.SendAsync(request);

            while (!task.IsCompleted)
                yield return null;

            if (task.Result)
            {
                queue.RemoveAt(0);
                Save();
                Debug.Log("[OfflineQueue] Item sent successfully and removed from queue.");
            }
            else
            {
                Debug.LogWarning("[OfflineQueue] Retry failed, will try again in 5s.");
            }

            isSending = false;
        }
    }

    [System.Serializable]
    class Wrapper
    {
        public List<string> items;
    }
}