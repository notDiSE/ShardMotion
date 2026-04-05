#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using OpenCvSharp;
using UnityEditor;
using UnityEngine;
using Rect = UnityEngine.Rect;

public class CamCalibEditor : EditorWindow
{
    private TrackingCamera trackingCamera;
    WebCamTexture webCamTexture;
    Texture2D tex;

    bool capturing = false;
    int captureMode = 0;
    readonly string[] captureModes = { "Automatic Capture", "Manual Capture" };
    private string webcamText = "";
    Coroutine autoCaptureRoutine;
    List<Texture2D> captures = new List<Texture2D>();
    private bool processed = false;
    
    int boardW = 7;
    int boardH = 5;
    private double rms;
    private Mat cameraMatrix;
    private Mat distCoeffs;

    public static void Open(TrackingCamera camera)
    {
        var window = (CamCalibEditor)EditorWindow.GetWindow(typeof(CamCalibEditor));
        window.titleContent = new GUIContent("Camera Calibration");
        window.minSize = new Vector2(860, 520);
        window.trackingCamera = camera;
        window.Show();
        window.captures.Clear();
        window.webCamTexture = new WebCamTexture(WebCamTexture.devices[window.trackingCamera.sel].name, 640, 480, 60);
        window.webCamTexture.Play();
        window.processed = false;
    }

