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
    /// <summary>
    /// Component responsible for tracking markers. It represents on physical camera. Attach it to any game object (camera is recommended)
    /// </summary>
    [Icon("Assets/ShardMotion/Editor/Resources/icon.png")]
    [DisallowMultipleComponent]
    [AddComponentMenu("ShardMotion/TrackingCamera")]
    public class TrackingCamera : MonoBehaviour
    {
        public int sel; // used for dropdown
        public bool isTracking; // indicator
        
        public WebCamTexture webCamTexture; // webcam feed
        private Color32[] pixelData;
        public Texture2D tex;
        
        private readonly bool drawAxes = true;
        private readonly bool drawBoxes = true;
        
        public DebugView debugView = DebugView.Normal; // render mode

        private Mat frame = new Mat();
        
        private Queue<(float timestamp, List<TrackingRecord> records)> buffer  = new Queue<(float, List<TrackingRecord>)>(); // queue of tracking record waiting to be commited 
        
        private List<string> camNames = new List<string>(); // names of available cameras
        public IReadOnlyList<string> CameraNames => camNames; // exposed unmodifiable list of cam names

        public DetectorParamsData savedDetectorParams = new DetectorParamsData(); // custom serializable detector params struct, used of configuration window
        private DetectorParameters detectorParams = new DetectorParameters(); // not serializable OpenCV detector params
        
        private Dictionary dictionary; // ArUco marker Dictionary used while detecting
        
        private Coroutine tickRoutine;
        
        // Values used for position calibration
        public int calibratedValues = 0;
        public float calibratedAmountDebug = 0;
        public Vector3 calibratedPosAverage;
        public Quaternion calibratedRotAverage;
        public CalibrationState calibrationState = CalibrationState.NotCalibrated;
        
        public bool startAutomatically = true;
        public bool flipX = false; // flips the camera feed on X axis
    
        // Resolution and Fps settings
        public static readonly string[] ResolutionOptions = { "320x240", "424x240", "640x360", "640x480", "848x480", "960x540", "1280x720", "1600x896", "1920x1080", "2560x1440", "3840x2160" };
        public static readonly string[] FpsOptions  = { "5", "10", "15", "20", "24", "25", "30", "48", "60", "90", "120", "144", "240" };
    
        public int resolutionIndex = 3;
        public int fpsIndex = 8;

        public float delay = 0; // value of delay in seconds
    
        // Used for searching for saved position and rotation of camera in PlayerPrefs
        private string SavedPosKey => $"{gameObject.name}_pos";
        private string SavedRotKey => $"{gameObject.name}_rot";
    
        /// <summary>
        /// Action is invoked when camera starts capturing
        /// </summary>
        public Action OnStartedCapturing;
        
        /// <summary>
        /// Action is invoked when camera stops capturing
        /// </summary>
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
        
        /// <summary>
        /// Singular recorded position of marker in given time
        /// </summary>
        public class TrackingRecord
        {
            public ArUcoTarget Target;
            public Vector3 Pos;
            public Quaternion Rot;
            public float Dot;
        }

        void OnEnable()
        {
            ScanCams();
            dictionary = CvAruco.GetPredefinedDictionary(ShardMotionConfig.Dictionary); // pulls the used dictionary type from Settings
            TrackingMind.Register(this); // camera is registered as active
        }
        private void Start()
        {
            if(startAutomatically) StartTracking(sel);
            LoadPos();
        }


        void OnDisable()
        {
            StopTracking();
            if (frame != null && !frame.IsDisposed) frame.Dispose();
            TrackingMind.Unregister(this); // camera is unregistered
        }

        /// <summary>
        /// Saves the camera position to <see cref="PlayerPrefs"/>
        /// </summary>
        public void SavePos()
        {
            // Saves the position of rotation to PlayerPrefs using key for this cam
            PlayerPrefs.SetFloat(SavedPosKey + "_x", transform.position.x);
            PlayerPrefs.SetFloat(SavedPosKey + "_y", transform.position.y);
            PlayerPrefs.SetFloat(SavedPosKey + "_z", transform.position.z);

            PlayerPrefs.SetFloat(SavedRotKey + "_x", transform.rotation.x);
            PlayerPrefs.SetFloat(SavedRotKey + "_y", transform.rotation.y);
            PlayerPrefs.SetFloat(SavedRotKey + "_z", transform.rotation.z);
            PlayerPrefs.SetFloat(SavedRotKey + "_w", transform.rotation.w);
        
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads the camera position from <see cref="PlayerPrefs"/>
        /// </summary>
        public void LoadPos()
        {
            if(!PlayerPrefs.HasKey(SavedPosKey + "_x")) return; // if there is no saved position return
        
            // Searches PlayerPrefs using key for this cam
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
        
            // sets the position and rotation
            transform.position = pos;
            transform.rotation = rot;
        }

        /// <summary>
        /// Loads the camera matrix calibration from PlayerPrefs
        /// </summary>
        /// <returns> Camera matrix <see cref="Mat"/> </returns>
        Mat GetCameraMatrix()
        {
            string cameraName = WebCamTexture.devices[sel].name;
            if (PlayerPrefs.GetInt(cameraName + "_calibrated", 0) == 0)
            {
                // Camera has not been calibrated, using default fallback
                Debug.LogWarning($"No Distortion camera matrix detected for {cameraName}, using default");
                return new Mat(3, 3, MatType.CV_64F, new double[] {
                    webCamTexture.width, 0, webCamTexture.width / 2.0,
                    0, webCamTexture.width, webCamTexture.height / 2.0,
                    0, 0, 1
                });
            }
            // Data is pulled from PlayerPrefs and assembled to Matrix
            return new Mat(3, 3, MatType.CV_64F, new double[] {
                PlayerPrefs.GetFloat(cameraName + "_fx"), PlayerPrefs.GetFloat(cameraName + "_gamma"), PlayerPrefs.GetFloat(cameraName + "_cx"),
                0,                                            PlayerPrefs.GetFloat(cameraName + "_fy"),    PlayerPrefs.GetFloat(cameraName + "_cy"),
                0,                                            0,                                               1
            });
        }

        /// <summary>
        /// Loads distortion coeffs from PlayerPrefs
        /// </summary>
        /// <returns>Distortion coeffs <see cref="Mat"/></returns>
        Mat GetDistCoeffs()
        {
            string cameraName = WebCamTexture.devices[sel].name;
            if (PlayerPrefs.GetInt(cameraName + "_calibrated", 0) == 0)
            {
                // no distortion coeffs found, using default fallback
                Debug.LogWarning($"No Distortion coefficients detected for {cameraName}, using default");
                return new Mat(1, 5, MatType.CV_64F, new Scalar(0));
            }

            // distortion coeffs are pulled from PlayerPrefs and assembled to Matrix
            return new Mat(1, 5, MatType.CV_64F, new double[] {
                PlayerPrefs.GetFloat(cameraName + "_k1"),
                PlayerPrefs.GetFloat(cameraName + "_k2"),
                PlayerPrefs.GetFloat(cameraName + "_p1"),
                PlayerPrefs.GetFloat(cameraName + "_p2"),
                PlayerPrefs.GetFloat(cameraName + "_k3")
            });
        }

        /// <summary>
        /// Refreshes the list of available cameras
        /// </summary>
        public void ScanCams()
        {
            camNames.Clear();
            foreach (var device in WebCamTexture.devices)
            {
                camNames.Add(device.name);
            }
            sel = Mathf.Clamp(sel, 0, Mathf.Max(0, camNames.Count - 1));
        }

        /// <summary>
        /// Starts tracking with specified camera
        /// </summary>
        /// <param name="selector">Index of physical camera that should begin tracking, the index is index of the camera  in <see cref="WebCamTexture.devices"/> </param>
        public void StartTracking(int selector)
        {
            StopTracking(); // prevents starting multiple trackings
            sel = selector; // called seelctor is used as global selector
            if (WebCamTexture.devices.Length == 0) return; 
            
            var resParts = ResolutionOptions[resolutionIndex].Split('x'); // splits the W x H selected resolution to W and H
            int w = int.Parse(resParts[0]);
            int h = int.Parse(resParts[1]);
            
            int fps  = int.Parse(FpsOptions[fpsIndex]); // parses the fps from selected
            
            // Starts the webcam feed using parameters
            webCamTexture = new WebCamTexture(WebCamTexture.devices[sel].name, w, h, fps);
            webCamTexture.Play();

            detectorParams = savedDetectorParams.ToDetectorParameters(); // detector parames are converted from custom serializable struct to OpenCV
            
            isTracking = true;
            //tickRoutine = StartCoroutine(RunTick());
            OnStartedCapturing?.Invoke(); // invoked the action
        }

        /// <summary>
        /// Stops the current tracking
        /// </summary>
        public void StopTracking()
        {
            isTracking = false;
            if(tickRoutine != null) StopCoroutine(tickRoutine);
            // stops webcam feed
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                webCamTexture = null;
            }
            OnStopedCapturing?.Invoke(); // invoked the action
        }

        void Update()
        {
            Tick(); // tick is called every frame
        }

        /// <summary>
        /// can be used instead of Update tick (helps performance)
        /// </summary>
        /// <returns></returns>
        IEnumerator RunTick()
        {
            // tick is called in steady 60 fps
            var wait = new WaitForSeconds(1f / 60f);
            while (true)
            {
                Tick();
                yield return wait;
            }
        }
        
        /// <summary>
        /// Tick containing the majority of computation, called for each processed frame
        /// </summary>
        void Tick()
        {
            if (!isTracking || webCamTexture == null || !webCamTexture.didUpdateThisFrame) return; // checks the webcam feed is ok
        
            // created new tex and pixel data reference, if they dont exist or dont match the webcam feed
            if (tex == null || tex.width != webCamTexture.width)
            {
                tex = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
                pixelData = new Color32[webCamTexture.width * webCamTexture.height];
            }
            
            ProcessFrame(); // prepares data for OpenCV
            DetectAndEstimate(); // OpenCV logic
            TryCommit(); // Tries to give data to Tracking Mind
            UpdatePreviewTexture();
        }

        /// <summary>
        /// preparing picture data for OpenCV
        /// </summary>
        void ProcessFrame()
        {
            webCamTexture.GetPixels32(pixelData);
            using (Mat tempRGBA = new Mat(webCamTexture.height, webCamTexture.width, MatType.CV_8UC4, pixelData))
            {
                Cv2.CvtColor(tempRGBA, frame, ColorConversionCodes.RGBA2BGR);
                Cv2.Flip(frame, frame, FlipMode.X); 
            }
        }

        /// <summary>
        /// Main marker detection method, called every tick
        /// </summary>
        void DetectAndEstimate()
        {
            Point2f[][] corners;
            int[] ids;
            CvAruco.DetectMarkers(frame, dictionary, out corners, out ids, detectorParams, out _); // finds corners of markers and ids of given markers in picture
    
            foreach (var t in ArUcoRegistry.All) t.tracked = false; // all markers should be flagged as not being tracked before being tracked in this frame
            if (ids == null || ids.Length == 0) return;
            
            using Mat k = GetCameraMatrix();
            using Mat d = GetDistCoeffs();

            if (drawBoxes) CvAruco.DrawDetectedMarkers(frame, corners, ids);
    
            List<TrackingRecord> records = new List<TrackingRecord>(); // list of records that will be filled with markers found and their poses
            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                if (!ArUcoRegistry.TryGet(id, out var target)) continue; // finds specific ArucoTarget reference for Id in ArUcoRegistry, skips the marker if there is no target for this id
        
                float half = target.markerSize * 0.5f; // marker size is taken from the specific found ArUcoTarget
                
                using var rvec = new Mat(); // rotation matrix
                using var tvec = new Mat(); // translation matrix
        
                // position of corners on marker (based on markers size)
                var markerPoints = new Point3f[]
                {
                    new Point3f(-half,  half, 0),
                    new Point3f( half,  half, 0),
                    new Point3f( half, -half, 0),
                    new Point3f(-half, -half, 0)
                };

                using var markerPointsMat = InputArray.Create(markerPoints);
                using var imgPointsMat = InputArray.Create(corners[i]);

                // solves the Perspective-n-Point for marker
                Cv2.SolvePnP(markerPointsMat, imgPointsMat, k, d, rvec, tvec);
        
                if (drawAxes) Cv2.DrawFrameAxes(frame, k, d, rvec, tvec, target.markerSize * 0.5f);
        
                //var rvecV3 = new Vec3d(rvec.Get<double>(0,0), rvec.Get<double>(1,0), rvec.Get<double>(2,0));
                //var tvecV3 = new Vec3d(tvec.Get<double>(0,0), tvec.Get<double>(1,0), tvec.Get<double>(2,0));
        
                //var p = PoseFromOpenCv(transform.localToWorldMatrix, rvecV3, tvecV3);
                var p = PoseFromOpenCv(transform.localToWorldMatrix, rvec, tvec);
        
                /*
                if (!filters.ContainsKey(id)) 
                    filters[id] = new PoseFilter(ShardMotionConfig.PositionSmoothing, ShardMotionConfig.RotationSmoothing);
                p = filters[id].Update(p);
                */
        
                target.tracked = true; // target is set as being tracked
        
                // calculate dot to camera 
                float dot = Vector3.Dot(p.rotation * Vector3.forward, (transform.position - p.position).normalized);
                
                records.Add(new TrackingRecord { Target = target, Pos = p.position, Rot = p.rotation, Dot = dot }); // record is added to list of markers solved this frame
            }

            if (calibrationState != CalibrationState.Calibrating)
            {
                buffer.Enqueue((Time.realtimeSinceStartup + delay, records)); // queue record for commiting
                //TryCommit();
            }
            else CalibrationMind.Calibrate(this, records); // data is used in calibration
        }

        void TryCommit()
        {
            while (buffer.Count > 0 && buffer.Peek().timestamp <= Time.realtimeSinceStartup)
            {
                var (_, records) = buffer.Dequeue();
                TrackingMind.Commit(this, records);
            }
        }
    
        /// <summary>
        /// Converts Pose from OpenCV coordinates to Unity world coordinates
        /// </summary>
        /// <param name="camLocalToWorld">Matrix representing the local to world transform of camera <see cref="Transform.localToWorldMatrix"/></param>
        /// <param name="rvec">rotation vector returned by OpenCV</param>
        /// <param name="tvec">translation vector returned by OpenCV</param>
        /// <returns>Marker pose in Unity world coordinates</returns>
        Pose PoseFromOpenCv(Matrix4x4 camLocalToWorld, Mat rvec, Mat tvec)
        {
            using var rodrigues = new Mat();
            Cv2.Rodrigues(rvec, rodrigues); // rotation is translated to rodrigues representation

            var r = MatToMatrix4x4(rodrigues);
            
            // rotation converted from OpenCV to Unity
            var s = Matrix4x4.Scale(new Vector3(flipX ? -1 : 1, -1, 1));  
            var rotationUnity = s * r * s;

            // position converted from OpenCV to Unity
            var positionOpenCv = new Vector3(
                (float)tvec.Get<double>(0, 0), 
                (float)tvec.Get<double>(1, 0), 
                (float)tvec.Get<double>(2, 0)
            );
            var positionUnity = s.MultiplyPoint3x4(positionOpenCv);

            // pose is converted from local camera coordinates to world coordinates;
            var local = Matrix4x4.TRS(positionUnity, QuaternionFromMatrix(rotationUnity), Vector3.one);
            var world = camLocalToWorld * local;

            return new Pose(world.GetColumn(3), QuaternionFromMatrix(world));
        }

        /// <summary>
        /// Converts Pose from OpenCV coordinates to Unity world coordinates
        /// </summary>
        /// <param name="camLocalToWorld">Matrix representing the local to world transform of camera <see cref="Transform.localToWorldMatrix"/></param>
        /// <param name="rvec">rotation vector returned by OpenCV in <see cref="Vec3d"/> </param>
        /// <param name="tvec">translation vector returned by OpenCV <see cref="Vec3d"/> </param>
        /// <returns>Marker pose in Unity world coordinates</returns>
        static Pose PoseFromOpenCv(Matrix4x4 camLocalToWorld, Vec3d rvec, Vec3d tvec)
        {
            // does the same this is the variant above, but uses Vec3d instead of Matrix
            using var r = new Mat(3, 1, MatType.CV_64F);
            r.Set(0, 0, rvec.Item0);
            r.Set(1, 0, rvec.Item1);
            r.Set(2, 0, rvec.Item2);

            using var rodrigues = new Mat();
            Cv2.Rodrigues(r, rodrigues);

            var R = MatToMatrix4x4(rodrigues);
            var s = Matrix4x4.Scale(new Vector3(-1, -1, 1));
            var rotationUnity = s * R * s;

            var positionOpenCV = new Vector3((float)tvec.Item0, (float)tvec.Item1, (float)tvec.Item2);
            var positionUnity = s.MultiplyPoint3x4(positionOpenCV);

            var local = Matrix4x4.TRS(positionUnity, QuaternionFromMatrix(rotationUnity), Vector3.one);
            var world = camLocalToWorld * local;

            return new Pose(world.GetColumn(3), QuaternionFromMatrix(world));
        }
    
    
        /// <summary>
        /// Converts OpenCV matrix <see cref="Mat"/> to <see cref="Matrix4x4"/>>
        /// </summary>
        /// <param name="m"> <see cref="Mat"/></param>
        /// <returns><see cref="Matrix4x4"/></returns>
        static Matrix4x4 MatToMatrix4x4(Mat m)
        {
            // Helper function for data conversion
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

        static Quaternion QuaternionFromMatrix(Matrix4x4 m) => Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    
        void UpdatePreviewTexture()
        {
            using var display = new Mat();
            
            // renders the camera previews texture in inspector
            switch (debugView)
            {
                // Different render types for analysing marker detection
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

    /// <summary>
    /// Class used to hold pose data of objects, to smooth out the tracking
    /// </summary>
    public class PoseFilter {
        public float positionSmoothing, rotationSmoothing;
        public Pose lastPose;
        private bool initialized = false;

        public PoseFilter(float position, float rotation)
        {
            positionSmoothing = position; 
            rotationSmoothing = rotation;
        }

        /// <summary>
        /// Returns the smoothed out pose from current pose 
        /// </summary>
        /// <param name="p">current pose</param>
        /// <returns>smoothed out pose</returns>
        public Pose GetSmoothed(Pose p) {
            if (!initialized)
            {
                lastPose = p; 
                initialized = true; 
                return p;
            }
            // Interpolates between values based on smothing Settings
            p.position = Vector3.Lerp(lastPose.position, p.position, positionSmoothing);
            p.rotation = Quaternion.Slerp(lastPose.rotation, p.rotation, rotationSmoothing);
            lastPose = p;
            return p;
        }

        /// <summary>
        /// Returns the smoothed out pose from current position and rotation 
        /// </summary>
        /// <param name="position">current position</param>
        /// <param name="rotation">current rotation</param>
        /// <returns>smoothed out pose</returns>
        public Pose GetSmoothed(Vector3 position, Quaternion rotation)
        {
            if (!initialized)
            {
                lastPose = new Pose(position, rotation); 
                initialized = true; 
                return lastPose;
            }
            if (float.IsNaN(position.x) || float.IsNaN(rotation.x)) return lastPose; // if the last value was for some reason NaN the NaN would be propagated to the next value resulting in infinite NaN numbers
            
            // Interpolates between values based on smothing Settings
            Vector3 smootherPos = Vector3.Lerp(lastPose.position, position, positionSmoothing);
            Quaternion smoothedRot = Quaternion.Slerp(lastPose.rotation, rotation, rotationSmoothing);
            
            lastPose = new Pose(smootherPos, smoothedRot);
            return lastPose;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom inspector written for the componenr
    /// </summary>
    [CustomEditor(typeof(TrackingCamera))]
    public class TrackingCameraEditor : Editor
    {
        private bool showPreview = true;
        
        private Texture2D _header;

        // load the heade texture for ShardMotion
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
            
            // Draw header
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
                GUI.DrawTexture(logoRect, Header);
                GUILayout.Space(8);
            }
        
            // settings button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_SettingsIcon"), GUILayout.Width(30), GUILayout.Height(20)))
            {
                TrackingCameraSettings.Open(script);
            }
            EditorGUILayout.EndHorizontal();
        

            GUILayout.Space(4);

            // camera dropdown selection
            EditorGUILayout.BeginHorizontal();
            script.sel = EditorGUILayout.Popup("Camera", script.sel, script.CameraNames.ToArray());
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), GUILayout.Width(36), GUILayout.Height(20)))
            {
                script.ScanCams();
            }
            EditorGUILayout.EndHorizontal();
        
            GUILayout.Space(4);
        
            // parametrs section
            script.resolutionIndex = EditorGUILayout.Popup("Resolution", script.resolutionIndex, TrackingCamera.ResolutionOptions);
            script.fpsIndex = EditorGUILayout.Popup("FPS", script.fpsIndex, TrackingCamera.FpsOptions);
            script.startAutomatically = EditorGUILayout.Toggle("Start Automatically", script.startAutomatically);
            script.delay = EditorGUILayout.FloatField("Delay", script.delay);
            script.flipX = EditorGUILayout.Toggle("Flip X", script.flipX);

            GUILayout.Space(8);

            // If the application is running, draw the camera preview
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Preview");
                script.debugView = (TrackingCamera.DebugView)EditorGUILayout.EnumPopup("", script.debugView);
                GUILayout.FlexibleSpace();
                
                // turn on and off the preview
                var eyeIcon = showPreview ? EditorGUIUtility.IconContent("d_scenevis_visible_hover") : EditorGUIUtility.IconContent("d_scenevis_hidden_hover");
                if (GUILayout.Button(eyeIcon, GUILayout.Width(28), GUILayout.Height(18)))
                {
                    showPreview = !showPreview;
                }
                EditorGUILayout.EndHorizontal();
                
                // if the preview should be rendered, draw
                if (showPreview && script.tex) {
                    float aspect = (float)script.tex.width / script.tex.height;
                    Rect r = GUILayoutUtility.GetRect(Screen.width, Screen.width / aspect);
                    Matrix4x4 m = GUI.matrix;
                    GUIUtility.ScaleAroundPivot(new Vector2(1, -1), new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f));
                    GUI.DrawTexture(r, script.tex, ScaleMode.ScaleToFit);
                    GUI.matrix = m;
                    Repaint(); // forces editor repaint, without this the camera preview would refresh only if user interacts with inspector
                }
            }

            GUILayout.Space(8);

            // Open camera calibration
            if (GUILayout.Button(new GUIContent("  Camera Calibration", EditorGUIUtility.IconContent("d_SettingsIcon").image), ButtonStyle())) CamCalibEditor.Open(script);

            GUILayout.Space(8);

            // If the application is playing offer tracking control
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

        // style of the buttons
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