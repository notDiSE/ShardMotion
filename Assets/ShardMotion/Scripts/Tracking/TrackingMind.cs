using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;


public static class TrackingMind
{
    
    static HashSet<TrackingCamera> cameras = new HashSet<TrackingCamera>();
    static List<TrackingCamera.TrackingRecord> commitedRecords = new List<TrackingCamera.TrackingRecord>();
    static HashSet<TrackingCamera> committedCameras = new HashSet<TrackingCamera>();
    static int commited = 0;
    static float lerpBorder = 0.5f;

    public static void Register(TrackingCamera cam)
    {
        cameras.Add(cam);
    }

    public static void Unregister(TrackingCamera cam)
    {
        cameras.Remove(cam);
    }

    public static void Commit(TrackingCamera cam, List<TrackingCamera.TrackingRecord> records)
    {
        if (!committedCameras.Add(cam))
        {
            Evaluate();
            Commit(cam,records);
            return;
        }
            

        if(records != null) commitedRecords.AddRange(records);
        commited++;
        
        if (commited>=cameras.Count)
        {
            Evaluate();
        }
        
    }
    
    
    static void Evaluate()
    {
        foreach (var record in commitedRecords)
        {
            record.Dot = Mathf.Clamp01((record.Dot - lerpBorder) * (1 / lerpBorder));
        }
        
        Dictionary<GameObject, (Vector3 pos, Quaternion rot, float totalDot)> perTarget = new Dictionary<GameObject, (Vector3, Quaternion, float)>();

        foreach (TrackingCamera.TrackingRecord record in commitedRecords.OrderBy(r => r.Target.markerId))
        {
            var pose = record.Target.CorrectedPose(record.Pos, record.Rot);

            if (perTarget.TryGetValue(record.Target.gameObject, out var current))
            {
                float totalDot = current.totalDot + record.Dot;
                float t = record.Dot / totalDot;

                current.pos = Vector3.Lerp(current.pos, pose.pos, t);
                current.rot = Quaternion.Slerp(current.rot, pose.rot, t);
                current.totalDot = totalDot;
                perTarget[record.Target.gameObject] = current;
            }
            else
            {
                perTarget.Add(record.Target.gameObject, (pose.pos, pose.rot, record.Dot));
            }
        }
        
        foreach (var record in perTarget)
        {
            record.Key.transform.SetPositionAndRotation(record.Value.Item1, record.Value.Item2);
        }
        
        /*
        Dictionary<ArUcoTarget, TrackingCamera.TrackingRecord> bestPerTarget = new Dictionary<ArUcoTarget, TrackingCamera.TrackingRecord>();

        foreach (var record in commitedRecords)
        {
            if (record.Target == null)
                continue;

            if (!bestPerTarget.TryGetValue(record.Target, out var best) || record.Dot > best.Dot)
            {
                bestPerTarget[record.Target] = record;
            }
        }
        
                foreach (var record in bestPerTarget.Values)
           {
               record.Apply();
           }
           
        */
        
        /*
        Dictionary<ArUcoTarget, TrackingCamera.TrackingRecord> mergedPerTarget = new Dictionary<ArUcoTarget, TrackingCamera.TrackingRecord>();

        foreach (var record in commitedRecords)
        {
            if (record.Target == null)
                continue;

            if (!mergedPerTarget.TryGetValue(record.Target, out var existing))
            {
                mergedPerTarget[record.Target] = new TrackingCamera.TrackingRecord
                {
                    Target = record.Target,
                    Pos = record.Pos,
                    Rot = record.Rot,
                    Dot = record.Dot
                };
            }
            else
            {
                float totalDot = existing.Dot + record.Dot;
                float t = record.Dot / totalDot;

                existing.Pos = Vector3.Lerp(existing.Pos, record.Pos, t);
                existing.Rot = Quaternion.Slerp(existing.Rot, record.Rot, t);
                existing.Dot = totalDot;
            }
        }
            

        foreach (var record in mergedPerTarget.Values)
        {
            record.Apply();
        }
        */
        
        // Vzdavam to tady, dodělat rano co to sakra je.
        
        commited = 0;
        commitedRecords.Clear();
        committedCameras.Clear();
    }
    
    
}
