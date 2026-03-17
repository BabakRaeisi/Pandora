using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlow : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName ;

    public void GoToMainMenu()
    {
        LoadingScreenController.Instance.LoadScene(mainMenuSceneName);
        AudioManager.Instance.Play("Button");
    }
}