    private void OnGUI()
    {
        float sideWidth = 260f;
        float previewWidth = position.width - sideWidth;

        if (tex != null)
        {
            float aspect = (float)tex.width / tex.height;
            float h = previewWidth / aspect;
            float y = (position.height - h) * 0.5f;
            GUI.DrawTexture(new Rect(0, y, previewWidth, h), tex, ScaleMode.ScaleToFit);
            
            GUIStyle overlayText = new GUIStyle(EditorStyles.boldLabel);
            overlayText.fontSize = 32;
            overlayText.normal.textColor = Color.white;
            overlayText.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, 0, previewWidth, position.height), webcamText, overlayText);
        }

        GUILayout.BeginArea(new Rect(previewWidth, 0, sideWidth, position.height));

        GUILayout.Space(16);
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        GUILayout.BeginVertical();

        captureMode = EditorGUILayout.Popup(captureMode, captureModes);
        GUILayout.Space(8);
        
        boardW = EditorGUILayout.IntField("Corners X", boardW);
        boardH = EditorGUILayout.IntField("Corners Y", boardH);
        
        GUILayout.Space(8);

        bool isAuto = captureMode == 0;
        string btnLabel = isAuto
            ? (capturing ? "  Stop Capture" : "  Capture")
            : "  Capture";

        GUIContent captureContent = new GUIContent(
            btnLabel,
            EditorGUIUtility.IconContent(capturing ? "d_PauseButton" : "d_Record Off").image
        );

        if (GUILayout.Button(captureContent, CaptureStyle()))
        {
            if (isAuto)
            {
                webcamText = "";
                if(autoCaptureRoutine != null) trackingCamera.StopCoroutine(autoCaptureRoutine);
                capturing = !capturing;
                if(capturing) autoCaptureRoutine = trackingCamera.StartCoroutine(AutomaticCapture());
            }
            else
            {
                TakePicture();
            }
            
        }
        GUILayout.Space(8);
        
        GUILayout.Label($"Captures: {captures.Count}/5 minimum");
        
        GUILayout.Space(8);
        
        GUIContent processContent = new GUIContent(
            "  Process",
            EditorGUIUtility.IconContent("d_Play").image
        );
        
        if (captures.Count < 5) EditorGUI.BeginDisabledGroup(true);
        if (GUILayout.Button(processContent, CaptureStyle()))
        {
            Process();
        }
        EditorGUI.EndDisabledGroup();

        /*
        if (processed)
        {
            GUILayout.Label($"reprojection error: {rms:F4}");
            GUILayout.Label($"Camera Matrix:\n{cameraMatrix.Dump()}");
            GUILayout.Label($"Dist Coeffs:\n{distCoeffs.Dump()}");
        }
        */

        GUILayout.EndVertical();
        GUILayout.Space(16);
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        GUILayout.BeginVertical();

        if (!processed) EditorGUI.BeginDisabledGroup(true);
        if (GUILayout.Button(new GUIContent("  Save", EditorGUIUtility.IconContent("d_SaveAs").image), SaveStyle()))
        {
            Close();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(8);

        if (GUILayout.Button(new GUIContent("  Discard & Close", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image), DiscardStyle()))
            TryClose();

        GUILayout.EndVertical();
        GUILayout.Space(16);
        GUILayout.EndHorizontal();

        GUILayout.Space(24);
        GUILayout.EndArea();
    }

    GUIStyle CaptureStyle()
    {
        GUIStyle s = new GUIStyle(GUI.skin.button);
        s.fixedHeight = 60;
        s.fontSize = 14;
        s.fontStyle = FontStyle.Bold;
        s.border = new RectOffset(8, 8, 8, 8);
        return s;
    }

    GUIStyle SaveStyle()
    {
        GUIStyle s = new GUIStyle(GUI.skin.button);
        s.fontSize = 14;
        s.fontStyle = FontStyle.Bold;
        s.fixedHeight = 60;
        s.border = new RectOffset(8, 8, 8, 8);
        return s;
    }

    GUIStyle DiscardStyle()
    {
        GUIStyle s = new GUIStyle(GUI.skin.button);
        s.fixedHeight = 36;
        s.border = new RectOffset(8, 8, 8, 8);
        s.normal.textColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        s.hover.textColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        return s;
    }

    void TakePicture()
    {
        Texture2D snapshot = new Texture2D(tex.width, tex.height, tex.format, false);
        Graphics.CopyTexture(tex, snapshot);
        captures.Add(snapshot);
        Debug.Log("took picture");
    }
    
    IEnumerator AutomaticCapture()
    {
        webcamText = "Starting Capture";
        yield return new WaitForSeconds(3);
        while (capturing)
        {
            webcamText = "Move";
            yield return new WaitForSeconds(2);
            webcamText = "Hold";
            yield return new WaitForSeconds(1);
            TakePicture();
        }
    }

    void TryClose()
    {
        if (EditorUtility.DisplayDialog(
            "Discard Calibration",
            "Close without saving? All unsaved calibration data will be lost.",
            "Discard",
            "Keep Working"))
        {
            Close();
        }
    }

    void Process()
    {
        if (captures.Count < 5)
        {
            EditorUtility.DisplayDialog("Calibration Failed", 
                $"Need at least 5 captures, got {captures.Count}.", "OK");
            return;
        }
        
        Size boardSize = new Size(boardW, boardH);
        float squareSizeMeters = 0.03f;
        
        var objPoints = new List<Mat>();
        var imgPoints = new List<Mat>();
        
        Mat objCorners = new Mat(boardSize.Width * boardSize.Height, 1, MatType.CV_32FC3);
        for (int row = 0; row < boardSize.Height; row++)
            for (int col = 0; col < boardSize.Width; col++)
                objCorners.Set<Vec3f>(row * boardSize.Width + col, 
                    new Vec3f(col * squareSizeMeters, row * squareSizeMeters, 0f));

        int successCount = 0;

        foreach (var cap in captures)
        {
            Color32[] pixels = cap.GetPixels32();
            using Mat rgba = new Mat(cap.height, cap.width, MatType.CV_8UC4, pixels);
            using Mat bgr = new Mat();
            Cv2.CvtColor(rgba, bgr, ColorConversionCodes.RGBA2BGR);
            //Cv2.Flip(bgr, bgr, FlipMode.X);
            using Mat gray = new Mat();
            Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

            Point2f[] corners;
            bool found = Cv2.FindChessboardCorners(gray, boardSize, out corners,
                ChessboardFlags.AdaptiveThresh | ChessboardFlags.NormalizeImage);

            if (!found)
            {
                Cv2.ImWrite(Application.temporaryCachePath + $"/calib_fail_{successCount}.bmp", gray);
                Debug.Log("Saved to: " + Application.temporaryCachePath + $"/calib_fail_{successCount}.bmp");
                //Cv2.ImWrite(Application.temporaryCachePath + $"/calib_fail_{successCount}.png", gray);
                continue;
            }
            
            var criteria = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.MaxIter, 30, 0.001);
            Cv2.CornerSubPix(gray, corners, new Size(11, 11), new Size(-1, -1), criteria);

            Mat imgCorners = new Mat(corners.Length, 1, MatType.CV_32FC2, corners);
            objPoints.Add(objCorners.Clone());
            imgPoints.Add(imgCorners);
            successCount++;
        }

        if (successCount < 5)
        {
            EditorUtility.DisplayDialog("Calibration Failed",
                $"Chessboard found in only {successCount}/{captures.Count} images. Need at least 5.", "OK");
            return;
        }
        
        Size imgSize = new Size(captures[0].width, captures[0].height);
        cameraMatrix = Mat.Eye(3, 3, MatType.CV_64F);
        distCoeffs = new Mat(1, 5, MatType.CV_64F, new Scalar(0));
        Mat[] rvecs, tvecs;

        rms = Cv2.CalibrateCamera(
            objPoints, imgPoints, imgSize,
            cameraMatrix, distCoeffs,
            out rvecs, out tvecs,
            CalibrationFlags.None
        );

        Debug.Log($"[CamCalib] RMS reprojection error: {rms:F4}");
        Debug.Log($"[CamCalib] Camera Matrix:\n{cameraMatrix.Dump()}");
        Debug.Log($"[CamCalib] Dist Coeffs: {distCoeffs.Dump()}");
        processed = true;
        
        foreach (var m in objPoints) m.Dispose();
        foreach (var m in imgPoints) m.Dispose();
        objCorners.Dispose();
        cameraMatrix.Dispose();
        distCoeffs.Dispose();
        foreach (var m in rvecs) m.Dispose();
        foreach (var m in tvecs) m.Dispose();

        /*
        string prefix = trackingCamera.gameObject.name + "_calib";
        
        
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                PlayerPrefs.SetFloat($"{prefix}_K_{r}_{c}", (float)cameraMatrix.Get<double>(r, c));

        
        for (int i = 0; i < 5; i++)
            PlayerPrefs.SetFloat($"{prefix}_D_{i}", (float)distCoeffs.Get<double>(0, i));

        PlayerPrefs.SetInt($"{prefix}_valid", 1);
        PlayerPrefs.Save();

        EditorUtility.DisplayDialog("Calibration Saved",
            $"Calibrated from {successCount} images.\nRMS error: {rms:F4}", "OK");
            
        */
    }

    private void OnEnable()
    {
        EditorApplication.update += Update;
    }

    void Update()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            tex = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            tex.SetPixels32(webCamTexture.GetPixels32());
            tex.Apply();
            Repaint();
        }
    }

    void OnDisable()
    {
        EditorApplication.update -= Update;
        webCamTexture.Stop();
        captures.Clear();
        if(autoCaptureRoutine != null) trackingCamera.StopCoroutine(autoCaptureRoutine);
    }
}
#endif