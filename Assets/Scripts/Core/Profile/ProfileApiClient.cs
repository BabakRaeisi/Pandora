using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles POST /api/analytics/profile — sign up or restore an existing player.
/// Attach to the same persistent GameObject as DataUploader.
/// </summary>
public class ProfileApiClient : MonoBehaviour
{
    public static ProfileApiClient Instance { get; private set; }

    [SerializeField] private string profileUrl = "https://10.0.0.183:5001/api/analytics/profile";
    [SerializeField] private int    requestTimeoutSeconds = 10;
    [SerializeField] private bool allowInsecureHttpInEditor = false;
    [SerializeField] private bool allowInvalidHttpsCertificateInEditor = true;

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

    /// <summary>
    /// Calls POST /api/analytics/profile.
    /// Returns a ProfileRestoreResponse on success, or null if the call fails.
    /// </summary>
    public Task<ProfileRestoreResponse> RegisterOrRestoreAsync(PlayerProfile profile)
    {
        var tcs = new TaskCompletionSource<ProfileRestoreResponse>();
        StartCoroutine(RegisterCoroutine(profile, tcs));
        return tcs.Task;
    }

    private IEnumerator RegisterCoroutine(PlayerProfile profile,
                                          TaskCompletionSource<ProfileRestoreResponse> tcs)
    {
        if (!TryGetValidatedProfileUri(out var validatedUri, out var validationError))
        {
             tcs.SetResult(null);
            yield break;
        }

        var body    = new ProfileRegisterRequest(profile);
        string json = JsonUtility.ToJson(body);
        byte[] raw  = Encoding.UTF8.GetBytes(json);

        using var www = new UnityWebRequest(validatedUri, "POST");
        www.uploadHandler   = new UploadHandlerRaw(raw);
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

        UnityWebRequestAsyncOperation op;
        try
        {
            op = www.SendWebRequest();
        }
        catch (InvalidOperationException ex)
        {
            tcs.SetResult(null);
            yield break;
        }

        yield return op;

        bool ok = www.result == UnityWebRequest.Result.Success
               && www.responseCode >= 200 && www.responseCode < 300;

        if (!ok)
        {
          
            tcs.SetResult(null);
            yield break;
        }

        string responseJson = www.downloadHandler.text;
        var response = JsonUtility.FromJson<ProfileRestoreResponse>(responseJson);

        if (response == null)
        {
           
            tcs.SetResult(null);
            yield break;
        }

        tcs.SetResult(response);
    }

    private bool TryGetValidatedProfileUri(out string validatedUri, out string error)
    {
        validatedUri = profileUrl?.Trim();
        error = null;

        if (string.IsNullOrWhiteSpace(validatedUri))
        {
            error = "Profile URL is empty.";
            return false;
        }

        if (!Uri.TryCreate(validatedUri, UriKind.Absolute, out var uri))
        {
            error = $"Profile URL is invalid: '{validatedUri}'.";
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

        error = "Insecure HTTP URL is blocked. Use an HTTPS endpoint for profile API.";
        return false;
    }

    private void UpgradeUrlToHttpsIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(profileUrl))
            return;

        const string insecurePrefix = "http://";
        if (profileUrl.StartsWith(insecurePrefix, StringComparison.OrdinalIgnoreCase))
            profileUrl = "https://" + profileUrl.Substring(insecurePrefix.Length);
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
