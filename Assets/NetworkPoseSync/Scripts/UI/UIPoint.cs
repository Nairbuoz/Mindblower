using UnityEngine;
using UnityEngine.UI;

namespace NetworkPoseSync.UI
{
  [RequireComponent(typeof(RectTransform))]
  public class UIPoint : Graphic
  {
    [Min(0.5f)]
    public float size = 8f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
      vh.Clear();
      var rt = rectTransform;
      float half = size * 0.5f;
      var bl = new Vector2(-half, -half);
      var tl = new Vector2(-half,  half);
      var tr = new Vector2( half,  half);
      var br = new Vector2( half, -half);

      var color32 = color;

      int idx = 0;
      vh.AddVert(bl, color32, Vector2.zero);
      vh.AddVert(tl, color32, Vector2.zero);
      vh.AddVert(tr, color32, Vector2.zero);
      vh.AddVert(br, color32, Vector2.zero);

      vh.AddTriangle(idx + 0, idx + 1, idx + 2);
      vh.AddTriangle(idx + 0, idx + 2, idx + 3);
    }

    public void SetAnchoredPosition(Vector2 pos)
    {
      rectTransform.anchoredPosition = pos;
      SetVerticesDirty();
    }

    public void SetSize(float newSize)
    {
      size = Mathf.Max(0.5f, newSize);
      SetVerticesDirty();
    }
  }
}
