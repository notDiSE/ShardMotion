using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ControlPanel : EditorWindow
{
    [SerializeField] private readonly List<bool> expanded = new List<bool>();
    [SerializeField] private List<string> cameraIds = new List<string>();
    [SerializeField] private List<string> targetIds = new List<string>();
    [SerializeField] private CalibrationDevice calibrationDevice;

    private List<TrackingCamera> cameras = new List<TrackingCamera>();
    private List<ArUcoTarget> targets = new List<ArUcoTarget>();

    private bool calibrating = false;

    [MenuItem("Tools/ShardMotion/ControlPanel")]
    public static void ShowWindow()
    {
        var window = GetWindow<ControlPanel>();
        window.titleContent = new GUIContent("ShardMotion Control Panel");
        window.minSize = new Vector2(260, 140);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        FindCameras();
        FindTargets();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            ResolveFromIds();
            Repaint();
        }
    }

    void FindCameras()
    {
        cameras = FindObjectsByType<TrackingCamera>(FindObjectsSortMode.None).ToList();
        cameraIds = cameras.Select(c => GlobalObjectId.GetGlobalObjectIdSlow(c).ToString()).ToList();
    }

    void FindTargets()
    {
        targets = FindObjectsByType<ArUcoTarget>(FindObjectsSortMode.None).ToList();
        targetIds = targets.Select(t => GlobalObjectId.GetGlobalObjectIdSlow(t).ToString()).ToList();
    }

    void ResolveFromIds()
    {
        cameras.Clear();
        for (int i = 0; i < cameraIds.Count; i++)
        {
            if (GlobalObjectId.TryParse(cameraIds[i], out var gid))
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as TrackingCamera;
                if (obj) cameras.Add(obj);
            }
        }

        targets.Clear();
        for (int i = 0; i < targetIds.Count; i++)
        {
            if (GlobalObjectId.TryParse(targetIds[i], out var gid))
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as ArUcoTarget;
                if (obj) targets.Add(obj);
            }
        }
    }

    void StartCalibration()
    {
        foreach (TrackingCamera camera in cameras)
        {
            camera.calibrationState = TrackingCamera.CalibrationState.Calibrating;
            camera.calibratedValues = 0;
        }
        UnregisterTargets();
        calibrating = true;
        CalibrationMind.CreateTarget(calibrationDevice);
    }

    void StopCalibration()
    {
        foreach (TrackingCamera camera in cameras)
        {
            camera.calibrationState = TrackingCamera.CalibrationState.Calibrated;
        }
        RegisterTargets();
        calibrating = false;
        CalibrationMind.Cleanup();
    }

    void RegisterTargets()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t) ArUcoRegistry.Register(t);
        }
    }

    void UnregisterTargets()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t) ArUcoRegistry.Unregister(t);
        }
    }

    private void OnGUI()
    {
        ResolveFromIds();

        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), EditorStyles.toolbarButton, GUILayout.Width(28), GUILayout.Height(28)))
        {
            FindCameras();
            FindTargets();
        }
        EditorGUILayout.LabelField("Detected Cameras: " + cameras.Count);
        EditorGUILayout.LabelField("Detected Targets: " + targets.Count);
        GUILayout.EndHorizontal();

        calibrationDevice = (CalibrationDevice)EditorGUILayout.ObjectField(
            "Calibration Device",
            calibrationDevice,
            typeof(CalibrationDevice),
            false
        );

        SyncStateSizes();

        if (calibrating)
        {
            int calibratedCount = 0;
            foreach (var trackingCamera in cameras)
            {
                if (trackingCamera.calibrationState is TrackingCamera.CalibrationState.Calibrated or TrackingCamera.CalibrationState.Failed) calibratedCount++;
            }
            if(calibratedCount == cameras.Count) StopCalibration();
            Repaint();
        }

        for (int i = 0; i < cameras.Count; i++)
        {
            var trackingCamera = cameras[i];
            if (!trackingCamera) continue;

            var lineH = EditorGUIUtility.singleLineHeight;
            var dotSize = 10f;

            EditorGUILayout.BeginHorizontal();

            expanded[i] = EditorGUILayout.Foldout(expanded[i], trackingCamera.name, true);

            GUILayout.FlexibleSpace();
            GUILayout.Space(10);

            if (trackingCamera.calibrationState == TrackingCamera.CalibrationState.Calibrating) GUILayout.Label((trackingCamera.calibratedAmountDebug * 100f).ToString("0.0") + "%", GUILayout.Height(lineH));

            GUILayout.Space(6);

            var r = GUILayoutUtility.GetRect(dotSize, dotSize, GUILayout.Width(dotSize), GUILayout.Height(lineH));
            var center = new Vector2(r.x + dotSize * 0.5f, r.y + lineH * 0.5f);

            Handles.BeginGUI();
            if(trackingCamera.calibrationState == TrackingCamera.CalibrationState.Calibrated) Handles.color =  Color.green;
            else if (trackingCamera.calibrationState == TrackingCamera.CalibrationState.Failed) Handles.color =  Color.red;
            else if (trackingCamera.calibrationState == TrackingCamera.CalibrationState.Calibrating) Handles.color =  Color.yellow;
            else Handles.color =  Color.white;
            Handles.DrawSolidDisc(center, Vector3.forward, dotSize * 0.5f);
            Handles.EndGUI();

            GUILayout.Space(6);

            EditorGUILayout.EndHorizontal();

            if (expanded[i])
            {
                EditorGUI.indentLevel++;
                if (GUILayout.Button("Save Calibration"))
                {
                    CalibrationMind.SaveCalibration(trackingCamera);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(6);
            }
        }

        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (calibrating)
        {
            if (GUILayout.Button("Stop Calibration"))
            {
                StopCalibration();
            }
        }
        else
        {
            if (GUILayout.Button("Start Calibration"))
            {
                StartCalibration();
            }
        }
        GUILayout.EndHorizontal();
    }

    private void SyncStateSizes()
    {
        while (expanded.Count < cameras.Count) expanded.Add(false);
        while (expanded.Count > cameras.Count) expanded.RemoveAt(expanded.Count - 1);
    }
}