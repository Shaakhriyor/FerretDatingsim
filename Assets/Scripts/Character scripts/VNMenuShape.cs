using UnityEngine;

public class VNMenuShape : UnityEngine.UI.MaskableGraphic
{
    public enum Shape { Heart, Slant }
    [SerializeField] private Shape shape;
    public void SetShape(Shape value) { shape = value; SetVerticesDirty(); }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Color32 tint = color;
        if (shape == Shape.Slant)
        {
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), tint, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin + rect.width * 0.14f, rect.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax - rect.width * 0.14f, rect.yMin), tint, Vector2.zero);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
            return;
        }
        const int segments = 160;
        vh.AddVert(new Vector3(rect.center.x, rect.center.y), tint, Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float t = i * Mathf.PI * 2f / segments;
            float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
            float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f*t) - 2f * Mathf.Cos(3f*t) - Mathf.Cos(4f*t);
            vh.AddVert(new Vector3(rect.center.x + x / 34f * rect.width,
                rect.center.y + (y + 2.5f) / 32f * rect.height), tint, Vector2.zero);
            if (i > 0) vh.AddTriangle(0, i, i + 1);
        }
    }
}
