using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class DataUploader : MonoBehaviour
{
    public static DataUploader Instance { get; private set; }

    [SerializeField] private string apiUrl = "https://10.0.0.183:5001/api/analytics/upload";
    [SerializeField] private int requestTimeoutSeconds = 10;
    [SerializeField] private bool allowInsecureHttpInEditor = false;
    [SerializeField] private bool allowInvalidHttpsCertificateInEditor = true;

    [Header("Offline Mode")]
    [Tooltip("When enabled, all uploads are skipped and stored locally. OfflineQueue will not retry until this is turned off.")]
    [SerializeField] private bool offlineMode = true;

    public int RequestTimeoutSeconds => requestTimeoutSeconds;
    public bool OfflineMode => offlineMode;
    public long LastResponseCode { get; private set; }
    public string LastResponseBody { get; private set; }
    public string LastRequestJson { get; private set; }

    private void OnValidate()
    {
        UpgradeUrlToHttpsIfNeeded();
    }

    void Awake()
    {
        UpgradeUrlToHttpsIfNeeded();

        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Task<bool> SendAsync(SessionUploadRequest request)
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(SendCoroutine(request, tcs));
        return tcs.Task;
    }

    private IEnumerator SendCoroutine(SessionUploadRequest request, TaskCompletionSource<bool> tcs)
    {
        if (request == null)
        {
            
            tcs.SetResult(false);
            yield break;
        }

        NormalizeRequestForUpload(request);

        if (!TryGetValidatedUploadUri(out var validatedUri, out var validationError))
        {
       
            tcs.SetResult(false);
            yield break;
        }

        string json = JsonUtility.ToJson(request);
        LastRequestJson = json;

        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
           
            tcs.SetResult(false);
            yield break;
        }

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using var www = new UnityWebRequest(validatedUri, "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        www.timeout = requestTimeoutSeconds;

#if UNITY_EDITOR
        if (allowInvalidHttpsCertificateInEditor &&
            Uri.TryCreate(validatedUri, UriKind.Absolute, out var requestUri) &&
            requestUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            www.certificateHandler = new AcceptAllCertificatesForEditorOnly();
        }
#endif

        UnityWebRequestAsyncOperation operation;
        try
        {
            operation = www.SendWebRequest();
        }
        catch (InvalidOperationException ex)
        {
          
            tcs.SetResult(false);
            yield break;
        }

        yield return operation;

        LastResponseCode = www.responseCode;
        LastResponseBody = www.downloadHandler?.text ?? string.Empty;

        bool transportSuccess = www.result == UnityWebRequest.Result.Success;
        bool httpSuccess = www.responseCode >= 200 && www.responseCode < 300;
        bool htmlResponse = LooksLikeHtml(LastResponseBody);
        bool success = transportSuccess && httpSuccess && !htmlResponse;

        if (!success)
          {}
        else
           {
        tcs.SetResult(success);
    }}

    private static void NormalizeRequestForUpload(SessionUploadRequest request)
    {
        string nowIso = DateTime.UtcNow.ToString("o");

        if (request.trials == null)
            request.trials = new List<TrialRecord>();

        if (request.saveData != null)
        {
            if (request.saveData.profile == null)
                request.saveData.profile = request.profile;

            if (string.IsNullOrWhiteSpace(request.saveData.lastDayCompletionTime))
                request.saveData.lastDayCompletionTime = nowIso;
        }

        if (request.profile == null && request.saveData != null)
            request.profile = request.saveData.profile;

        for (int i = request.trials.Count - 1; i >= 0; i--)
        {
            TrialRecord trial = request.trials[i];
            if (trial == null)
            {
                request.trials.RemoveAt(i);
                continue;
            }

            if (trial.target_sequence == null)
                trial.target_sequence = new List<int>();

            if (string.IsNullOrWhiteSpace(trial.timestamp_iso))
                trial.timestamp_iso = nowIso;

            if (trial.day <= 0 && request.saveData != null)
                trial.day = request.saveData.currentDay;
        }
    }

    private static bool LooksLikeHtml(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string trimmed = text.TrimStart();
        return trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetValidatedUploadUri(out string validatedUri, out string error)
    {
        validatedUri = apiUrl?.Trim();
        error = null;

        if (string.IsNullOrWhiteSpace(validatedUri))
        {
            error = "Upload URL is empty.";
            return false;
        }

        if (!Uri.TryCreate(validatedUri, UriKind.Absolute, out var uri))
        {
            error = $"Upload URL is invalid: '{validatedUri}'.";
            return false;
        }

        bool isHttps = uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        if (isHttps)
            return true;

        bool isHttp = uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        if (!isHttp)
        {
            error = $"Unsupported URL scheme '{uri.Scheme}'. Use HTTPS.";
            return false;
        }

#if UNITY_EDITOR
        if (allowInsecureHttpInEditor)
            return true;
#endif

        error = "Insecure HTTP URL is blocked. Use an HTTPS endpoint for uploads.";
        return false;
    }

    private void UpgradeUrlToHttpsIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
            return;

        const string insecurePrefix = "http://";
        if (apiUrl.StartsWith(insecurePrefix, StringComparison.OrdinalIgnoreCase))
            apiUrl = "https://" + apiUrl.Substring(insecurePrefix.Length);
    }

#if UNITY_EDITOR
    private sealed class AcceptAllCertificatesForEditorOnly : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
#endif
}