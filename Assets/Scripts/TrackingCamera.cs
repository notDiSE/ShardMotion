using System;
using System.Collections;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using System.Collections.Generic;
using System.Linq;
using Rect = UnityEngine.Rect;

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

    private Mat frame = new Mat();
    private readonly Dictionary<int, PoseFilter> filters = new Dictionary<int, PoseFilter>();
    private List<string> camNames = new List<string>();

    private DetectorParameters detectorParams;
    private Dictionary dictionary;
    private Coroutine tickRoutine;
    

    public int calibratedValues = 0;
    public float calibratedAmountDebug = 0;
    public Vector3 calibratedPosAverage;
    public Quaternion calibratedRotAverage;
    public CalibrationState calibrationState = CalibrationState.NotCalibrated;
    public bool startAutomatically = true;
    
    private string SavedPosKey => $"{gameObject.name}_pos";
    private string SavedRotKey => $"{gameObject.name}_rot";

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
        LoadPos();
    }

    void OnEnable()
    {
        ScanCams();
        detectorParams = new DetectorParameters();
        dictionary = CvAruco.GetPredefinedDictionary(ShardMotionConfig.Dictionary);
        TrackingMind.Register(this);
    }

    void OnDisable()
    {
        StopTracking();
        if (frame != null && !frame.IsDisposed) frame.Dispose();
        TrackingMind.Unregister(this);
    }

    public void SavePos()
    {
        PlayerPrefs.SetFloat(SavedPosKey + "_x", transform.position.x);
        PlayerPrefs.SetFloat(SavedPosKey + "_y", transform.position.y);
        PlayerPrefs.SetFloat(SavedPosKey + "_z", transform.position.z);

        PlayerPrefs.SetFloat(SavedRotKey + "_x", transform.rotation.x);
        PlayerPrefs.SetFloat(SavedRotKey + "_y", transform.rotation.y);
        PlayerPrefs.SetFloat(SavedRotKey + "_z", transform.rotation.z);
        PlayerPrefs.SetFloat(SavedRotKey + "_w", transform.rotation.w);
        
        PlayerPrefs.Save();
    }

    public void LoadPos()
    {
        if(!PlayerPrefs.HasKey(SavedPosKey + "_x")) return;
        
        Vector3 pos = new Vector3(
            PlayerPrefs.GetFloat(SavedPosKey + "_x"),
            PlayerPrefs.GetFloat(SavedPosKey + "_y"),
            PlayerPrefs.GetFloat(SavedPosKey + "_z")
        );
        Quaternion rot = new Quaternion(
            PlayerPrefs.GetFloat(SavedRotKey + "_x"),
            PlayerPrefs.GetFloat(SavedRotKey + "_y"),
            PlayerPrefs.GetFloat(SavedRotKey + "_z"),
            PlayerPrefs.GetFloat(SavedRotKey + "_w")
        );
        
        transform.position = pos;
        transform.rotation = rot;
    }

    Mat GetCameraMatrix()
    {
        string cameraName = WebCamTexture.devices[sel].name;
        if (PlayerPrefs.GetInt(cameraName + "_calibrated", 0) == 0)
        {
            Debug.LogWarning($"No Distortion camera matrix detected for {cameraName}, using default");
            return new Mat(3, 3, MatType.CV_64F, new double[] {
                webCamTexture.width, 0, webCamTexture.width / 2.0,
                0, webCamTexture.width, webCamTexture.height / 2.0,
                0, 0, 1
            });
        }
        return new Mat(3, 3, MatType.CV_64F, new double[] {
            PlayerPrefs.GetFloat(cameraName + "_fx"), PlayerPrefs.GetFloat(cameraName + "_gamma"), PlayerPrefs.GetFloat(cameraName + "_cx"),
            0,                                            PlayerPrefs.GetFloat(cameraName + "_fy"),    PlayerPrefs.GetFloat(cameraName + "_cy"),
            0,                                            0,                                               1
        });
    }

    Mat GetDistCoeffs()
    {
        string cameraName = WebCamTexture.devices[sel].name;
        if (PlayerPrefs.GetInt(cameraName + "_calibrated", 0) == 0)
        {
            Debug.LogWarning($"No Distortion coefficients detected for {cameraName}, using default");
            return new Mat(1, 5, MatType.CV_64F, new Scalar(0));
        }

        return new Mat(1, 5, MatType.CV_64F, new double[] {
            PlayerPrefs.GetFloat(cameraName + "_k1"),
            PlayerPrefs.GetFloat(cameraName + "_k2"),
            PlayerPrefs.GetFloat(cameraName + "_p1"),
            PlayerPrefs.GetFloat(cameraName + "_p2"),
            PlayerPrefs.GetFloat(cameraName + "_k3")
        });
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
        
        using Mat k = GetCameraMatrix();
        using Mat d = GetDistCoeffs();
        
        using Mat rvecs = new Mat();
        using Mat tvecs = new Mat(); 

        CvAruco.EstimatePoseSingleMarkers(corners, tagSizeMeters, k, d, rvecs, tvecs);

        if (drawBoxes) CvAruco.DrawDetectedMarkers(frame, corners, ids);
        
        List<TrackingRecord> records = new List<TrackingRecord>();
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
            if (ArUcoRegistry.TryGet(id, out var target))
            {   
                var p = PoseFromOpenCv(transform.localToWorldMatrix, rvecV3, tvecV3);
                
                if (!filters.ContainsKey(id)) filters[id] = new PoseFilter(ShardMotionConfig.PositionSmoothing, ShardMotionConfig.RotationSmoothing);
                p = filters[id].Update(p);

                target.tracked = true;
                
                float dot = Vector3.Dot(p.rotation * Vector3.forward, (transform.position - p.position).normalized);
                TrackingRecord record = new TrackingRecord();
                record.Target = target;
                record.Pos = p.position;
                record.Rot = p.rotation;
                record.Dot = dot;
                records.Add(record);
            
            }
        }
        
        if(calibrationState != CalibrationState.Calibrating) TrackingMind.Commit(this, records);
        else CalibrationMind.Calibrate(this, records);
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
        var S = Matrix4x4.Scale(new Vector3(-1, -1, 1));
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
public class TrackingCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var script = (TrackingCamera)target;

        GUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        script.sel = EditorGUILayout.Popup("Camera", script.sel, script.CameraNames.ToArray());
        if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), GUILayout.Width(36), GUILayout.Height(20)))
            script.ScanCams();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        if (!script.isTracking)
        {
            if (GUILayout.Button("START")) script.StartTracking();
        }
        else
        {
            if (GUILayout.Button("STOP")) script.StopTracking();
        }

        GUILayout.Space(8);

        if (script.tex) {
            float aspect = (float)script.tex.width / script.tex.height;
            Rect r = GUILayoutUtility.GetRect(Screen.width, Screen.width / aspect);
            Matrix4x4 m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(1, -1), new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f));
            GUI.DrawTexture(r, script.tex, ScaleMode.ScaleToFit);
            GUI.matrix = m;
            Repaint();
        }

        GUILayout.Space(8);

        if (GUILayout.Button(new GUIContent("  Camera Calibration", EditorGUIUtility.IconContent("d_SettingsIcon").image), CalibStyle()))
            CamCalibEditor.Open(script);

        GUILayout.Space(8);
    }

    GUIStyle CalibStyle()
    {
        var s = new GUIStyle(GUI.skin.button);
        s.fixedHeight = 60;
        s.fontSize = 14;
        s.fontStyle = FontStyle.Bold;
        s.border = new RectOffset(8, 8, 8, 8);
        return s;
    }
}
#endif