using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class DataUploader : MonoBehaviour
{
    public static DataUploader Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────
    // API URLS
    // ─────────────────────────────────────────────────────────────

    [Header("API URLs")]
     
    private string apiUrl =
        "https://pandora-api.babakraeisi.com/api/analytics/upload";

   
    private string levelCompletionUrl =
        "https://pandora-api.babakraeisi.com/api/analytics/level-completed";

    [Header("Request Settings")]
    [SerializeField]
    private int requestTimeoutSeconds = 10;

    [SerializeField]
    private bool allowInsecureHttpInEditor = false;

    [SerializeField]
    private bool allowInvalidHttpsCertificateInEditor = true;

    [Header("Offline Mode")]
    [Tooltip(
        "When enabled, uploads remain queued and are not sent."
    )]
    [SerializeField]
    private bool offlineMode = false;

    // ─────────────────────────────────────────────────────────────
    // PUBLIC STATE
    // ─────────────────────────────────────────────────────────────

    public int RequestTimeoutSeconds =>
        requestTimeoutSeconds;

    public bool OfflineMode =>
        offlineMode;

    public long LastResponseCode { get; private set; }

    public string LastResponseBody { get; private set; }

    public string LastRequestJson { get; private set; }

    // ─────────────────────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────────────────────

    private void OnValidate()
    {
        apiUrl = UpgradeUrlToHttpsIfNeeded(apiUrl);

        levelCompletionUrl =
            UpgradeUrlToHttpsIfNeeded(levelCompletionUrl);
    }

    private void Awake()
    {
        apiUrl = UpgradeUrlToHttpsIfNeeded(apiUrl);

        levelCompletionUrl =
            UpgradeUrlToHttpsIfNeeded(levelCompletionUrl);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────
    // SESSION UPLOAD
    // ─────────────────────────────────────────────────────────────

    public Task<bool> SendAsync(
        SessionUploadRequest request)
    {
        var tcs =
            new TaskCompletionSource<bool>();

        StartCoroutine(
            SendSessionCoroutine(request, tcs)
        );

        return tcs.Task;
    }

    private IEnumerator SendSessionCoroutine(
        SessionUploadRequest request,
        TaskCompletionSource<bool> tcs)
    {
        if (request == null)
        {
            tcs.SetResult(false);
            yield break;
        }

        NormalizeRequestForUpload(request);

        if (!TryGetValidatedUri(
                apiUrl,
                out string validatedUri,
                out string validationError))
        {
            Debug.LogError(
                $"[DataUploader] Invalid session URL: " +
                $"{validationError}"
            );

            tcs.SetResult(false);
            yield break;
        }

        string json =
            JsonUtility.ToJson(request);

        LastRequestJson = json;

        if (string.IsNullOrWhiteSpace(json) ||
            json == "{}")
        {
            tcs.SetResult(false);
            yield break;
        }

        Debug.Log(
            $"[DataUploader] Sending session → " +
            $"{validatedUri}"
        );

        yield return SendJsonCoroutine(
            validatedUri,
            json,
            (success, responseCode, responseBody) =>
            {
                LastResponseCode = responseCode;
                LastResponseBody = responseBody;

                if (success)
                {
                    Debug.Log(
                        "[DataUploader] Session upload successful."
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"[DataUploader] Session upload failed | " +
                        $"Status={responseCode} | " +
                        $"Response={responseBody}"
                    );
                }

                tcs.SetResult(success);
            }
        );
    }

    // ─────────────────────────────────────────────────────────────
    // LEVEL COMPLETION UPLOAD
    // ─────────────────────────────────────────────────────────────

    public Task<bool> SendLevelCompletionAsync(
        LevelCompletionRecord record)
    {
        var tcs =
            new TaskCompletionSource<bool>();

        StartCoroutine(
            SendLevelCompletionCoroutine(
                record,
                tcs
            )
        );

        return tcs.Task;
    }

    private IEnumerator SendLevelCompletionCoroutine(
        LevelCompletionRecord record,
        TaskCompletionSource<bool> tcs)
    {
        if (record == null)
        {
            tcs.SetResult(false);
            yield break;
        }

        if (!TryGetValidatedUri(
                levelCompletionUrl,
                out string validatedUri,
                out string validationError))
        {
            Debug.LogError(
                $"[DataUploader] Invalid level URL: " +
                $"{validationError}"
            );

            tcs.SetResult(false);
            yield break;
        }

        string json =
            JsonUtility.ToJson(record);

        if (string.IsNullOrWhiteSpace(json) ||
            json == "{}")
        {
            tcs.SetResult(false);
            yield break;
        }

        Debug.Log(
            $"[DataUploader] Sending level report → " +
            $"{validatedUri}"
        );

        Debug.Log(
            $"[DataUploader] " +
            $"{record.playerId} | " +
            $"{record.minigame} L{record.levelNumber} | " +
            $"{record.successfulTrials}/" +
            $"{record.requiredTrials}"
        );

        yield return SendJsonCoroutine(
            validatedUri,
            json,
            (success, responseCode, responseBody) =>
            {
                if (success)
                {
                    Debug.Log(
                        $"[DataUploader] Level report sent | " +
                        $"{record.minigame} " +
                        $"L{record.levelNumber}"
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"[DataUploader] Level report failed | " +
                        $"Status={responseCode} | " +
                        $"Response={responseBody}"
                    );
                }

                tcs.SetResult(success);
            }
        );
    }

    // ─────────────────────────────────────────────────────────────
    // GENERIC JSON POST
    // ─────────────────────────────────────────────────────────────

    private IEnumerator SendJsonCoroutine(
        string uri,
        string json,
        Action<bool, long, string> onComplete)
    {
        byte[] bodyRaw =
            Encoding.UTF8.GetBytes(json);

        using var www =
            new UnityWebRequest(uri, "POST");

        www.uploadHandler =
            new UploadHandlerRaw(bodyRaw);

        www.downloadHandler =
            new DownloadHandlerBuffer();

        www.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        www.timeout =
            requestTimeoutSeconds;

#if UNITY_EDITOR
        if (allowInvalidHttpsCertificateInEditor &&
            Uri.TryCreate(
                uri,
                UriKind.Absolute,
                out Uri requestUri) &&
            requestUri.Scheme.Equals(
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            www.certificateHandler =
                new AcceptAllCertificatesForEditorOnly();
        }
#endif

        UnityWebRequestAsyncOperation operation;

        try
        {
            operation =
                www.SendWebRequest();
        }
        catch (InvalidOperationException ex)
        {
            Debug.LogError(
                $"[DataUploader] Request failed to start: " +
                $"{ex.Message}"
            );

            onComplete?.Invoke(
                false,
                0,
                ex.Message
            );

            yield break;
        }

        yield return operation;

        long responseCode =
            www.responseCode;

        string responseBody =
            www.downloadHandler?.text ??
            string.Empty;

        bool transportSuccess =
            www.result ==
            UnityWebRequest.Result.Success;

        bool httpSuccess =
            responseCode >= 200 &&
            responseCode < 300;

        bool htmlResponse =
            LooksLikeHtml(responseBody);

        bool success =
            transportSuccess &&
            httpSuccess &&
            !htmlResponse;

        if (!success)
        {
            Debug.LogWarning(
                $"[DataUploader] HTTP request failed | " +
                $"Result={www.result} | " +
                $"Status={responseCode} | " +
                $"Error={www.error}"
            );
        }

        onComplete?.Invoke(
            success,
            responseCode,
            responseBody
        );
    }

    // ─────────────────────────────────────────────────────────────
    // SESSION NORMALIZATION
    // ─────────────────────────────────────────────────────────────

    private static void NormalizeRequestForUpload(
        SessionUploadRequest request)
    {
        string nowIso =
            DateTime.UtcNow.ToString("o");

        if (request.trials == null)
        {
            request.trials =
                new List<TrialRecord>();
        }

        if (request.saveData != null)
        {
            if (request.saveData.profile == null)
            {
                request.saveData.profile =
                    request.profile;
            }

            if (string.IsNullOrWhiteSpace(
                    request.saveData
                        .lastDayCompletionTime))
            {
                request.saveData
                    .lastDayCompletionTime =
                    nowIso;
            }
        }

        if (request.profile == null &&
            request.saveData != null)
        {
            request.profile =
                request.saveData.profile;
        }

        for (int i =
                 request.trials.Count - 1;
             i >= 0;
             i--)
        {
            TrialRecord trial =
                request.trials[i];

            if (trial == null)
            {
                request.trials.RemoveAt(i);
                continue;
            }

            if (trial.target_sequence == null)
            {
                trial.target_sequence =
                    new List<int>();
            }

            if (string.IsNullOrWhiteSpace(
                    trial.timestamp_iso))
            {
                trial.timestamp_iso =
                    nowIso;
            }

            if (trial.day <= 0 &&
                request.saveData != null)
            {
                trial.day =
                    request.saveData.currentDay;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // URL VALIDATION
    // ─────────────────────────────────────────────────────────────

    private bool TryGetValidatedUri(
        string rawUrl,
        out string validatedUri,
        out string error)
    {
        validatedUri =
            rawUrl?.Trim();

        error = null;

        if (string.IsNullOrWhiteSpace(
                validatedUri))
        {
            error =
                "URL is empty.";

            return false;
        }

        if (!Uri.TryCreate(
                validatedUri,
                UriKind.Absolute,
                out Uri uri))
        {
            error =
                $"Invalid URL: '{validatedUri}'.";

            return false;
        }

        bool isHttps =
            uri.Scheme.Equals(
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase);

        if (isHttps)
            return true;

        bool isHttp =
            uri.Scheme.Equals(
                Uri.UriSchemeHttp,
                StringComparison.OrdinalIgnoreCase);

        if (!isHttp)
        {
            error =
                $"Unsupported URL scheme " +
                $"'{uri.Scheme}'.";

            return false;
        }

#if UNITY_EDITOR
        if (allowInsecureHttpInEditor)
            return true;
#endif

        error =
            "HTTP is blocked. Use HTTPS.";

        return false;
    }

    private static string UpgradeUrlToHttpsIfNeeded(
        string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        const string httpPrefix =
            "http://";

        if (url.StartsWith(
                httpPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return "https://" +
                   url.Substring(
                       httpPrefix.Length);
        }

        return url;
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────

    private static bool LooksLikeHtml(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string trimmed =
            text.TrimStart();

        return
            trimmed.StartsWith(
                "<!DOCTYPE",
                StringComparison.OrdinalIgnoreCase)
            ||
            trimmed.StartsWith(
                "<html",
                StringComparison.OrdinalIgnoreCase);
    }

#if UNITY_EDITOR
    private sealed class
        AcceptAllCertificatesForEditorOnly :
        CertificateHandler
    {
        protected override bool ValidateCertificate(
            byte[] certificateData)
        {
            return true;
        }
    }
#endif
}