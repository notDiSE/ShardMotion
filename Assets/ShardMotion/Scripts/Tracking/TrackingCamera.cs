using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using ShardMotion.Calibration;
using ShardMotion.Settings;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Rect = UnityEngine.Rect;

namespace ShardMotion
{
    [Icon("Assets/ShardMotion/Editor/Resources/icon.png")]
    [DisallowMultipleComponent]
    [AddComponentMenu("ShardMotion/TrackingCamera")]
    public class TrackingCamera : MonoBehaviour
    {
        [Header("Webcam Settings")] public int sel;
        public bool isTracking;
        public WebCamTexture webCamTexture;
        private Color32[] pixelData;
        public Texture2D tex;

        [Header("ArUco Settings")]
        public bool drawAxes = true;
        public bool drawBoxes = true;
        public DebugView debugView = DebugView.Normal;

        private Mat frame = new Mat();
        private Queue<(float timestamp, List<TrackingRecord> records)> buffer  = new Queue<(float, List<TrackingRecord>)>();
        
        private List<string> camNames = new List<string>();

        public DetectorParamsData savedDetectorParams = new DetectorParamsData();
        private DetectorParameters detectorParams = new DetectorParameters();
        
        private Dictionary dictionary;
        private Coroutine tickRoutine;
    

        public int calibratedValues = 0;
        public float calibratedAmountDebug = 0;
        public Vector3 calibratedPosAverage;
        public Quaternion calibratedRotAverage;
        public CalibrationState calibrationState = CalibrationState.NotCalibrated;
        public bool startAutomatically = true;
        public bool flipX = false;
    
        public static readonly string[] ResolutionOptions = { "320x240", "424x240", "640x360", "640x480", "848x480", "960x540", "1280x720", "1600x896", "1920x1080", "2560x1440", "3840x2160" };
        public static readonly string[] FpsOptions  = { "5", "10", "15", "20", "24", "25", "30", "48", "60", "90", "120", "144", "240" };
    
        public int resolutionIndex = 3;
        public int fpsIndex = 8;

        public float delay = 0;
    
        private string SavedPosKey => $"{gameObject.name}_pos";
        private string SavedRotKey => $"{gameObject.name}_rot";
    
        public Action OnStartedCapturing;
        public Action OnStopedCapturing;

        public enum CalibrationState
        {
            NotCalibrated,
            Calibrating,
            Calibrated,
            Failed
        }
        
        public enum DebugView
        {
            Normal,
            Grayscale,
            AdaptiveThreshold,
            Edges
        }

//public int 
    

        public class TrackingRecord
        {
            public ArUcoTarget Target;
            public Vector3 Pos;
            public Quaternion Rot;
            public float Dot;
        }

        private void Start()
        {
            if(startAutomatically) StartTracking(sel);
            LoadPos();
        }

        void OnEnable()
        {
            ScanCams();
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

        public void StartTracking(int selector)
        {
            StopTracking();
            sel = selector;
            if (WebCamTexture.devices.Length == 0) return;

            webCamTexture = new WebCamTexture(WebCamTexture.devices[sel].name, 640, 480, 60);
            webCamTexture.Play();

            detectorParams = savedDetectorParams.ToDetectorParameters();
            
            isTracking = true;
            tickRoutine = StartCoroutine(RunTick());
            OnStartedCapturing?.Invoke();
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
            OnStopedCapturing?.Invoke();
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
            TryCommit();
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

            if (drawBoxes) CvAruco.DrawDetectedMarkers(frame, corners, ids);
    
            List<TrackingRecord> records = new List<TrackingRecord>();
            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                if (!ArUcoRegistry.TryGet(id, out var target)) continue;
        
                float half = target.markerSize * 0.5f;
        
                using var rvec = new Mat();
                using var tvec = new Mat();
        
                var markerPoints = new Point3f[]
                {
                    new Point3f(-half,  half, 0),
                    new Point3f( half,  half, 0),
                    new Point3f( half, -half, 0),
                    new Point3f(-half, -half, 0)
                };

                using var markerPointsMat = InputArray.Create(markerPoints);
                using var imgPointsMat = InputArray.Create(corners[i]);

                Cv2.SolvePnP(
                    markerPointsMat, imgPointsMat, k, d,
                    rvec, tvec, false,
                    SolvePnPFlags.Iterative
                );
        
                if (drawAxes)
                    Cv2.DrawFrameAxes(frame, k, d, rvec, tvec, target.markerSize * 0.5f);
        
                //var rvecV3 = new Vec3d(rvec.Get<double>(0,0), rvec.Get<double>(1,0), rvec.Get<double>(2,0));
                //var tvecV3 = new Vec3d(tvec.Get<double>(0,0), tvec.Get<double>(1,0), tvec.Get<double>(2,0));
        
                //var p = PoseFromOpenCv(transform.localToWorldMatrix, rvecV3, tvecV3);
                var p = PoseFromOpenCv(transform.localToWorldMatrix, rvec, tvec);
        
                /*
                if (!filters.ContainsKey(id)) 
                    filters[id] = new PoseFilter(ShardMotionConfig.PositionSmoothing, ShardMotionConfig.RotationSmoothing);
                p = filters[id].Update(p);
                */
        
                target.tracked = true;
        
                float dot = Vector3.Dot(p.rotation * Vector3.forward, (transform.position - p.position).normalized);
                records.Add(new TrackingRecord { Target = target, Pos = p.position, Rot = p.rotation, Dot = dot });
            }

            if (calibrationState != CalibrationState.Calibrating)
            {
                buffer.Enqueue((Time.realtimeSinceStartup + delay, records));
                //TryCommit();
            }
            else CalibrationMind.Calibrate(this, records);
        }

