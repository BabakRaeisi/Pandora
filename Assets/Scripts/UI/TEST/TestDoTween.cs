 
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
 

public class TestDoTween : MonoBehaviour
{

    public Slider slider;
    void Start() 
    {
        
        slider.value = 0.11f;
        slider.DOValue(0.94f, 3f);
    }    
}
