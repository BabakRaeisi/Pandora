using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance;

    public RectTransform topPanel;
    public RectTransform bottomPanel;
    public RectTransform leftPanel;
    public RectTransform rightPanel;

    public float duration = 1.2f;
    public float moveDistance = 1500f;

    bool transitioning;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Open();
    }

    void Open()
    {
        Sequence seq = DOTween.Sequence();

        seq.Join(topPanel.DOAnchorPosY(moveDistance, duration));
        seq.Join(bottomPanel.DOAnchorPosY(-moveDistance, duration));
        seq.Join(leftPanel.DOAnchorPosX(-moveDistance, duration));
        seq.Join(rightPanel.DOAnchorPosX(moveDistance, duration));

        seq.SetEase(Ease.InOutCubic);
    }

    void Close()
    {
        Sequence seq = DOTween.Sequence();

        seq.Join(topPanel.DOAnchorPosY(0, duration));
        seq.Join(bottomPanel.DOAnchorPosY(0, duration));
        seq.Join(leftPanel.DOAnchorPosX(0, duration));
        seq.Join(rightPanel.DOAnchorPosX(0, duration));

        seq.SetEase(Ease.InOutCubic);
    }

    public void LoadScene(string sceneName)
    {
        if (transitioning) return;

        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        transitioning = true;

        Close();

        yield return new WaitForSeconds(duration);

        SceneManager.LoadScene(sceneName);

        transitioning = false;
    }
}