        void TryCommit()
        {
            while (buffer.Count > 0 && buffer.Peek().timestamp <= Time.realtimeSinceStartup)
            {
                var (_, records) = buffer.Dequeue();
                TrackingMind.Commit(this, records);
            }
        }
    
        Pose PoseFromOpenCv(Matrix4x4 camLocalToWorld, Mat rvec, Mat tvec)
        {
            using var rm = new Mat();
            Cv2.Rodrigues(rvec, rm);

            var r = MatToMatrix4x4(rm);
            var s = Matrix4x4.Scale(new Vector3(flipX ? -1 : 1, -1, 1)); 
            var rotationUnity = s * r * s;

            var positionOpenCv = new Vector3(
                (float)tvec.Get<double>(0, 0), 
                (float)tvec.Get<double>(1, 0), 
                (float)tvec.Get<double>(2, 0)
            );
            var positionUnity = s.MultiplyPoint3x4(positionOpenCv);

            var local = Matrix4x4.TRS(positionUnity, QuaternionFromMatrix(rotationUnity), Vector3.one);
        
            var world = camLocalToWorld * local;

            return new Pose(world.GetColumn(3), QuaternionFromMatrix(world));
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
    
        void UpdatePreviewTexture()
        {
            using var display = new Mat();
    
            switch (debugView)
            {
                case DebugView.Grayscale:
                    using (var gray = new Mat())
                    {
                        Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                        Cv2.CvtColor(gray, display, ColorConversionCodes.GRAY2BGR);
                    }
                    break;
            
                case DebugView.AdaptiveThreshold:
                    using (var gray2 = new Mat())
                    using (var thresh = new Mat())
                    {
                        Cv2.CvtColor(frame, gray2, ColorConversionCodes.BGR2GRAY);
                        Cv2.AdaptiveThreshold(gray2, thresh, 255,
                            AdaptiveThresholdTypes.MeanC,
                            ThresholdTypes.Binary, 
                            detectorParams.AdaptiveThreshWinSizeMax,
                            detectorParams.AdaptiveThreshConstant);
                        Cv2.CvtColor(thresh, display, ColorConversionCodes.GRAY2BGR);
                    }
                    break;
            
                case DebugView.Edges:
                    using (var gray3 = new Mat())
                    using (var edges = new Mat())
                    {
                        Cv2.CvtColor(frame, gray3, ColorConversionCodes.BGR2GRAY);
                        Cv2.Canny(gray3, edges, 50, 150);
                        Cv2.CvtColor(edges, display, ColorConversionCodes.GRAY2BGR);
                    }
                    break;
            
                default:
                    frame.CopyTo(display);
                    break;
            }
    
            using var rgba = new Mat();
            Cv2.CvtColor(display, rgba, ColorConversionCodes.BGR2RGBA);
            tex.LoadRawTextureData(rgba.Data, (int)(rgba.Total() * rgba.ElemSize()));
            tex.Apply();
        }
    }

    public class PoseFilter {
        public float positionSmoothing, rotationSmoothing;
        public Pose lastPose;
        private bool initialized = false;

        public PoseFilter(float position, float rotation)
        {
            positionSmoothing = position; 
            rotationSmoothing = rotation;
        }

        public Pose Update(Pose p) {
            if (!initialized)
            {
                lastPose = p; 
                initialized = true; 
                return p;
            }
            p.position = Vector3.Lerp(lastPose.position, p.position, positionSmoothing);
            p.rotation = Quaternion.Slerp(lastPose.rotation, p.rotation, rotationSmoothing);
            lastPose = p;
            return p;
        }

