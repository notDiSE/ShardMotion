using System;
using System.Collections;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrackingCamera : MonoBehaviour
{
    [Header("Webcam Settings")] public int sel;
    public bool isTracking;
    private WebCamTexture webCamTexture;
    private Color32[] pixelData;
    public Texture2D tex;

    [Header("ArUco Settings")] public float tagSizeMeters = 0.05f;
    public bool drawAxes = true;
    public bool drawBoxes = true;

    [Header("Smoothing")] [Range(0.01f, 1f)]
    public float alphaPos = 0.25f;

    [Range(0.01f, 1f)] public float alphaRot = 0.25f;

    private Mat frame = new Mat();
    private readonly Dictionary<int, PoseFilter> filters = new Dictionary<int, PoseFilter>();
    private List<string> camNames = new List<string>();

    private DetectorParameters detectorParams;
    private Dictionary dictionary;
    private Coroutine tickRoutine;
    
    public CalibrationDevice calibrationDevice;
    public bool startAutomatically = false;

    public int calibratedValues = 0;
    public float calibratedAmountDebug = 0;
    public Vector3 calibratedPosAverage;
    public Quaternion calibratedRotAverage;
    public CalibrationState calibrationState = CalibrationState.NotCalibrated;

    public enum CalibrationState
    {
        NotCalibrated,
        Calibrating,
        Calibrated,
        Failed
    }

//public int 
    

    public class TrackingRecord
    {
        public ArUcoTarget Target;
        public Vector3 Pos;
        public Quaternion Rot;
        public float Dot;

        public void Apply()
        {
            Target.ApplyPose(Pos, Rot);
        }
    }

    private void Start()
    {
        if(startAutomatically) StartTracking();
    }

    void OnEnable()
    {
        ScanCams();
        detectorParams = new DetectorParameters();
        dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_50);
        TrackingMind.Register(this);
    }

    void OnDisable()
    {
        StopTracking();
        if (frame != null && !frame.IsDisposed) frame.Dispose();
        TrackingMind.Unregister(this);
    }

    public void ScanCams()
    {
        camNames.Clear();
        foreach (var device in WebCamTexture.devices)
            camNames.Add(device.name);
        
        if (camNames.Count == 0) camNames.Add("No Camera Found");
        sel = Mathf.Clamp(sel, 0, Mathf.Max(0, camNames.Count - 1));
    }

    public IReadOnlyList<string> CameraNames => camNames;

    public void StartTracking()
    {
        StopTracking();
        if (WebCamTexture.devices.Length == 0) return;

        webCamTexture = new WebCamTexture(WebCamTexture.devices[sel].name, 640, 480, 60);
        webCamTexture.Play();
        isTracking = true;
        tickRoutine = StartCoroutine(RunTick());
    }

    public void StopTracking()
    {
        isTracking = false;
        if(tickRoutine != null) StopCoroutine(tickRoutine);
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            webCamTexture = null;
        }
    }

    void Update()
    {
        Tick();
    }

    IEnumerator RunTick()
    {
        var wait = new WaitForSeconds(1f / 60f);
        while (true)
        {
            //Tick();
            yield return wait;
        }
    }
    
    void Tick()
    {
        if (!isTracking || webCamTexture == null || !webCamTexture.didUpdateThisFrame) return;
        
        if (tex == null || tex.width != webCamTexture.width)
        {
            tex = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            pixelData = new Color32[webCamTexture.width * webCamTexture.height];
        }
        
        ProcessFrame();
        DetectAndEstimate();
        UpdatePreviewTexture();
    }

    void ProcessFrame()
    {
        webCamTexture.GetPixels32(pixelData);
        using (Mat tempRGBA = new Mat(webCamTexture.height, webCamTexture.width, MatType.CV_8UC4, pixelData))
        {
            Cv2.CvtColor(tempRGBA, frame, ColorConversionCodes.RGBA2BGR);
            Cv2.Flip(frame, frame, FlipMode.X); 
        }
    }

    void DetectAndEstimate()
    {
        Point2f[][] corners;
        int[] ids;
        
        CvAruco.DetectMarkers(frame, dictionary, out corners, out ids, detectorParams, out _);
        
        foreach (var t in ArUcoRegistry.All) t.tracked = false;

        if (ids == null || ids.Length == 0) return;
        
        using Mat k = new Mat(3, 3, MatType.CV_64F, new double[] {
            webCamTexture.width, 0, webCamTexture.width / 2.0,
            0, webCamTexture.width, webCamTexture.height / 2.0,
            0, 0, 1
        });

        using Mat d = new Mat(1, 5, MatType.CV_64F, 0);
        using Mat rvecs = new Mat();
        using Mat tvecs = new Mat(); 

        CvAruco.EstimatePoseSingleMarkers(corners, tagSizeMeters, k, d, rvecs, tvecs);

        if (drawBoxes) CvAruco.DrawDetectedMarkers(frame, corners, ids);
        
        Dictionary<Transform, TrackingRecord> localRecords = new Dictionary<Transform, TrackingRecord>();
        
        foreach (var arUcoTarget in ArUcoRegistry.All)
        {
            Debug.Log("has target: " + arUcoTarget.transform.gameObject.name + " with id: " + arUcoTarget.markerId);
        }
        
        for (int i = 0; i < ids.Length; i++)
        {
            Vec3d rvecV3 = rvecs.Get<Vec3d>(i);
            Vec3d tvecV3 = tvecs.Get<Vec3d>(i);
            
            using (Mat rvecMat = new Mat(rvecs, new OpenCvSharp.Range(i, i + 1), OpenCvSharp.Range.All))
            using (Mat tvecMat = new Mat(tvecs, new OpenCvSharp.Range(i, i + 1), OpenCvSharp.Range.All))
            {
                if (drawAxes) 
                    Cv2.DrawFrameAxes(frame, k, d, rvecMat, tvecMat, tagSizeMeters * 0.5f);
            }

            int id = ids[i];
            Debug.Log("ID ------");
            if (ArUcoRegistry.TryGet(id, out var target))
            {   
                Debug.Log("ID " + target.markerId);
                var p = PoseFromOpenCv(transform.localToWorldMatrix, rvecV3, tvecV3);
                
                if (!filters.ContainsKey(id)) filters[id] = new PoseFilter(alphaPos, alphaRot);
                p = filters[id].Update(p);

                target.tracked = true;
                
                float dot = Vector3.Dot(p.rotation * Vector3.forward, (transform.position - p.position).normalized);

                if (!localRecords.ContainsKey(target.transform))
                    localRecords[target.transform] = new TrackingRecord();

                if (localRecords[target.transform].Target == null || localRecords[target.transform].Dot < dot)
                {
                    localRecords[target.transform].Target = target;
                    localRecords[target.transform].Pos = p.position;
                    Vector3 rotOffset = new Vector3(0, 0, 180);
                    if (target.forwardAxis == MarkerAxis.X_NEG) rotOffset = testRotate;// new Vector3(180, 0, 0);
                    localRecords[target.transform].Rot = p.rotation * Quaternion.Euler(rotOffset);
                    localRecords[target.transform].Dot = dot;
                }
            }
        }
        
        if(calibrationState != CalibrationState.Calibrating) TrackingMind.Commit(this, localRecords.Values.ToList());
        else CalibrationMind.Calibrate(this, localRecords.Values.ToList());
    }
    
    public Vector3 testRotate = new Vector3(0f, 0f, 180f);
    

    void UpdateTargetPose(int id, Mat rvec, Mat tvec)
    {
        if (!ArUcoRegistry.TryGet(id, out var target)) return;
        
        var pose = PoseFromOpenCv(transform, rvec, tvec);

        if (!filters.TryGetValue(id, out var f))
            filters[id] = f = new PoseFilter(alphaPos, alphaRot);

        var smoothed = f.Update(pose);
        target.ApplyPose(smoothed.position, smoothed.rotation);
    }
    
    
    static Pose PoseFromOpenCv(Matrix4x4 camLocalToWorld, Vec3d rvec, Vec3d tvec)
    {
        using var r = new Mat(3, 1, MatType.CV_64F);
        r.Set(0, 0, rvec.Item0);
        r.Set(1, 0, rvec.Item1);
        r.Set(2, 0, rvec.Item2);

        using var Rm = new Mat();
        Cv2.Rodrigues(r, Rm);

        var R = MatToMatrix4x4(Rm);
        var S = Matrix4x4.Scale(new Vector3(1, -1, 1));
        var Ru = S * R * S;

        var tCv = new Vector3((float)tvec.Item0, (float)tvec.Item1, (float)tvec.Item2);
        var tUnity = S.MultiplyPoint3x4(tCv);

        var local = Matrix4x4.TRS(tUnity, QuaternionFromMatrix(Ru), Vector3.one);
        var world = camLocalToWorld * local;

        return new Pose(world.GetColumn(3), QuaternionFromMatrix(world));
    }
    
    static Matrix4x4 MatToMatrix4x4(Mat m)
    {
        var M = Matrix4x4.identity;
        M.m00 = (float)m.Get<double>(0, 0);
        M.m01 = (float)m.Get<double>(0, 1);
        M.m02 = (float)m.Get<double>(0, 2);
        M.m10 = (float)m.Get<double>(1, 0);
        M.m11 = (float)m.Get<double>(1, 1);
        M.m12 = (float)m.Get<double>(1, 2);
        M.m20 = (float)m.Get<double>(2, 0);
        M.m21 = (float)m.Get<double>(2, 1);
        M.m22 = (float)m.Get<double>(2, 2);
        return M;
    }

    static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        => Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));

    static Pose PoseFromOpenCv(Transform camTf, Mat rvec, Mat tvec)
    {
        using Mat rm = new Mat();
        Cv2.Rodrigues(rvec, rm);

        Matrix4x4 m = Matrix4x4.identity;
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                m[r, c] = (float)rm.At<double>(r, c);
        
        Matrix4x4 changeBasis = Matrix4x4.Scale(new UnityEngine.Vector3(1, -1, 1));
        m = changeBasis * m * changeBasis;
        
        Vector3 pos = new Vector3(
            (float)tvec.At<double>(0, 0), 
            -(float)tvec.At<double>(0, 1), 
            (float)tvec.At<double>(0, 2)
        );
        
        Quaternion rot = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));

        return new Pose(camTf.TransformPoint(pos), camTf.rotation * rot);
    }

    void UpdatePreviewTexture()
    {
        using var rgba = new Mat();
        Cv2.CvtColor(frame, rgba, ColorConversionCodes.BGR2RGBA);
        tex.LoadRawTextureData(rgba.Data, (int)(rgba.Total() * rgba.ElemSize()));
        tex.Apply();
    }
}

