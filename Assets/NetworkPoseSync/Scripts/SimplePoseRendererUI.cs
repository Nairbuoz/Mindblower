using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NetworkPoseSync.UI;

namespace NetworkPoseSync
{
  [RequireComponent(typeof(RectTransform))]
  public class SimplePoseRendererUI : MonoBehaviour
  {
    [Header("Sources")]
    public UdpPoseReceiver receiver;

    [Header("Canvas/Space")]
    public RectTransform renderArea; // If null, uses this RectTransform
    public bool yFlipForUI = true;   // Convert normalized y (origin bottom) to UI (origin top)

    [Header("Appearance")]
    [Range(0f, 1f)] public float minVisibilityToDraw = 0.2f;
    public float pointSize = 8f;
    public float lineThickness = 2f;
    public Color landmarkColor = Color.green;
    public Color connectionColor = Color.white;

    // BlazePose 33-landmark skeleton connections (indices)
    private static readonly int[][] Connections = new int[][]
    {
      // Torso/shoulders/hips
      new []{11,12}, new []{11,23}, new []{12,24}, new []{23,24},
      // Left arm
      new []{11,13}, new []{13,15}, new []{15,17}, new []{15,19}, new []{15,21},
      // Right arm
      new []{12,14}, new []{14,16}, new []{16,18}, new []{16,20}, new []{16,22},
      // Left leg
      new []{23,25}, new []{25,27}, new []{27,29}, new []{29,31},
      // Right leg
      new []{24,26}, new []{26,28}, new []{28,30}, new []{30,32},
      // Head/face outline (light)
      new []{0,1}, new []{1,2}, new []{2,3}, new []{3,7},
      new []{0,4}, new []{4,5}, new []{5,6}, new []{6,8},
      new []{9,10}
    };

    private RectTransform _root;
    private readonly List<PoseUI> _poseUIs = new List<PoseUI>(4);

    private class PoseUI
    {
      public UIPoint[] points;
      public UILineRenderer[] lines;
      public GameObject rootGO;
    }

    private void Awake()
    {
      _root = renderArea != null ? renderArea : GetComponent<RectTransform>();
      if (receiver == null)
      {
        receiver = FindObjectOfType<UdpPoseReceiver>();
      }
    }

    private void Update()
    {
      if (receiver == null) return;
      var frame = receiver.LatestFrame;
      if (frame == null || frame.poses == null) return;

      EnsurePoseUICount(frame.poses.Length);
      var size = _root.rect.size;

      for (int p = 0; p < frame.poses.Length; p++)
      {
        var pose = frame.poses[p];
        var ui = _poseUIs[p];
        if (pose == null || pose.landmarks == null)
        {
          SetActive(ui, false);
          continue;
        }

        SetActive(ui, true);
        EnsurePoints(ui, pose.landmarks.Length);
        EnsureLines(ui);

        // Cache screen positions
        var pts = new Vector2[pose.landmarks.Length];
        for (int i = 0; i < pose.landmarks.Length; i++)
        {
          var lm = pose.landmarks[i];
          if (lm.visibility < minVisibilityToDraw)
          {
            pts[i] = new Vector2(-9999, -9999); // sentinel off-screen
            ui.points[i].color = new Color(0,0,0,0);
            continue;
          }

          float nx = frame.isMirrored ? (1f - lm.x) : lm.x;
          float ny = lm.y;
          // Convert normalized [0..1] to UI anchored
          float x = nx * size.x;
          float y = (yFlipForUI ? (1f - ny) : ny) * size.y;
          var pos = new Vector2(x, y);

          pts[i] = pos;

          var point = ui.points[i];
          point.color = landmarkColor;
          point.SetSize(pointSize);
          point.SetAnchoredPosition(pos);
        }

        // Draw lines
        for (int c = 0; c < ui.lines.Length; c++)
        {
          var conn = Connections[c];
          int a = conn[0];
          int b = conn[1];
          var la = (a >= 0 && a < pts.Length) ? pts[a] : new Vector2(-9999, -9999);
          var lb = (b >= 0 && b < pts.Length) ? pts[b] : new Vector2(-9999, -9999);

          bool valid = la.x > -1000 && lb.x > -1000;
          var line = ui.lines[c];
          line.color = valid ? connectionColor : new Color(0,0,0,0);
          line.SetThickness(lineThickness);
          if (valid)
          {
            line.SetEndpoints(la, lb);
          }
          else
          {
            // Collapse off-screen if invalid
            line.SetEndpoints(Vector2.zero, Vector2.zero);
          }
        }
      }

      // Hide unused pose UIs
      for (int i = frame.poses.Length; i < _poseUIs.Count; i++)
      {
        SetActive(_poseUIs[i], false);
      }
    }

    private void EnsurePoseUICount(int count)
    {
      while (_poseUIs.Count < count)
      {
        _poseUIs.Add(CreatePoseUI(_poseUIs.Count));
      }
    }

    private PoseUI CreatePoseUI(int index)
    {
      var go = new GameObject($"Pose_{index}", typeof(RectTransform));
      var rt = go.GetComponent<RectTransform>();
      rt.SetParent(_root, false);
      rt.anchorMin = Vector2.zero;
      rt.anchorMax = Vector2.zero;
      rt.pivot = Vector2.zero;
      rt.sizeDelta = Vector2.zero;
      var ui = new PoseUI
      {
        rootGO = go,
        points = new UIPoint[0],
        lines = new UILineRenderer[Connections.Length]
      };

      for (int i = 0; i < ui.lines.Length; i++)
      {
        var lineGO = new GameObject($"Line_{i}", typeof(RectTransform), typeof(UILineRenderer));
        var lrt = lineGO.GetComponent<RectTransform>();
        lrt.SetParent(rt, false);
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.zero;
        lrt.pivot = Vector2.zero;
        lrt.sizeDelta = Vector2.zero;

        var lr = lineGO.GetComponent<UILineRenderer>();
        lr.color = connectionColor;
        lr.thickness = lineThickness;
        ui.lines[i] = lr;
      }

      return ui;
    }

    private void EnsurePoints(PoseUI ui, int count)
    {
      if (ui.points != null && ui.points.Length == count) return;

      // Destroy old points
      if (ui.points != null)
      {
        for (int i = 0; i < ui.points.Length; i++)
        {
          if (ui.points[i] != null)
          {
            Destroy(ui.points[i].gameObject);
          }
        }
      }

      ui.points = new UIPoint[count];
      var parent = ui.rootGO.GetComponent<RectTransform>();
      for (int i = 0; i < count; i++)
      {
        var pGO = new GameObject($"P_{i}", typeof(RectTransform), typeof(UIPoint));
        var prt = pGO.GetComponent<RectTransform>();
        prt.SetParent(parent, false);
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.zero;
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = Vector2.zero;

        var point = pGO.GetComponent<UIPoint>();
        point.color = landmarkColor;
        point.size = pointSize;
        ui.points[i] = point;
      }
    }

    private void EnsureLines(PoseUI ui)
    {
      if (ui.lines == null || ui.lines.Length != Connections.Length)
      {
        // Lines are created in CreatePoseUI; if mismatch, rebuild
        if (ui.lines != null)
        {
          for (int i = 0; i < ui.lines.Length; i++)
          {
            if (ui.lines[i] != null)
              Destroy(ui.lines[i].gameObject);
          }
        }
        ui.lines = new UILineRenderer[Connections.Length];
        var parent = ui.rootGO.GetComponent<RectTransform>();
        for (int i = 0; i < ui.lines.Length; i++)
        {
          var lineGO = new GameObject($"Line_{i}", typeof(RectTransform), typeof(UILineRenderer));
          var lrt = lineGO.GetComponent<RectTransform>();
          lrt.SetParent(parent, false);
          lrt.anchorMin = Vector2.zero;
          lrt.anchorMax = Vector2.zero;
          lrt.pivot = Vector2.zero;
          lrt.sizeDelta = Vector2.zero;

          var lr = lineGO.GetComponent<UILineRenderer>();
          lr.color = connectionColor;
          lr.thickness = lineThickness;
          ui.lines[i] = lr;
        }
      }
    }

    private static void SetActive(PoseUI ui, bool active)
    {
      if (ui != null && ui.rootGO != null)
      {
        if (ui.rootGO.activeSelf != active)
          ui.rootGO.SetActive(active);
      }
    }
  }
}
