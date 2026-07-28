using UnityEngine;
using UnityEngine.UIElements;

namespace RVFX.UIToolkit.EdgeEffects
{
    public class AttachUXMLByName : MonoBehaviour
    {
        public UIDocument uiDocument;       // Reference to UIDocument (auto-get if null)
        public string targetName = "ContentRoot";   // Name of the container where UXML content will be attached
        public VisualTreeAsset uxml;        // UXML file to instantiate

        void OnEnable()
        {
            if (uxml == null)
            {
              
                return;
            }

            var doc = uiDocument != null ? uiDocument : GetComponent<UIDocument>();
            if (doc == null || doc.rootVisualElement == null)
            {
            
                return;
            }

            var root = doc.rootVisualElement;
            var target = root.Q<VisualElement>(targetName);
            if (target == null)
            {
             
                return;
            }

            // Instantiate the UXML file
            var widgetRoot = uxml.Instantiate();

            // Safely move children (reparent) without modifying the collection during enumeration
            // Approach: always take the first child until none remain
            while (widgetRoot.childCount > 0)
            {
                var child = widgetRoot[0];   // get first child
                target.Add(child);           // Add() will reparent the element
            }
        }
    }
}
