using TMPro;
using UnityEngine;

namespace RTLTMPro
{
    public class RTLTMP_InputField : TMP_InputField
    {

        [SerializeField]
        RTLTextMeshPro displayText;

        protected override void Awake()
        {
            displayText = GetComponentInChildren<RTLTextMeshPro>();
            onValueChanged.AddListener((string newText) =>
            {
                UpdateRTLText(newText); ;

            });
        }


        void UpdateRTLText(string newText)
        {
            if (displayText == null)
            {
            
                return;
            }

            if (displayText == m_Placeholder)
            {
              
                return;
            }

            displayText.ChangeText(newText);
        }
    }
}
