using System;
using System.IO;

namespace NetworkPoseSync
{
  public static class PosePacketSerializer
  {
    // "POSE"
    public const uint Magic = 0x504F5345;
    public const ushort Version = 1;

    public static byte[] Serialize(NetPoseFrame frame)
    {
      if (frame == null) throw new ArgumentNullException(nameof(frame));

      using (var ms = new MemoryStream(1024))
      using (var bw = new BinaryWriter(ms))
      {
        bw.Write(Magic);
        bw.Write(Version);
        bw.Write(frame.frameId);
        bw.Write(frame.timestamp);
        bw.Write(frame.isMirrored);

        ushort poseCount = (ushort)((frame.poses != null) ? frame.poses.Length : 0);
        bw.Write(poseCount);

        if (frame.poses != null)
        {
          for (int p = 0; p < frame.poses.Length; p++)
          {
            var pose = frame.poses[p];
            ushort landmarkCount = (ushort)((pose != null && pose.landmarks != null) ? pose.landmarks.Length : 0);
            bw.Write(landmarkCount);

            if (pose != null && pose.landmarks != null)
            {
              for (int i = 0; i < pose.landmarks.Length; i++)
              {
                var lm = pose.landmarks[i];
                bw.Write(lm.x);
                bw.Write(lm.y);
                bw.Write(lm.z);
                bw.Write(lm.visibility);
              }
            }
          }
        }

        bw.Flush();
        return ms.ToArray();
      }
    }

    public static bool TryDeserialize(byte[] data, int length, out NetPoseFrame frame, out string error)
    {
      frame = null;
      error = null;

      if (data == null || length <= 0)
      {
        error = "No data";
        return false;
      }

      try
      {
        using (var ms = new MemoryStream(data, 0, length))
        using (var br = new BinaryReader(ms))
        {
          uint magic = br.ReadUInt32();
          if (magic != Magic)
          {
            error = "Magic mismatch";
            return false;
          }

          ushort version = br.ReadUInt16();
          if (version != Version)
          {
            error = "Version mismatch";
            return false;
          }

          uint frameId = br.ReadUInt32();
          double timestamp = br.ReadDouble();
          bool isMirrored = br.ReadBoolean();

          ushort poseCount = br.ReadUInt16();
          var poses = new NetPose[poseCount];

          for (int p = 0; p < poseCount; p++)
          {
            ushort lmCount = br.ReadUInt16();
            var pose = new NetPose(lmCount);

            for (int i = 0; i < lmCount; i++)
            {
              float x = br.ReadSingle();
              float y = br.ReadSingle();
              float z = br.ReadSingle();
              float v = br.ReadSingle();
              pose.landmarks[i] = new NetLandmark(x, y, z, v);
            }

            poses[p] = pose;
          }

          frame = new NetPoseFrame(frameId, timestamp, isMirrored, poses);
          return true;
        }
      }
      catch (Exception ex)
      {
        error = $"Deserialize error: {ex.Message}";
        frame = null;
        return false;
      }
    }
  }
}
