using System.Collections.Generic;
using System.Linq;
using ShardMotion.Settings;
using UnityEngine;

namespace ShardMotion
{
    public static class TrackingMind
    {
    
        static HashSet<TrackingCamera> cameras = new HashSet<TrackingCamera>();
        static List<TrackingCamera.TrackingRecord> commitedRecords = new List<TrackingCamera.TrackingRecord>();
        static HashSet<TrackingCamera> committedCameras = new HashSet<TrackingCamera>();
        static Dictionary<GameObject, PoseFilter> filters = new Dictionary<GameObject, PoseFilter>();
        static int commited = 0;
        static float lerpBorder = 0.5f;

        /// <summary>
        /// Adds camera to the list of known cameras, will expect data from the camera
        /// </summary>
        /// <param name="cam"> reference to the <see cref="TrackingCamera"/></param>
        public static void Register(TrackingCamera cam)
        {
            cameras.Add(cam);
        }

        /// <summary>
        /// Removes the camera from know cameras
        /// </summary>
        /// <param name="cam">reference to the <see cref="TrackingCamera"/></param>
        public static void Unregister(TrackingCamera cam)
        {
            cameras.Remove(cam);
        }

        /// <summary>
        /// Passed data from camera for processing
        /// </summary>
        /// <param name="cam">reference to the <see cref="TrackingCamera"/> that sent the data</param>
        /// <param name="records"> list of records the camera collected this frame</param>
        public static void Commit(TrackingCamera cam, List<TrackingCamera.TrackingRecord> records)
        {
            if (!committedCameras.Add(cam)) // if the camera is already in the list of commited cameras (got 2 commits from this camera while didnt get the data from some of the other cameras)
            {
                Evaluate(); // poses are evaluated from current data without the late cameras, list of commited cameras is cleared while doing this
                Commit(cam,records); // then tries it again for this camera
                return;
            }
            
            // records are added to complete list of recorded markers with poses
            if(records != null) commitedRecords.AddRange(records);
            commited++;
            
            // if all cameras commited
            if (commited>=cameras.Count)
            {
                Evaluate();
            }
        
        }
    
        /// <summary>
        /// Merge of all pose data
        /// </summary>
        static void Evaluate()
        {
            foreach (var record in commitedRecords)
            {
                record.Dot = Mathf.Clamp01((record.Dot - lerpBorder) * (1 / lerpBorder)); // dot is remaped from 0.5-1 to 0-1, to minimise marker popin 
            }
        
            Dictionary<GameObject, (Vector3 pos, Quaternion rot, float totalDot)> perObject = new Dictionary<GameObject, (Vector3, Quaternion, float)>(); // total computed position and rotation for one gameobject

            foreach (TrackingCamera.TrackingRecord record in commitedRecords.OrderBy(r => r.Target.markerId)) // must be looped in same order every frame, hence OrderBy
            {
                var pose = record.Target.CorrectedPose(record.Pos, record.Rot); // computed object rotation from marker

                if (perObject.TryGetValue(record.Target.gameObject, out var current)) // if we already have some computed pose for this object
                {
                    // Get weight of the current data based on the dot and total dot already computed with
                    float totalDot = current.totalDot + record.Dot;
                    float t = record.Dot / totalDot;

                    // Interpolate between position and rotation of old computed and new record
                    current.pos = Vector3.Lerp(current.pos, pose.pos, t);
                    current.rot = Quaternion.Slerp(current.rot, pose.rot, t);
                    current.totalDot = totalDot;
                    perObject[record.Target.gameObject] = current;
                }
                else
                {
                    perObject.Add(record.Target.gameObject, (pose.pos, pose.rot, record.Dot)); // first record for this object
                }
            }
        
            // Pose filter is applied for each object
            foreach (var record in perObject)
            {
                // if the Gameobject doesnt have any pose filter (its his first appearance)
                if (!filters.ContainsKey(record.Key)) filters[record.Key] = new PoseFilter(ShardMotionConfig.PositionSmoothing, ShardMotionConfig.RotationSmoothing);
                
                // Get smoothed pose
                Pose p = filters[record.Key].GetSmoothed(record.Value.Item1, record.Value.Item2);
                
                //position and rotation is set.
                record.Key.transform.SetPositionAndRotation(p.position, p.rotation);
            }
            
            commited = 0;
            commitedRecords.Clear();
            committedCameras.Clear();
        }
    
    
    }
}