public class PoseFilter {
    public float aP, aR;
    public Pose lastPose;
    private bool initialized = false;

    public PoseFilter(float ap, float ar) { aP = ap; aR = ar; }

    public Pose Update(Pose p) {
        if (!initialized) { lastPose = p; initialized = true; return p; }
        p.position = Vector3.Lerp(lastPose.position, p.position, aP);
        p.rotation = Quaternion.Slerp(lastPose.rotation, p.rotation, aR);
        lastPose = p;
        return p;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TrackingCamera))]
public class TrackingCameraEditor : Editor {
    public override void OnInspectorGUI() {
        var script = (TrackingCamera)target;
        DrawDefaultInspector();
        
        if(GUILayout.Button("Save calibration")) CalibrationMind.SaveCalibration(script);
        if (GUILayout.Button("Scan Cameras")) script.ScanCams();
        script.sel = EditorGUILayout.Popup("Camera", script.sel, script.CameraNames.ToArray());
        
        if (!script.isTracking && GUILayout.Button("START")) script.StartTracking();
        if (script.isTracking && GUILayout.Button("STOP")) script.StopTracking();
        
        if (script.tex) {
            float aspect = (float)script.tex.width / script.tex.height;
            UnityEngine.Rect r = GUILayoutUtility.GetRect(Screen.width, Screen.width / aspect);
            GUI.DrawTexture(r, script.tex, ScaleMode.ScaleToFit);
            Repaint();
        }
    }
}
#endif