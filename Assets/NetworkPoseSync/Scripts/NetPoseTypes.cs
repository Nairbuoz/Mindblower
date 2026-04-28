using System;

namespace NetworkPoseSync
{
  [Serializable]
  public struct NetLandmark
  {
    public float x;
    public float y;
    public float z;
    public float visibility;

    public NetLandmark(float x, float y, float z, float visibility)
    {
      this.x = x;
      this.y = y;
      this.z = z;
      this.visibility = visibility;
    }
  }

  [Serializable]
  public class NetPose
  {
    public NetLandmark[] landmarks;

    public NetPose(int landmarkCount)
    {
      landmarks = new NetLandmark[landmarkCount];
    }
  }

  [Serializable]
  public class NetPoseFrame
  {
    public uint frameId;
    public double timestamp; // seconds
    public bool isMirrored;
    public NetPose[] poses;

    public NetPoseFrame(uint frameId, double timestamp, bool isMirrored, NetPose[] poses)
    {
      this.frameId = frameId;
      this.timestamp = timestamp;
      this.isMirrored = isMirrored;
      this.poses = poses;
    }
  }
}
