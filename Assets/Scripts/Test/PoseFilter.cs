using System.Collections.Generic;
using UnityEngine;

public class PoseFilter
{
    readonly float alphaPos;
    readonly float alphaRot;
    readonly int delayFrames;
    readonly Queue<Pose> buffer = new();
    bool has;
    Vector3 pos;
    Quaternion rot;

    public PoseFilter(float alphaPos = 0.25f, float alphaRot = 0.25f, int delayFrames = 0)
    {
        this.alphaPos = Mathf.Clamp01(alphaPos);
        this.alphaRot = Mathf.Clamp01(alphaRot);
        this.delayFrames = Mathf.Max(0, delayFrames);
    }

    public Pose Update(Pose measurement)
    {
        if (!has)
        {
            pos = measurement.position;
            rot = measurement.rotation;
            has = true;
        }
        else
        {
            pos = Vector3.Lerp(pos, measurement.position, alphaPos);
            rot = Quaternion.Slerp(rot, measurement.rotation, alphaRot);
        }

        var smoothed = new Pose(pos, rot);

        if (delayFrames <= 0) return smoothed;

        buffer.Enqueue(smoothed);
        while (buffer.Count > delayFrames + 1) buffer.Dequeue();
        return buffer.Count == delayFrames + 1 ? buffer.Peek() : smoothed;
    }
}