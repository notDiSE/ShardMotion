using ShardMotion;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShardMotion
{
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
            if (autoRegister) ArUcoRegistry.Register(this);
        }
        void OnDisable() => ArUcoRegistry.Unregister(this);
        
        public (Vector3 pos, Quaternion rot) CorrectedPose(Vector3 rawPos, Quaternion rawRot)
        {
            
            var rot = rawRot * Quaternion.Euler(rotationOffset);
            var pos = rawPos + rot * positionOffset;
            return (pos, rot);
        }
    
        public void ApplyPose(Vector3 rawPos, Quaternion rawRot)
        {
            var correctedRot = rawRot * Quaternion.Euler(rotationOffset);
            var correctedPos = rawPos + correctedRot * positionOffset;
    
            transform.SetPositionAndRotation(correctedPos, correctedRot);
        }
    
        public (Vector3 markerPos, Quaternion markerRot) InverseMarkerPose()
        {
            var rot = transform.rotation * Quaternion.Inverse(Quaternion.Euler(rotationOffset));
            var pos = transform.position - transform.rotation * positionOffset;
            return (pos, rot);
        }
    
    
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var (markerPos, markerRot) = InverseMarkerPose();

            var half = markerSize * 0.5f;
            var r = markerRot * Vector3.right;
            var u = markerRot * Vector3.up;
            var f = markerRot * Vector3.forward;

            var p0 = markerPos + (-r - u) * half;
            var p1 = markerPos + ( r - u) * half;
            var p2 = markerPos + ( r + u) * half;
            var p3 = markerPos + (-r + u) * half;

            UnityEditor.Handles.color = Application.isPlaying
                ? (tracked ? gizmoColor : Color.grey)
                : gizmoColor;
            UnityEditor.Handles.DrawAAPolyLine(3f, p0, p1, p2, p3, p0);

            if (drawForwardArrow)
            {
                UnityEditor.Handles.ArrowHandleCap(
                    0, markerPos,
                    Quaternion.LookRotation(f, u),
                    gizmoArrowLength,
                    EventType.Repaint
                );
            }

            if (drawUpArrow)
            {
                UnityEditor.Handles.ArrowHandleCap(
                    0, markerPos,
                    Quaternion.LookRotation(-u, f),
                    gizmoArrowLength/2,
                    EventType.Repaint
                );
            }

            UnityEditor.Handles.Label(
                markerPos,
                $"ID: {markerId}"
            );
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