using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NetworkPoseSync
{
  public class UdpPoseReceiver : MonoBehaviour
  {
    public int listenPort = 50000;
    public bool autoStart = true;

    public NetPoseFrame LatestFrame { get; private set; }

    private UdpClient _client;
    private CancellationTokenSource _cts;
    private readonly ConcurrentQueue<NetPoseFrame> _queue = new ConcurrentQueue<NetPoseFrame>();

    private void OnEnable()
    {
      if (autoStart)
        StartReceiver();
    }

    private void OnDisable()
    {
      StopReceiver();
      LatestFrame = null;
      while (_queue.TryDequeue(out _)) {}
    }

    public void StartReceiver()
    {
      if (_client != null) return;

      try
      {
        _cts = new CancellationTokenSource();
        _client = new UdpClient(listenPort);
        Task.Run(() => ReceiveLoop(_cts.Token), _cts.Token);
      }
      catch (Exception ex)
      {
        Debug.LogError($"UdpPoseReceiver start failed: {ex.Message}");
        StopReceiver();
      }
    }

    public void StopReceiver()
    {
      try { _cts?.Cancel(); } catch { }
      try { _client?.Close(); } catch { }
      _cts = null;
      _client = null;
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
      IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);
      while (!ct.IsCancellationRequested)
      {
        try
        {
          var result = await _client.ReceiveAsync();
          if (result.Buffer != null && result.Buffer.Length > 0)
          {
            if (PosePacketSerializer.TryDeserialize(result.Buffer, result.Buffer.Length, out var frame, out var err))
            {
              _queue.Enqueue(frame);
            }
            else
            {
              Debug.LogWarning($"UdpPoseReceiver deserialize failed: {err}");
            }
          }
        }
        catch (ObjectDisposedException)
        {
          break;
        }
        catch (SocketException)
        {
          if (ct.IsCancellationRequested) break;
        }
        catch (Exception ex)
        {
          if (ct.IsCancellationRequested) break;
          Debug.LogWarning($"UdpPoseReceiver error: {ex.Message}");
        }
      }
    }

    private void Update()
    {
      // Always keep the latest frame, drop older ones for low latency
      NetPoseFrame frame;
      bool gotAny = false;
      while (_queue.TryDequeue(out frame))
      {
        LatestFrame = frame;
        gotAny = true;
      }

      if (gotAny)
      {
        // Could raise an event or notify another component here if needed
      }
    }
  }
}
