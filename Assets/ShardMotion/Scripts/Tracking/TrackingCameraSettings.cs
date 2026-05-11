#if UNITY_EDITOR
using OpenCvSharp.Aruco;
using UnityEditor;
using UnityEngine;

namespace ShardMotion
{
    /// <summary>
    /// Camera properties settings, visual interface for DetectorParamsDat
    /// </summary>
    public class TrackingCameraSettings : EditorWindow
    {
        private TrackingCamera cameraRef;

        public static void Open(TrackingCamera camera)
        {
            var window = GetWindow<TrackingCameraSettings>("Tracking Camera Settings");
            window.cameraRef = camera;
            window.minSize = new Vector2(300, 300);
        }

        void OnGUI()
        {
            if (cameraRef == null) { Close(); return; }
            var p = cameraRef.savedDetectorParams;

            EditorGUI.BeginChangeCheck();
            
            // Draw of fields for each property in DetectorParamsData

            GUILayout.Label("Thresholding", EditorStyles.boldLabel);
            p.AdaptiveThreshWinSizeMin = EditorGUILayout.IntField("Win Size Min", p.AdaptiveThreshWinSizeMin);
            p.AdaptiveThreshWinSizeMax = EditorGUILayout.IntField("Win Size Max", p.AdaptiveThreshWinSizeMax);
            p.AdaptiveThreshWinSizeStep = EditorGUILayout.IntField("Win Size Step", p.AdaptiveThreshWinSizeStep);
            p.AdaptiveThreshConstant = EditorGUILayout.DoubleField("Thresh Constant", p.AdaptiveThreshConstant);

            GUILayout.Space(8);
            GUILayout.Label("Contour / Marker Filter", EditorStyles.boldLabel);
            p.MinMarkerPerimeterRate = EditorGUILayout.DoubleField("Min Perimeter Rate", p.MinMarkerPerimeterRate);
            p.MaxMarkerPerimeterRate = EditorGUILayout.DoubleField("Max Perimeter Rate", p.MaxMarkerPerimeterRate);
            p.PolygonalApproxAccuracyRate = EditorGUILayout.DoubleField("Polygonal Approx Rate", p.PolygonalApproxAccuracyRate);
            p.MinCornerDistanceRate = EditorGUILayout.DoubleField("Min Corner Distance", p.MinCornerDistanceRate);
            p.MinDistanceToBorder = EditorGUILayout.IntField("Min Distance To Border", p.MinDistanceToBorder);
            p.MinMarkerDistanceRate = EditorGUILayout.DoubleField("Min Marker Distance", p.MinMarkerDistanceRate);

            GUILayout.Space(8);
            GUILayout.Label("Corner Refinement", EditorStyles.boldLabel);
            p.CornerRefinementMethod = (CornerRefineMethod)EditorGUILayout.EnumPopup("Method", p.CornerRefinementMethod);
            p.CornerRefinementWinSize = EditorGUILayout.IntField("Win Size", p.CornerRefinementWinSize);
            p.CornerRefinementMaxIterations = EditorGUILayout.IntField("Max Iterations", p.CornerRefinementMaxIterations);
            p.CornerRefinementMinAccuracy = EditorGUILayout.DoubleField("Min Accuracy", p.CornerRefinementMinAccuracy);

            GUILayout.Space(8);
            GUILayout.Label("Marker Decoding", EditorStyles.boldLabel);
            p.MarkerBorderBits = EditorGUILayout.IntField("Border Bits", p.MarkerBorderBits);
            p.PerspectiveRemovePixelPerCell = EditorGUILayout.IntField("Pixels Per Cell", p.PerspectiveRemovePixelPerCell);
            p.PerspectiveRemoveIgnoredMarginPerCell = EditorGUILayout.DoubleField("Ignored Margin Rate", p.PerspectiveRemoveIgnoredMarginPerCell);
            p.MaxErroneousBitsInBorderRate = EditorGUILayout.DoubleField("Max Border Error Rate", p.MaxErroneousBitsInBorderRate);
            p.MinOtsuStdDev = EditorGUILayout.DoubleField("Min Otsu StdDev", p.MinOtsuStdDev);
            p.ErrorCorrectionRate = EditorGUILayout.DoubleField("Error Correction Rate", p.ErrorCorrectionRate);

            GUILayout.Space(8);
            GUILayout.Label("Misc", EditorStyles.boldLabel);
            p.DetectInvertedMarker = EditorGUILayout.Toggle("Detect Inverted Marker", p.DetectInvertedMarker);
            p.UseAruco3Detection = EditorGUILayout.Toggle("Use Aruco3 Detection", p.UseAruco3Detection);
            p.MinSideLengthCanonicalImg = EditorGUILayout.IntField("Min Side Length (Canonical)", p.MinSideLengthCanonicalImg);
            p.MinMarkerLengthRatioOriginalImg = EditorGUILayout.FloatField("Min Length Ratio (Original)", p.MinMarkerLengthRatioOriginalImg);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(cameraRef);

            GUILayout.Space(8);
            if (GUILayout.Button("Reset to Default"))
            {
                cameraRef.savedDetectorParams = new DetectorParamsData(); // makes new instance
                EditorUtility.SetDirty(cameraRef);
            }
        }
    }
}
#endif