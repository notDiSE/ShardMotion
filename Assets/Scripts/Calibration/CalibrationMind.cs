using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class CalibrationMind
{
    private static GameObject hubu;
    public static void Calibrate(TrackingCamera cam, List<TrackingCamera.TrackingRecord> records)
    {
        Debug.Log("-------------------------");
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
            /*
            Quaternion rawMarkerRot = ArUcoTarget.ToForwardRotation(bestRecord.Target.forwardAxis) * bestRecord.Rot;

            Vector3 forward = rawMarkerRot * Vector3.forward;
            Vector3 up = rawMarkerRot * Vector3.up;

            Quaternion invRot = Quaternion.Inverse(rawMarkerRot) * quaternion.Euler(cam.calibrationDevice.UpVector);

            Vector3 localPos = bestRecord.Pos;
    
            Vector3 pos = -(invRot * localPos);

            hubu.transform.SetPositionAndRotation(pos, invRot);
            */
        }
    }

    public static void CreateTarget(CalibrationDevice calibrationDevice)
    {
        hubu = new GameObject("Hubu");
        GameObject root = new GameObject("CalibrationTarget");
        
        GameObject go = new GameObject("CalibrationTargetForward");
        go.transform.SetParent(root.transform);
        ArUcoTarget forward = go.AddComponent<ArUcoTarget>();
        forward.markerId = calibrationDevice.forwardID;
        forward.forwardAxis = MarkerAxis.Z_POS;
        forward.gizmoMarkerSize = calibrationDevice.codeSize;
        forward.positionOffset = new Vector3(0,0,-calibrationDevice.cubeSize/2);
        //forward.Reregister();
        ArUcoRegistry.Register(forward);
        
        go = new GameObject("CalibrationTargetRight");
        go.transform.SetParent(root.transform);
        ArUcoTarget right = go.AddComponent<ArUcoTarget>();
        right.markerId = calibrationDevice.rightID;
        right.forwardAxis = MarkerAxis.X_NEG;
        right.gizmoMarkerSize = calibrationDevice.codeSize;
        right.positionOffset = new Vector3(0,0,-calibrationDevice.cubeSize/2);
        //right.Reregister();
        ArUcoRegistry.Register(right);
        
        go = new GameObject("CalibrationTargetBack");
        go.transform.SetParent(root.transform);
        ArUcoTarget backwards = go.AddComponent<ArUcoTarget>();
        backwards.markerId = calibrationDevice.backwardID;
        backwards.forwardAxis = MarkerAxis.Z_NEG;
        backwards.gizmoMarkerSize = calibrationDevice.codeSize;
        backwards.positionOffset = new Vector3(0,0,-calibrationDevice.cubeSize/2);
        //backwards.Reregister();
        ArUcoRegistry.Register(backwards);
        
        go = new GameObject("CalibrationTargetLeft");
        go.transform.SetParent(root.transform);
        ArUcoTarget left = go.AddComponent<ArUcoTarget>();
        left.markerId = calibrationDevice.leftID;
        left.forwardAxis = MarkerAxis.X_POS;
        left.gizmoMarkerSize = calibrationDevice.codeSize;
        left.positionOffset = new Vector3(0,0,-calibrationDevice.cubeSize/2);
        //left.Reregister();
        ArUcoRegistry.Register(left);
        
    }
}
