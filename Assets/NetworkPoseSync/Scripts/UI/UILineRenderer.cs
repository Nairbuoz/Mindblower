using UnityEngine;
using UnityEngine.UI;

namespace NetworkPoseSync.UI
{
  [RequireComponent(typeof(RectTransform))]
  public class UILineRenderer : Graphic
  {
    public Vector2 start;
    public Vector2 end;
    [Min(0.5f)]
    public float thickness = 2f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
      vh.Clear();

      Vector2 dir = (end - start);
      float len = dir.magnitude;
      if (len <= 0.0001f)
        return;

      Vector2 n = new Vector2(-dir.y, dir.x).normalized;
      Vector2 halfW = n * (thickness * 0.5f);

      Vector2 v0 = start - halfW;
      Vector2 v1 = start + halfW;
      Vector2 v2 = end + halfW;
      Vector2 v3 = end - halfW;

      var color32 = color;

      int idx = 0;
      vh.AddVert(v0, color32, Vector2.zero);
      vh.AddVert(v1, color32, Vector2.zero);
      vh.AddVert(v2, color32, Vector2.zero);
      vh.AddVert(v3, color32, Vector2.zero);

      vh.AddTriangle(idx + 0, idx + 1, idx + 2);
      vh.AddTriangle(idx + 0, idx + 2, idx + 3);
    }

    public void SetEndpoints(Vector2 s, Vector2 e)
    {
      start = s;
      end = e;
      SetVerticesDirty();
    }

    public void SetThickness(float t)
    {
      thickness = Mathf.Max(0.5f, t);
      SetVerticesDirty();
    }
  }
}
