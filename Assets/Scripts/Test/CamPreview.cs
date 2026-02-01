using UnityEditor;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using System.Collections.Generic;

public class CamPreview : EditorWindow
{
    /*
    Texture2D tex;
    VideoCapture cap;
    Mat frame = new();
    readonly List<int> camIdx = new();
    readonly List<string> camNames = new();
    int sel;

    bool isTracking;
    Camera sceneCamera;
    float tagSizeMeters = 0.05f;
    bool drawAxes = true;
    bool drawBoxes = true;

    float alphaPos = 0.25f;
    float alphaRot = 0.25f;
    int delayFrames = 0;

    readonly Dictionary<int, PoseFilter> filters = new();

    Point2f[][] corners;
    int[] ids;
    Vec3d[] rvecs, tvecs;

    [MenuItem("Tools/CV/Cam Preview + ArUco Pose")]
    static void Open() => GetWindow<CamPreview>("CamPreview");

    void OnEnable() => ScanCams();
    void OnDisable() { StopTracking(); frame.Dispose(); }

    void ScanCams()
    {
        camIdx.Clear(); camNames.Clear();
        for (int i = 0; i < 10; i++)
        {
            using var t = new VideoCapture(i);
            if (t.IsOpened())
            {
                camIdx.Add(i);
                camNames.Add($"Camera {i}");
            }
        }
        if (camIdx.Count == 0) { camIdx.Add(0); camNames.Add("Camera 0"); }
        sel = Mathf.Clamp(sel, 0, camIdx.Count - 1);
    }

    void StartTracking()
    {
        StopTracking();
        cap = new VideoCapture(camIdx[sel]);
        cap.Set(VideoCaptureProperties.Exposure, -6);
        cap.Set(VideoCaptureProperties.Fps, 60);
        int w = (int)(cap?.FrameWidth ?? 640);
        int h = (int)(cap?.FrameHeight ?? 480);
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        isTracking = true;
        EditorApplication.update += Tick;
    }

    void StopTracking()
    {
        isTracking = false;
        EditorApplication.update -= Tick;
        cap?.Release();
        cap = null;
    }

    void Tick()
    {
        if (!isTracking || cap == null) return;
        if (!cap.Read(frame) || frame.Empty()) return;
        DetectAndEstimate();
        UpdateTargets();
        UpdateTexture();
        Repaint();
    }

    Mat CamK()
    {
        var k = new Mat(3, 3, MatType.CV_64F, Scalar.All(0));
        k.Set(0, 0, (double)cap.FrameWidth);
        k.Set(1, 1, (double)cap.FrameWidth);
        k.Set(0, 2, (double)cap.FrameWidth * 0.5);
        k.Set(1, 2, (double)cap.FrameHeight * 0.5);
        k.Set(2, 2, 1.0);
        return k;
    }

    Mat Dist() => new Mat(1, 5, MatType.CV_64F, 0);

    void DetectAndEstimate()
    {
        var dict = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_50);
        var parameters = new DetectorParameters
        {
            AdaptiveThreshWinSizeMin = 3,
            AdaptiveThreshWinSizeMax = 23,
            AdaptiveThreshWinSizeStep = 10,
            AdaptiveThreshConstant = 7,

            MinMarkerPerimeterRate = 0.03,
            MaxMarkerPerimeterRate = 4.0,

            PolygonalApproxAccuracyRate = 0.05,

            CornerRefinementMethod = CornerRefineMethod.Subpix,
            CornerRefinementWinSize = 5,
            CornerRefinementMaxIterations = 30,
            CornerRefinementMinAccuracy = 0.01,

            MinCornerDistanceRate = 0.05,
            MinDistanceToBorder = 3,

            PerspectiveRemoveIgnoredMarginPerCell = 0.13,
            PerspectiveRemovePixelPerCell = 8
        };
        
        CvAruco.DetectMarkers(frame, dict, out corners, out ids, parameters, out _);
        if (ids == null || ids.Length == 0) { rvecs = null; tvecs = null; return; }

        // sub-pixel refine
        using var gray = frame.CvtColor(ColorConversionCodes.BGR2GRAY);
        var win = new OpenCvSharp.Size(5, 5);
        var zero = new OpenCvSharp.Size(-1, -1);
        var term = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 30, 0.01);
        for (int i = 0; i < corners.Length; i++)
            Cv2.CornerSubPix(gray, corners[i], win, zero, term);

        var K = CamK();
        var D = Dist();
        CvAruco.DrawDetectedMarkers(frame, corners, ids);

        using var rM = new Mat();
        using var tM = new Mat();
        CvAruco.EstimatePoseSingleMarkers(corners, tagSizeMeters, K, D, rM, tM);

        int n = rM.Rows;
        rvecs = new Vec3d[n];
        tvecs = new Vec3d[n];
        for (int i = 0; i < n; i++)
        {
            rvecs[i] = rM.Get<Vec3d>(i);
            tvecs[i] = tM.Get<Vec3d>(i);
        }

        if (drawAxes)
            for (int i = 0; i < ids.Length; i++)
                Cv2.DrawFrameAxes(frame, K, D, InputArray.Create(rvecs[i]), InputArray.Create(tvecs[i]), tagSizeMeters * 0.5f, 2);

        if (drawBoxes)
            for (int i = 0; i < ids.Length; i++)
                Cv2.Polylines(frame, new[] { System.Array.ConvertAll(corners[i], p => (Point)p) }, true, Scalar.LimeGreen, 2);
    }

    void UpdateTargets()
    {
        var cam = sceneCamera ? sceneCamera : Camera.main;
        if (cam == null) return;
        foreach (var t in ArUcoRegistry.All) t.tracked = false;
        
        if (ids == null || rvecs == null || tvecs == null) return;
        
        Dictionary<Transform, TrackingRecord> records = new Dictionary<Transform, TrackingRecord>();
        
        for (int i = 0; i < ids.Length; i++)
        {
            int id = ids[i];
            if (!ArUcoRegistry.TryGet(id, out var target)) continue;

            var raw = PoseFromOpenCv(cam.transform, rvecs[i], tvecs[i]);
            if (!filters.TryGetValue(id, out var f))
            {
                f = new PoseFilter(alphaPos, alphaRot, delayFrames);
                filters[id] = f;
            }
            var p = f.Update(raw);
            target.tracked = true;
            
            float dot = Vector3.Dot(p.rotation * Vector3.forward, (cam.transform.position - p.position).normalized);
            if (records.TryGetValue(target.transform, out var record))
            {
                if (record.Dot < dot)
                {
                    record.Target = target;
                    record.Pos = p.position;
                    record.Rot = p.rotation;
                    record.Dot = dot;
                }
            }
            else
            {
                TrackingRecord newRecord = new TrackingRecord();
                newRecord.Target = target;
                newRecord.Pos = p.position;
                newRecord.Rot = p.rotation;
                newRecord.Dot = dot;
                records.Add(target.transform, newRecord);
            }
            
            //target.ApplyPose(p.position, p.rotation);
        }

        foreach (KeyValuePair<Transform,TrackingRecord> pair in records)
        {
            Debug.Log($"best marker for gameobject: {pair.Key.gameObject.name} is: {pair.Value.Target.markerId}");
            pair.Value.Apply();
        }
    }

    class TrackingRecord
    {
        public ArUcoTarget Target;
        public Vector3 Pos;
        public Quaternion Rot;
        public float Dot;

        public void Apply()
        {
            Target.ApplyPose(Pos,Rot);
        }
    }

    static Pose PoseFromOpenCv(Transform camTf, Vec3d rvec, Vec3d tvec)
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
        var world = camTf.localToWorldMatrix * local;
        return new Pose(world.GetColumn(3), QuaternionFromMatrix(world));
    }

    static Matrix4x4 MatToMatrix4x4(Mat m)
    {
        var M = Matrix4x4.identity;
        M.m00 = (float)m.Get<double>(0, 0); M.m01 = (float)m.Get<double>(0, 1); M.m02 = (float)m.Get<double>(0, 2);
        M.m10 = (float)m.Get<double>(1, 0); M.m11 = (float)m.Get<double>(1, 1); M.m12 = (float)m.Get<double>(1, 2);
        M.m20 = (float)m.Get<double>(2, 0); M.m21 = (float)m.Get<double>(2, 1); M.m22 = (float)m.Get<double>(2, 2);
        return M;
    }

    static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        => Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));

    void UpdateTexture()
    {
        using var rgba = frame.CvtColor(ColorConversionCodes.BGR2RGBA);
        Cv2.Flip(rgba, rgba, FlipMode.X);
        tex.LoadRawTextureData(rgba.Data, rgba.Rows * rgba.Cols * 4);
        tex.Apply();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        int newSel = EditorGUILayout.Popup(sel, camNames.ToArray(), GUILayout.MaxWidth(220));
        if (GUILayout.Button("Refresh", GUILayout.MaxWidth(100))) ScanCams();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        sceneCamera = (Camera)EditorGUILayout.ObjectField("Scene Camera", sceneCamera, typeof(Camera), true);
        tagSizeMeters = EditorGUILayout.FloatField("Tag Size (m)", tagSizeMeters);
        drawAxes = EditorGUILayout.ToggleLeft("Axes", drawAxes);
        drawBoxes = EditorGUILayout.ToggleLeft("Boxes", drawBoxes);

        EditorGUILayout.Space(4);
        alphaPos = EditorGUILayout.Slider("Smooth Pos", alphaPos, 0.01f, 0.9f);
        alphaRot = EditorGUILayout.Slider("Smooth Rot", alphaRot, 0.01f, 0.9f);
        delayFrames = EditorGUILayout.IntField("Delay Frames", delayFrames);

        EditorGUILayout.Space(8);
        if (!isTracking)
        {
            if (GUILayout.Button("Start Tracking")) StartTracking();
        }
        else
        {
            if (GUILayout.Button("Stop Tracking")) StopTracking();
        }

        if (newSel != sel) { sel = newSel; if (isTracking) StartTracking(); }

        if (ids != null && ids.Length > 0)
        {
            var t = tvecs[0]; var r = rvecs[0];
            EditorGUILayout.LabelField($"ID {ids[0]}  T[{t.Item0:F3},{t.Item1:F3},{t.Item2:F3}]  R[{r.Item0:F3},{r.Item1:F3},{r.Item2:F3}]");
        }

        if (tex)
            GUI.DrawTexture(new UnityEngine.Rect(0, 220, position.width, position.height - 220), tex, ScaleMode.ScaleToFit);
    }
    */
}
