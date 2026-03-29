using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class FinalVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (!videoPlayer)
            videoPlayer = GetComponent<VideoPlayer>();
    }

    private void OnEnable()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDisable()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void Start()
    {
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        LoadingScreenController.Instance.LoadScene(mainMenuSceneName);
    }
}