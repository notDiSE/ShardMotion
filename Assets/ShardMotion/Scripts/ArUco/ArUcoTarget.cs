using ShardMotion;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShardMotion
{
    /// <summary>
    /// Component used for defining one marker on specific object, multiple instances of this script can be placed on one object.
    /// </summary>
    [Icon("Assets/ShardMotion/Editor/Resources/icon.png")]
    [AddComponentMenu("ShardMotion/ArUcoTarget")]
    public class ArUcoTarget : MonoBehaviour
    {
        public bool tracked = false;
        public int markerId;
        public float markerSize = 0.08f;
        public Vector3 rotationOffset = Vector3.zero;
        public Vector3 positionOffset;
        public bool autoRegister = false;
    
        public float gizmoArrowLength = 0.12f;
        public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 1f);
        public bool drawUpArrow = true;
        public bool drawForwardArrow = true;

        void OnEnable()
        {
            if (autoRegister) ArUcoRegistry.Register(this); // registered automatically if commanded to
        }
        void OnDisable() => ArUcoRegistry.Unregister(this); // unregister
        
        /// <summary>
        /// Gets corrected object pose from raw marker position and rotation data
        /// </summary>
        /// <param name="rawPos">position of marker</param>
        /// <param name="rawRot">rotation of marker</param>
        /// <returns>position and rotation of object</returns>
        public (Vector3 pos, Quaternion rot) CorrectedPose(Vector3 rawPos, Quaternion rawRot)
        {
            
            var rot = rawRot * Quaternion.Euler(rotationOffset);
            var pos = rawPos + rot * positionOffset; // uses roation to get the position offset correctly
            return (pos, rot);
        }
    
        public void ApplyPose(Vector3 rawPos, Quaternion rawRot)
        {
            var correctedRot = rawRot * Quaternion.Euler(rotationOffset);
            var correctedPos = rawPos + correctedRot * positionOffset;
    
            transform.SetPositionAndRotation(correctedPos, correctedRot);
        }

        private (Vector3 markerPos, Quaternion markerRot) InverseMarkerPose()
        {
            // returns inverse marker pose from the center of the object, used for visualization 
            var rot = transform.rotation * Quaternion.Inverse(Quaternion.Euler(rotationOffset));
            var pos = transform.position - transform.rotation * positionOffset;
            return (pos, rot);
        }
    
    
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var (markerPos, markerRot) = InverseMarkerPose();

            
            // marker directions
            var r = markerRot * Vector3.right;
            var u = markerRot * Vector3.up;
            var f = markerRot * Vector3.forward;

            // Get marker corners
            var half = markerSize * 0.5f;
            var p0 = markerPos + (-r - u) * half;
            var p1 = markerPos + ( r - u) * half;
            var p2 = markerPos + ( r + u) * half;
            var p3 = markerPos + (-r + u) * half;

            // if the application is in playtime, color smybolises it being tracked
            Handles.color = Application.isPlaying ? (tracked ? gizmoColor : Color.grey) : gizmoColor;
            
            Handles.DrawAAPolyLine(3f, p0, p1, p2, p3, p0); // marker is drawn as rectangle

            if (drawForwardArrow)
            {
                // marker forward is drawn
                Handles.ArrowHandleCap(
                    0, markerPos,
                    Quaternion.LookRotation(f, u),
                    gizmoArrowLength,
                    EventType.Repaint
                );
            }

            if (drawUpArrow)
            {
                // Marker up is drawn
                Handles.ArrowHandleCap(
                    0, markerPos,
                    Quaternion.LookRotation(-u, f),
                    gizmoArrowLength/2,
                    EventType.Repaint
                );
            }

            // Id is drawn on top of marker
            Handles.Label(markerPos, $"ID: {markerId}");
        }
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ArUcoTarget))]
public class ArUcoTargetEditor : Editor
{
    ArUcoTarget targetScript;
    bool gizmoFoldout = false;

    public void Awake()
    {
        targetScript = (ArUcoTarget)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        Rect lineRect = GUILayoutUtility.GetRect(0, 3, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(lineRect, targetScript.gizmoColor);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("tracked"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("markerId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("markerSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("positionOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRegister"));

        EditorGUILayout.Space();
        gizmoFoldout = EditorGUILayout.Foldout(gizmoFoldout, "Gizmo Settings", true);
        if (gizmoFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoArrowLength"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("drawUpArrow"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("drawForwardArrow"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif