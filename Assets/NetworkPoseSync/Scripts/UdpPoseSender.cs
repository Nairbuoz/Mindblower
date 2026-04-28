using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace NetworkPoseSync
{
  public class UdpPoseSender : MonoBehaviour
  {
    [Header("Network")]
    public string remoteIp = "192.168.0.100";
    public int remotePort = 50000;

    [Header("Sending")]
    public bool autoSend = false;
    [Range(1, 120)]
    public int maxSendFps = 60;
    public bool includeMirroredFlag = false;

    private UdpClient _client;
    private IPEndPoint _endPoint;
    private float _nextSendTime;
    private uint _frameId;
    private NetPose[] _currentPoses;
    private bool _currentMirrored;

    private void Awake()
    {
      ValidateEndpoint();
    }

    private void OnEnable()
    {
      EnsureClient();
    }

    private void OnDisable()
    {
      CloseClient();
    }

    private void Update()
    {
      if (!autoSend) return;
      if (_currentPoses == null || _currentPoses.Length == 0) return;

      float interval = 1f / Mathf.Max(1, maxSendFps);
      if (Time.unscaledTime >= _nextSendTime)
      {
        _nextSendTime = Time.unscaledTime + interval;
        SendCurrent();
      }
    }

    public void UpdateCurrentPoses(NetPose[] poses, bool isMirrored = false)
    {
      _currentPoses = poses;
      _currentMirrored = isMirrored;
    }

    public void SubmitPoses(NetPose[] poses, bool isMirrored = false)
    {
      if (poses == null) return;

      var frame = new NetPoseFrame(
        frameId: _frameId++,
        timestamp: (double)Time.realtimeSinceStartup,
        isMirrored: includeMirroredFlag ? isMirrored : false,
        poses: poses
      );

      var bytes = PosePacketSerializer.Serialize(frame);
      SendBytesAsync(bytes);
    }

    public void SubmitPosesFromArrays(float[] packedXYZv, int landmarkCountPerPose, bool isMirrored = false)
    {
      if (packedXYZv == null || landmarkCountPerPose <= 0) return;
      int valuesPerLandmark = 4;
      int totalLandmarks = packedXYZv.Length / valuesPerLandmark;
      if (totalLandmarks % landmarkCountPerPose != 0) return;

      int poseCount = totalLandmarks / landmarkCountPerPose;
      var poses = new NetPose[poseCount];
      int idx = 0;
      for (int p = 0; p < poseCount; p++)
      {
        var pose = new NetPose(landmarkCountPerPose);
        for (int i = 0; i < landmarkCountPerPose; i++)
        {
          float x = packedXYZv[idx++]; // normalized 0..1
          float y = packedXYZv[idx++];
          float z = packedXYZv[idx++];
          float v = packedXYZv[idx++];
          pose.landmarks[i] = new NetLandmark(x, y, z, v);
        }
        poses[p] = pose;
      }

      SubmitPoses(poses, isMirrored);
    }

    private void SendCurrent()
    {
      if (_currentPoses == null) return;
      SubmitPoses(_currentPoses, _currentMirrored);
    }

    private void EnsureClient()
    {
      if (_client != null) return;
      ValidateEndpoint();
      _client = new UdpClient();
    }

    private void CloseClient()
    {
      try { _client?.Close(); } catch { }
      _client = null;
    }

    private void ValidateEndpoint()
    {
      if (string.IsNullOrEmpty(remoteIp))
      {
        remoteIp = "127.0.0.1";
      }
      IPAddress ip;
      if (!IPAddress.TryParse(remoteIp, out ip))
      {
        try
        {
          var entry = Dns.GetHostEntry(remoteIp);
          if (entry.AddressList != null && entry.AddressList.Length > 0)
          {
            ip = entry.AddressList[0];
          }
          else
          {
            ip = IPAddress.Loopback;
          }
        }
        catch
        {
          ip = IPAddress.Loopback;
        }
      }
      _endPoint = new IPEndPoint(ip, Mathf.Clamp(remotePort, 1, 65535));
    }

    private async void SendBytesAsync(byte[] bytes)
    {
      EnsureClient();
      if (_client == null) return;
      try
      {
        await _client.SendAsync(bytes, bytes.Length, _endPoint).ConfigureAwait(false);
      }
      catch (ObjectDisposedException) { }
      catch (Exception ex)
      {
        Debug.LogWarning($"UdpPoseSender send error: {ex.Message}");
      }
    }
  }
}
