using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    [Header("Normal Gradient")]
    public Color colorTop = Color.white;
    public Color colorBottom = Color.black;

    [Header("Disabled Gradient")]
    public bool useDisabledGradient = true;
    public Color disabledTop = Color.gray;
    public Color disabledBottom = Color.darkGray;

    [Header("Upper Edge Glow")]
    [Range(0f, 1f)] public float glowIntensity = 0.3f;
    [Range(0.5f, 1f)] public float glowThickness = 0.85f; // Higher means closer to the top edge

    private Selectable targetSelectable;

    protected override void Start()
    {
        base.Start();
        targetSelectable = GetComponent<Selectable>();
    }

    private void Update()
    {
        if (targetSelectable != null && graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        int count = vh.currentVertCount;
        if (count == 0) return;

        bool isDisabled = targetSelectable != null && !targetSelectable.interactable;
        Color top = (useDisabledGradient && isDisabled) ? disabledTop : colorTop;
        Color bottom = (useDisabledGradient && isDisabled) ? disabledBottom : colorBottom;

        UIVertex vertex = new UIVertex();
        
        vh.PopulateUIVertex(ref vertex, 0);
        float yMin = vertex.position.y;
        float yMax = vertex.position.y;

        for (int i = 1; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float y = vertex.position.y;
            if (y > yMax) yMax = y;
            else if (y < yMin) yMin = y;
        }

        float uiHeight = yMax - yMin;
        if (uiHeight == 0) return;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            float t = (vertex.position.y - yMin) / uiHeight;
            
            // Base gradient color
            Color finalColor = Color.Lerp(bottom, top, t);

            // Calculate upper edge glow factor
            if (t > glowThickness && glowIntensity > 0)
            {
                // Normalizes the top area from 0 to 1
                float glowFactor = (t - glowThickness) / (1f - glowThickness); 
                // Linearly add white glow based on intensity
                finalColor = Color.Lerp(finalColor, Color.white, glowFactor * glowIntensity);
            }

            vertex.color = finalColor;
            vh.SetUIVertex(vertex, i);
        }
    }
}