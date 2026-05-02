using UnityEngine;

namespace ShardMotion
{
    public class ArUcoTarget : MonoBehaviour
    {
        public bool tracked = false;
        public int markerId;
        public float markerSize = 0.08f;
        public Vector3 rotationOffset = Vector3.zero;
        public Vector3 positionOffset;
    
        public float gizmoArrowLength = 0.12f;
        public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 1f);
    
        //void OnEnable() => ArUcoRegistry.Register(this);
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
        void OnDrawGizmosSelected()
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

            UnityEditor.Handles.ArrowHandleCap(
                0, markerPos,
                Quaternion.LookRotation(f, u),
                gizmoArrowLength,
                EventType.Repaint
            );

            UnityEditor.Handles.Label(
                markerPos,
                $"ID: {markerId}"
            );
        }
#endif
    }
}