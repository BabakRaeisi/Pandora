using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class FinalVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Video Audio")]
    [Tooltip("Assign one AudioSource used exclusively by this VideoPlayer.")]
    [SerializeField] private AudioSource videoAudioSource;

    [Header("Scene Routing")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string introSceneName = "IntroScene";

    [Tooltip("Used only when this controller runs in IntroScene and no player profile exists.")]
    [SerializeField] private string noProfileSceneName = "ProfileCreation";

    private bool isLoadingNextScene;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoAudioSource == null && AudioManager.Instance != null)
            videoAudioSource = AudioManager.Instance.musicSource;

        ConfigureVideoAudio();
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.Stop();
        }

        if (videoAudioSource != null)
            videoAudioSource.Stop();
    }

    private void Start()
    {
        if (videoPlayer != null && !videoPlayer.isPlaying)
            videoPlayer.Play();
    }

    private void ConfigureVideoAudio()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        if (videoAudioSource == null)
        {
         
            return;
        }

        // Route the video's audio through one controlled AudioSource instead
        // of using VideoPlayer's direct output.
        videoAudioSource.playOnAwake = false;
        videoAudioSource.loop = false;
        videoAudioSource.spatialBlend = 0f;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (isLoadingNextScene)
            return;

        isLoadingNextScene = true;

        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoAudioSource != null)
            videoAudioSource.Stop();

        string targetScene = GetTargetScene();

        if (LoadingScreenController.Instance != null)
            LoadingScreenController.Instance.LoadScene(targetScene);
        else
            SceneManager.LoadScene(targetScene);
    }

    private string GetTargetScene()
    {
        bool isInIntroScene =
            SceneManager.GetActiveScene().name == introSceneName;

        bool hasProfile =
            PlayerDataManager.Instance != null &&
            PlayerDataManager.Instance.Data != null &&
            PlayerDataManager.Instance.Data.profileCompleted;

        return isInIntroScene && !hasProfile
            ? noProfileSceneName
            : mainMenuSceneName;
    }
}