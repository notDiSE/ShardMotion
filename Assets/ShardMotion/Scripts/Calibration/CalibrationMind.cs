using System.Collections.Generic;
using ShardMotion;
using UnityEngine;

namespace ShardMotion.Calibration
{
    /// <summary>
    /// Calibration mind handles the position calibration of Targets
    /// </summary>
    public static class CalibrationMind
    {
        // each camera gets inverse point and calibrated point
        private static Dictionary<TrackingCamera, Dictionary<ArUcoTarget, GameObject>> inversePoints = new Dictionary<TrackingCamera, Dictionary<ArUcoTarget, GameObject>>();
        private static Dictionary<TrackingCamera, GameObject> calibratedPoints = new Dictionary<TrackingCamera, GameObject>();
        private static GameObject root;

        /// <summary>
        /// Data from Tracking Camera
        /// </summary>
        /// <param name="cam"> Reference to <see cref="TrackingCamera"/></param>
        /// <param name="records">List of records of markers from this frame</param>
        public static void Calibrate(TrackingCamera cam, List<TrackingCamera.TrackingRecord> records)
        {
            // Add calibraeted point for this camera if it doesnt exist
            if (!calibratedPoints.ContainsKey(cam))
            {
                calibratedPoints.Add(cam, new GameObject("Calibrated point for " + cam.name));
            }

            // Add inverse point for this camera if it doesnt exist
            if (!inversePoints.ContainsKey(cam))
            {
                inversePoints.Add(cam, new Dictionary<ArUcoTarget, GameObject>());
            }
            
            // Only keeps the best record for this camera (best dot)
            TrackingCamera.TrackingRecord bestRecord = null;
            foreach (TrackingCamera.TrackingRecord record in records)
            {
                if(bestRecord == null) bestRecord = record;
                else if(bestRecord.Dot<record.Dot) bestRecord = record;
            } 
            //if(bestRecord != null) cam.transform.SetPositionAndRotation(-bestRecord.Pos, Quaternion.identity);
            //if(bestRecord != null) cam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Inverse( ArUcoTarget.ToForwardRotation(bestRecord.Target.forwardAxis)));
            
            if (bestRecord != null)
            {
                
                if (inversePoints.TryGetValue(cam, out Dictionary<ArUcoTarget, GameObject> points))
                {
                    if (!points.ContainsKey(bestRecord.Target))
                    {
                        points.Add(bestRecord.Target, new GameObject("Inverse point for code " + bestRecord.Target.markerId));
                    }

                    if (points.TryGetValue(bestRecord.Target, out GameObject inversePoint) && calibratedPoints.TryGetValue(cam, out GameObject calibratedPoint))
                    {
                        //Quaternion forwardRotation = Quaternion.Euler(bestRecord.Target.rotationOffset) * Quaternion.Euler(0, 0, 180) ;
                        Quaternion forwardRotation = Quaternion.Inverse(Quaternion.Euler(bestRecord.Target.rotationOffset));
                    
                        // Camera gets set to the position of detected Marker in scene (sor of like inverse projection, we are trying to find, where the marker will be detected now)
                        var pos = forwardRotation * -bestRecord.Target.positionOffset;
                        cam.transform.SetPositionAndRotation(pos, forwardRotation);

                        // inverse point gets set to the position that the marker is detected in
                        inversePoint.transform.SetPositionAndRotation(bestRecord.Pos, bestRecord.Rot);
                        
                        //Save the relative pose
                        RelativePose relativePose = RelativePoseMath.Capture(cam.transform, inversePoint.transform);
                        
                        // move
                        inversePoint.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
                        
                        // Apply back relative pose
                        RelativePoseMath.Apply(calibratedPoint.transform, inversePoint.transform, relativePose);


                        var oldPos = cam.calibratedPosAverage;
                        var oldRot = cam.calibratedRotAverage;

                        // Camera gets X ammount of records and makes the average
                        
                        // no values recorded before
                        if (cam.calibratedValues == 0)
                        {
                            cam.calibratedPosAverage = calibratedPoint.transform.position;
                            cam.calibratedRotAverage = calibratedPoint.transform.rotation;
                            cam.calibratedAmountDebug = 0f;
                        }
                        else
                        {
                            // Running average position of camera
                            cam.calibratedPosAverage = (cam.calibratedValues * cam.calibratedPosAverage + calibratedPoint.transform.position) / (cam.calibratedValues + 1);

                            // running average rotation, using slerp for correct values, cannot simpy make averge from rotation
                            float t = 1f / (cam.calibratedValues + 1);
                            cam.calibratedRotAverage = Quaternion.Slerp(cam.calibratedRotAverage, calibratedPoint.transform.rotation, t);
                        }

                        cam.calibratedValues++; // camera gets calibrated values counter up

                        // Calculate progress (first 80% is calculated from number of values)
                        float baseProgress = Mathf.Clamp01(cam.calibratedValues / 100f) * 0.8f;
                        float progress = baseProgress;

                        // if there is more than 100 calibrated values.
                        if (cam.calibratedValues >= 100)
                        {
                            float posEps = 0.0001f;
                            float rotEpsDeg = 0.1f;

                            // measure how much average moved
                            float posErr = Mathf.Sqrt((oldPos - cam.calibratedPosAverage).sqrMagnitude);
                            float rotErr = Quaternion.Angle(oldRot, cam.calibratedRotAverage);

                            // normalize using tolerance
                            float posNorm = posErr / posEps;
                            float rotNorm = rotErr / rotEpsDeg;

                            // progress calculation (last 20% is calculated from how close the worst value is from treshold)
                            float worstNorm = Mathf.Max(posNorm, rotNorm);
                            float stability01 = 1f - Mathf.Clamp01(worstNorm);
                            float tail = 0.2f * stability01;
                            progress = 0.8f + tail;
                            
                            // if pos and rot changes less, than treshold
                            if (posErr < posEps && posErr < posEps)
                            {
                                SaveCalibration(cam); // save the calibration
                                progress = 1f;
                            }
                        }

                        // if there is more than 1000 recorded values and still cannot make average, the calibraiton failed
                        if (cam.calibratedValues >= 1000)
                        {
                            cam.calibrationState = TrackingCamera.CalibrationState.Failed;
                            cam.enabled = false;
                        }

                        cam.calibratedAmountDebug = Mathf.Max(cam.calibratedAmountDebug, progress);

                    }
                }
                
            }
        }
    