        public Pose Update(Vector3 position, Quaternion rotation)
        {
            if (!initialized)
            {
                lastPose = new Pose(position, rotation); 
                initialized = true; 
                return lastPose;
            }
            if (float.IsNaN(position.x) || float.IsNaN(rotation.x)) return lastPose; 
            Vector3 smootherPos = Vector3.Lerp(lastPose.position, position, positionSmoothing);
            Quaternion smoothedRot = Quaternion.Slerp(lastPose.rotation, rotation, rotationSmoothing);
            lastPose = new Pose(smootherPos, smoothedRot);
            return lastPose;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TrackingCamera))]
    public class TrackingCameraEditor : Editor
    {
        private bool showPreview = true;
        
        private Texture2D _header;

        private Texture2D Header
        {
            get
            {
                if (_header == null)
                    _header = AssetDatabase.LoadAssetAtPath<Texture2D>(
                        "Assets/ShardMotion/Editor/Resources/header.png");
                return _header;
            }
        }
        
        public override void OnInspectorGUI()
        {
            var script = (TrackingCamera)target;
            
            if (Header != null)
            {
                float aspect = (float)Header.width / Header.height;
                float width  = EditorGUIUtility.currentViewWidth - 20f;
                float height = width / aspect;
                height = Mathf.Min(height, 80f);
                width  = height * aspect;

                Rect logoRect = GUILayoutUtility.GetRect(width, height);
                logoRect.x = (EditorGUIUtility.currentViewWidth - width) * 0.5f;
                logoRect.width = width;
                GUI.DrawTexture(logoRect, Header, ScaleMode.ScaleToFit, true);
                GUILayout.Space(8);
            }
        
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_SettingsIcon"), GUILayout.Width(30), GUILayout.Height(20)))
                TrackingCameraSettings.Open(script);
            EditorGUILayout.EndHorizontal();
        

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            script.sel = EditorGUILayout.Popup("Camera", script.sel, script.CameraNames.ToArray());
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), GUILayout.Width(36), GUILayout.Height(20)))
                script.ScanCams();
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(4);
        
            script.resolutionIndex = EditorGUILayout.Popup("Resolution", script.resolutionIndex, TrackingCamera.ResolutionOptions);
            script.fpsIndex = EditorGUILayout.Popup("FPS", script.fpsIndex, TrackingCamera.FpsOptions);
            script.startAutomatically = EditorGUILayout.Toggle("Start Automatically", script.startAutomatically);
            script.delay = EditorGUILayout.FloatField("Delay", script.delay);
            script.flipX = EditorGUILayout.Toggle("Flip X", script.flipX);

            GUILayout.Space(8);

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Preview");
                script.debugView = (TrackingCamera.DebugView)EditorGUILayout.EnumPopup("", script.debugView);
                GUILayout.FlexibleSpace();
                var eyeIcon = showPreview 
                    ? EditorGUIUtility.IconContent("d_scenevis_visible_hover") 
                    : EditorGUIUtility.IconContent("d_scenevis_hidden_hover");
                if (GUILayout.Button(eyeIcon, GUILayout.Width(28), GUILayout.Height(18)))
                    showPreview = !showPreview;
                EditorGUILayout.EndHorizontal();
                
                
                if (showPreview && script.tex) {
                    float aspect = (float)script.tex.width / script.tex.height;
                    Rect r = GUILayoutUtility.GetRect(Screen.width, Screen.width / aspect);
                    Matrix4x4 m = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(1, -1), new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f));
                    GUI.DrawTexture(r, script.tex, ScaleMode.ScaleToFit);
                    GUI.matrix = m;
                    Repaint();
                }
            }

            GUILayout.Space(8);

            if (GUILayout.Button(new GUIContent("  Camera Calibration", EditorGUIUtility.IconContent("d_SettingsIcon").image), ButtonStyle()))
                CamCalibEditor.Open(script);

            GUILayout.Space(8);

            if (EditorApplication.isPlaying)
            {
                if (!script.isTracking)
                {
                    if (GUILayout.Button(new GUIContent("  Manual START", EditorGUIUtility.IconContent("PlayButton").image), ButtonStyle())) script.StartTracking(script.sel);
                }
                else
                {
                    if (GUILayout.Button(new GUIContent("  Manual STOP", EditorGUIUtility.IconContent("PreMatQuad").image), ButtonStyle())) script.StopTracking();
                }
            }

        }

        GUIStyle ButtonStyle()
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
}