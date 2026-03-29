using UnityEngine;

public class IntroMusic : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance.StopAll();
        AudioManager.Instance.Play("Intro");
    }

   
}