        /// <summary>
        /// Saves calibration
        /// </summary>
        /// <param name="cam">for this camera</param>
        public static void SaveCalibration(TrackingCamera cam)
        {
            // sets the position of the cam to the calibrated point position and rotation
            if (calibratedPoints.TryGetValue(cam, out GameObject calibratedPoint))
            {
                cam.calibrationState = TrackingCamera.CalibrationState.Calibrated;
                cam.transform.position = calibratedPoint.transform.position;
                cam.transform.rotation = calibratedPoint.transform.rotation;
                cam.SavePos();
            }
        }

        /// <summary>
        /// Destroys created calibration device
        /// </summary>
        public static void Cleanup()
        {
        
            // Inverse points and calibrated points get cleared
            foreach (KeyValuePair<TrackingCamera, Dictionary<ArUcoTarget, GameObject>> point in inversePoints)
            {
                foreach (KeyValuePair<ArUcoTarget, GameObject> pair in point.Value)
                {
                    Object.Destroy(pair.Value);
                }
            }

            foreach (KeyValuePair<TrackingCamera,GameObject> calibratedPoint in calibratedPoints)
            {
                Object.Destroy(calibratedPoint.Value);
            }
        
            inversePoints.Clear();
            calibratedPoints.Clear();
            
            // each target in calibrated device is unregistred
            foreach (var target in root.GetComponentsInChildren(typeof(ArUcoTarget)))
            {
                ArUcoRegistry.Unregister(target as ArUcoTarget);
            }
            Object.Destroy(root);
        }

        /// <summary>
        /// Created instance of virtual calibration device from parameters
        /// </summary>
        /// <param name="calibrationDevice">Referecne to calibration device, that it will be created from</param>
        public static void CreateTarget(CalibrationDevice calibrationDevice)
        {
        
            inversePoints.Clear();
            calibratedPoints.Clear();
        
            root = new GameObject("CalibrationTarget");
        
            GameObject go = new GameObject("CalibrationTargetForward");
            go.transform.SetParent(root.transform);
            ArUcoTarget forward = go.AddComponent<ArUcoTarget>();
            forward.markerId = calibrationDevice.forwardID;
            forward.markerSize = calibrationDevice.codeSize;
            forward.rotationOffset = new Vector3(0, 0, 180);
            forward.positionOffset = new Vector3(0, 0, -calibrationDevice.cubeSize/2);
            //forward.Reregister();
            ArUcoRegistry.Register(forward);
        
            go = new GameObject("CalibrationTargetRight");
            go.transform.SetParent(root.transform);
            ArUcoTarget right = go.AddComponent<ArUcoTarget>();
            right.markerId = calibrationDevice.rightID;
            right.markerSize = calibrationDevice.codeSize;
            right.rotationOffset = new Vector3(180, -90, 0);
            right.positionOffset = new Vector3(-calibrationDevice.cubeSize/2,0,0);
            //right.Reregister();
            ArUcoRegistry.Register(right);
        
            go = new GameObject("CalibrationTargetBack");
            go.transform.SetParent(root.transform);
            ArUcoTarget backwards = go.AddComponent<ArUcoTarget>();
            backwards.markerId = calibrationDevice.backwardID;
            backwards.markerSize = calibrationDevice.codeSize;
            backwards.rotationOffset = new Vector3(0, 180, 180);
            backwards.positionOffset = new Vector3(0, 0, calibrationDevice.cubeSize/2);
            //backwards.Reregister();
            ArUcoRegistry.Register(backwards);
        
            go = new GameObject("CalibrationTargetLeft");
            go.transform.SetParent(root.transform);
            ArUcoTarget left = go.AddComponent<ArUcoTarget>();
            left.markerId = calibrationDevice.leftID;
            left.markerSize = calibrationDevice.codeSize;
            left.rotationOffset = new Vector3(180, 90, 0);
            left.positionOffset = new Vector3(calibrationDevice.cubeSize/2,0,0);
            //left.Reregister();
            ArUcoRegistry.Register(left);
        }
    }
}
