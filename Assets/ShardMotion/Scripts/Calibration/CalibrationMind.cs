using System.Collections.Generic;
using ShardMotion;
using UnityEngine;

namespace ShardMotion.Calibration
{
    public static class CalibrationMind
    {

        private static Dictionary<TrackingCamera, Dictionary<ArUcoTarget, GameObject>> inversePoints = new Dictionary<TrackingCamera, Dictionary<ArUcoTarget, GameObject>>();
        private static Dictionary<TrackingCamera, GameObject> calibratedPoints = new Dictionary<TrackingCamera, GameObject>();
        private static GameObject root;

        public static void Calibrate(TrackingCamera cam, List<TrackingCamera.TrackingRecord> records)
        {
            if (!calibratedPoints.ContainsKey(cam))
            {
                calibratedPoints.Add(cam, new GameObject("Calibrated point for " + cam.name));
            }

            if (!inversePoints.ContainsKey(cam))
            {
                inversePoints.Add(cam, new Dictionary<ArUcoTarget, GameObject>());
            }
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

                    if (points.TryGetValue(bestRecord.Target, out GameObject inversePoint) &&
                        calibratedPoints.TryGetValue(cam, out GameObject calibratedPoint))
                    {
                        //Quaternion forwardRotation = Quaternion.Euler(bestRecord.Target.rotationOffset) * Quaternion.Euler(0, 0, 180) ;
                        Quaternion forwardRotation = Quaternion.Inverse(Quaternion.Euler(bestRecord.Target.rotationOffset));
                    
                        var pos = forwardRotation * -bestRecord.Target.positionOffset;
                        cam.transform.SetPositionAndRotation(pos, forwardRotation);

                        inversePoint.transform.SetPositionAndRotation(bestRecord.Pos, bestRecord.Rot);
                        RelativePose relativePose = RelativePoseMath.Capture(cam.transform, inversePoint.transform);
                        inversePoint.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
                        RelativePoseMath.Apply(calibratedPoint.transform, inversePoint.transform, relativePose);


                        var oldPos = cam.calibratedPosAverage;
                        var oldRot = cam.calibratedRotAverage;

                        if (cam.calibratedValues == 0)
                        {
                            cam.calibratedPosAverage = calibratedPoint.transform.position;
                            cam.calibratedRotAverage = calibratedPoint.transform.rotation;
                            cam.calibratedAmountDebug = 0f;
                        }
                        else
                        {
                            cam.calibratedPosAverage =
                                (cam.calibratedValues * cam.calibratedPosAverage + calibratedPoint.transform.position) /
                                (cam.calibratedValues + 1);

                            float t = 1f / (cam.calibratedValues + 1);
                            cam.calibratedRotAverage = Quaternion.Slerp(
                                cam.calibratedRotAverage,
                                calibratedPoint.transform.rotation,
                                t
                            );
                        }

                        cam.calibratedValues++;

                        float baseProgress = Mathf.Clamp01(cam.calibratedValues / 100f) * 0.8f;
                        float progress = baseProgress;

                        if (cam.calibratedValues >= 100)
                        {
                            float posEps = 0.0001f;
                            float rotEpsDeg = 0.1f;

                            float posErr = Mathf.Sqrt((oldPos - cam.calibratedPosAverage).sqrMagnitude);
                            float rotErr = Quaternion.Angle(oldRot, cam.calibratedRotAverage);

                            float posNorm = posErr / posEps;
                            float rotNorm = rotErr / rotEpsDeg;

                            float worstNorm = Mathf.Max(posNorm, rotNorm);

                            float stability01 = 1f - Mathf.Clamp01(worstNorm);
                            float tail = 0.2f * stability01;

                            progress = 0.8f + tail;

                            bool posClose = posErr < posEps;
                            bool rotClose = rotErr < rotEpsDeg;

                            if (posClose && rotClose)
                            {
                                SaveCalibration(cam);
                                progress = 1f;
                            }
                        }

                        if (cam.calibratedValues >= 1000)
                        {
                            cam.calibrationState = TrackingCamera.CalibrationState.Failed;
                            cam.enabled = false;
                        }

                        cam.calibratedAmountDebug = Mathf.Max(cam.calibratedAmountDebug, progress);

                    }
                }
                /*
            Quaternion forwardRotation = ArUcoTarget.ToForwardRotation(bestRecord.Target.forwardAxis);
            if(bestRecord.Target.forwardAxis == MarkerAxis.X_NEG || bestRecord.Target.forwardAxis == MarkerAxis.X_POS) forwardRotation *= Quaternion.Euler(0, 180, 0);
            var pos =  forwardRotation * -bestRecord.Target.positionOffset;
            cam.transform.SetPositionAndRotation(pos, forwardRotation);
                
            nahradnikod.transform.SetPositionAndRotation(bestRecord.Pos, bestRecord.Rot);
            RelativePose relativePose = RelativePoseMath.Capture(cam.transform, nahradnikod.transform);
            nahradnikod.transform.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
            RelativePoseMath.Apply(nahradnikamera.transform, nahradnikod.transform, relativePose);
            */
                //Quaternion rawMarkerRot = ArUcoTarget.ToForwardRotation(bestRecord.Target.forwardAxis) * bestRecord.Rot;

                //Vector3 forward = rawMarkerRot * Vector3.forward;
                //Vector3 up = rawMarkerRot * Vector3.up;

                //Quaternion invRot = Quaternion.Inverse() * quaternion.Euler(cam.calibrationDevice.UpVector);

                //Vector3 localPos = bestRecord.Pos;

                //Vector3 pos = -(invRot * localPos);

                //Quaternion rot = Quaternion.Euler(0, 180, 0);// * ArUcoTarget.ToForwardRotation(bestRecord.Target.forwardAxis);

                //hubu.transform.SetPositionAndRotation(bestRecord.Pos,  rot);

            }
        }
    

        public static void SaveCalibration(TrackingCamera cam)
        {
            if (calibratedPoints.TryGetValue(cam, out GameObject calibratedPoint))
            {
                cam.calibrationState = TrackingCamera.CalibrationState.Calibrated;
                cam.transform.position = calibratedPoint.transform.position;
                cam.transform.rotation = calibratedPoint.transform.rotation;
                cam.SavePos();
            }
        }

        public static void Cleanup()
        {
        
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
            foreach (var target in root.GetComponentsInChildren(typeof(ArUcoTarget)))
            {
                ArUcoRegistry.Unregister(target as ArUcoTarget);
            }
            Object.Destroy(root);
        }

        public static void CreateTarget(CalibrationDevice calibrationDevice)
        {
            //nahradnikamera = new GameObject("NahradniKamera");
            //nahradnikod = new GameObject("Nahradnikod");
        
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
