using UnityEditor;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using System.Collections.Generic;

public class CamPreview : EditorWindow
{
    Texture2D tex;
    VideoCapture cap;
    Mat m = new();
    List<int> camIdx = new();
    List<string> camNames = new();
    int sel;

    float tagM = 0.05f;
    CamCalib calib;
    bool drawAxes = true;
    bool drawBoxes = true;

    PredefinedDictionaryName dictName = PredefinedDictionaryName.Dict6X6_250;
    Point2f[][] corners;
    int[] ids;
    Vec3d[] rvecs, tvecs;

    [MenuItem("Tools/CV/Cam Preview + ArUco")]
    static void Open() => GetWindow<CamPreview>("CamPreview");

    void OnEnable()
    {
        ScanCams();
        StartCap();
        EditorApplication.update += Tick;
    }

    void OnDisable()
    {
        EditorApplication.update -= Tick;
        StopCap();
        m.Dispose();
    }

    void ScanCams()
    {
        camIdx.Clear(); camNames.Clear();
        for (int i = 0; i < 10; i++) { using var t = new VideoCapture(i); if (t.IsOpened()) { camIdx.Add(i); camNames.Add($"Camera {i}"); } }
        if (camIdx.Count == 0) { camIdx.Add(0); camNames.Add("Camera 0"); }
        sel = Mathf.Clamp(sel, 0, camIdx.Count - 1);
    }

    void StartCap()
    {
        StopCap();
        cap = new VideoCapture(camIdx[sel]);
        int w = (int)(cap?.FrameWidth ?? 640);
        int h = (int)(cap?.FrameHeight ?? 480);
        Debug.Log($"Stream: {w}x{h}");
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
    }

    void StopCap() { cap?.Release(); cap = null; }

    Mat CamK()
    {
        if (calib != null) return new Mat(3, 3, MatType.CV_64F, calib.K).Clone();
        var k = new Mat(3, 3, MatType.CV_64F, Scalar.All(0));
        k.Set(0, 0, (double)cap.FrameWidth);
        k.Set(1, 1, (double)cap.FrameWidth);
        k.Set(0, 2, (double)cap.FrameWidth * 0.5);
        k.Set(1, 2, (double)cap.FrameHeight * 0.5);
        k.Set(2, 2, 1.0);
        return k;
    }

    Mat Dist() => (calib != null && calib.Dist != null && calib.Dist.Length > 0)
        ? new Mat(1, calib.Dist.Length, MatType.CV_64F, calib.Dist).Clone()
        : new Mat(1, 5, MatType.CV_64F, 0);

    void Tick()
    {
        if (cap == null) return;
        if (!cap.Read(m) || m.Empty()) return;

        var dict = CvAruco.GetPredefinedDictionary(dictName);
        var parameters = new DetectorParameters();
        CvAruco.DetectMarkers(m, dict, out corners, out ids, parameters, out _);

        var K = CamK(); var D = Dist();

        if (ids != null && ids.Length > 0)
        {
            CvAruco.DrawDetectedMarkers(m, corners, ids);

            using var rM = new Mat();
            using var tM = new Mat();
            CvAruco.EstimatePoseSingleMarkers(corners, tagM, K, D, rM, tM);

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
                    Cv2.DrawFrameAxes(m, K, D, InputArray.Create(rvecs[i]), InputArray.Create(tvecs[i]), tagM * 0.5f, 2);

            if (drawBoxes)
                for (int i = 0; i < ids.Length; i++)
                    Cv2.Polylines(m, new[] { System.Array.ConvertAll(corners[i], p => (Point)p) }, true, Scalar.LimeGreen, 2);
        }

        using var rgba = m.CvtColor(ColorConversionCodes.BGR2RGBA);
        Cv2.Flip(rgba, rgba, FlipMode.X);
        tex.LoadRawTextureData(rgba.Data, rgba.Rows * rgba.Cols * 4);
        tex.Apply();
        Repaint();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        int newSel = EditorGUILayout.Popup(sel, camNames.ToArray(), GUILayout.MaxWidth(220));
        if (GUILayout.Button("Refresh", GUILayout.MaxWidth(100))) { ScanCams(); StartCap(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        calib = (CamCalib)EditorGUILayout.ObjectField("Calibration", calib, typeof(CamCalib), false);
        tagM = EditorGUILayout.FloatField("Tag size (m)", tagM);
        drawAxes = EditorGUILayout.ToggleLeft("Axes", drawAxes, GUILayout.MaxWidth(80));
        drawBoxes = EditorGUILayout.ToggleLeft("Boxes", drawBoxes, GUILayout.MaxWidth(80));
        EditorGUILayout.EndHorizontal();

        if (newSel != sel) { sel = newSel; StartCap(); }

        if (ids != null && ids.Length > 0)
        {
            var t = tvecs[0]; var r = rvecs[0];
            EditorGUILayout.LabelField($"ID {ids[0]}  T[{t.Item0:F3},{t.Item1:F3},{t.Item2:F3}]  R[{r.Item0:F3},{r.Item1:F3},{r.Item2:F3}]");
        }

        if (!tex) return;
        GUI.DrawTexture(new UnityEngine.Rect(0, 60, position.width, position.height - 60), tex, ScaleMode.ScaleToFit);
    }
}