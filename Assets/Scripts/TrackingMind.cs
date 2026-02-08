using System;
using System.Collections.Generic;
using UnityEngine;


public static class TrackingMind
{
    
    static HashSet<TrackingCamera> cameras = new HashSet<TrackingCamera>();
    static List<TrackingCamera.TrackingRecord> commitedRecords = new List<TrackingCamera.TrackingRecord>();
    static HashSet<TrackingCamera> committedCameras = new HashSet<TrackingCamera>();
    static int commited = 0;

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
            return;

        if(records != null) commitedRecords.AddRange(records);
        commited++;
        
        if (commited>=cameras.Count)
        {
            Evaluate();
        }
        
    }
    
    static void Evaluate()
    {
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
        
        commited = 0;
        commitedRecords.Clear();
        committedCameras.Clear();
    }
    
}
