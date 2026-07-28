using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to a GameObject that also has DataUploader on it (or in the scene).
/// In Play Mode, right-click this component and choose "Run Direct Send Test".
/// </summary>
public class DirectSendTest : MonoBehaviour
{
    [Header("Optional overrides")]
    [Tooltip("Leave empty to use DataUploader.Instance found in scene.")]
    [SerializeField] private DataUploader dataUploader;

    [Header("Test result (read-only)")]
    [SerializeField] private string lastResult = "Not run yet";

    void Start()
    {
        if (dataUploader == null)
            dataUploader = DataUploader.Instance;
    }

    // ── ContextMenu entry points ────────────────────────────────────────────

    [ContextMenu("Run Direct Send Test")]
    public void RunDirectSendTest()
    {
        StartCoroutine(DirectSendCoroutine());
    }

    [ContextMenu("Run Direct Send — Empty Trials")]
    public void RunDirectSendNoTrials()
    {
        StartCoroutine(DirectSendCoroutine(includeTrials: false));
    }

    // ── Coroutines ──────────────────────────────────────────────────────────

    IEnumerator DirectSendCoroutine(bool includeTrials = true)
    {
        if (dataUploader == null)
        {
            lastResult = "FAIL — DataUploader not found in scene.";
           
            yield break;
        }

        SessionUploadRequest request = BuildTestRequest(includeTrials);

        lastResult = "Sending…";
        var task = dataUploader.SendAsync(request);

        while (!task.IsCompleted)
            yield return null;

        lastResult = task.Result
            ? $"SUCCESS — Request accepted (HTTP {dataUploader.LastResponseCode})."
            : "FAIL — Server rejected or unreachable (check Console for details).";
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    static SessionUploadRequest BuildTestRequest(bool includeTrials)
    {
        var profile = new PlayerProfile
        {
            phoneNumber = "09120000001",
            playerName  = "TestPlayer",
            age         = 25,
            avatarIndex = 1,
            gender      = "male"
        };

        var saveData = new PlayerSaveData
        {
            profile                       = profile,
            currentDay                    = 3,
            miniGamesCompletedToday       = 3,
            trialsCompletedInCurrentGame  = 5,
            bridgeCompletedToday          = true,
            constellationCompletedToday   = true,
            swmCompletedToday             = true,
            programCompleted              = false,
            profileCompleted              = true,
            lastDayCompletionTime         = System.DateTime.UtcNow.ToString("o")
        };

        List<TrialRecord> trials = new List<TrialRecord>();
        if (includeTrials)
        {
            trials = new List<TrialRecord>
            {
                new TrialRecord
                {
                    minigame_id       = "Constellation",
                    day               = 3,
                    trial_index       = 0,
                    span              = 3,
                    target_sequence   = new List<int> { 1, 4, 7 },
                    wrong_attempts    = 0,
                    completion_time_ms = 1420,
                    timestamp_iso     = System.DateTime.UtcNow.ToString("o")
                },
                new TrialRecord
                {
                    minigame_id       = "SWM",
                    day               = 3,
                    trial_index       = 1,
                    span              = 4,
                    target_sequence   = new List<int> { 2, 5, 1, 8 },
                    wrong_attempts    = 1,
                    completion_time_ms = 3200,
                    timestamp_iso     = System.DateTime.UtcNow.AddSeconds(-30).ToString("o")
                },
                new TrialRecord
                {
                    minigame_id       = "Bridge",
                    day               = 3,
                    trial_index       = 2,
                    span              = 3,
                    target_sequence   = new List<int> { 0, 3, 6 },
                    wrong_attempts    = 2,
                    completion_time_ms = 5100,
                    timestamp_iso     = System.DateTime.UtcNow.AddSeconds(-60).ToString("o")
                }
            };
        }

        return new SessionUploadRequest(profile, saveData, trials);
    }
}
