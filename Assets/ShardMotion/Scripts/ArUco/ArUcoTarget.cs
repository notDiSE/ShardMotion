using UnityEngine;
public enum MarkerAxis
{
    X_POS,
    X_NEG,
    Y_POS,
    Y_NEG,
    Z_POS,
    Z_NEG
}

public class ArUcoTarget : MonoBehaviour
{
    public bool tracked = false;
    public int markerId;
    public MarkerAxis forwardAxis = MarkerAxis.Z_POS;
    public Vector3 rotationOffset = Vector3.zero;
    public Vector3 positionOffset;

    public float gizmoMarkerSize = 0.08f;
    public float gizmoArrowLength = 0.12f;
    public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 1f);
    
    //void OnEnable() => ArUcoRegistry.Register(this);
    void OnDisable() => ArUcoRegistry.Unregister(this);

    public static Quaternion ToForwardRotation(MarkerAxis axis)
    {
        
        return axis switch
        {
            MarkerAxis.Z_POS => Quaternion.Euler(0, 0, 0),
            MarkerAxis.Z_NEG => Quaternion.Euler(0, 180, 0),

            MarkerAxis.X_POS => Quaternion.Euler(0, -90, 0),
            MarkerAxis.X_NEG => Quaternion.Euler(0, 90, 0),

            MarkerAxis.Y_POS => Quaternion.Euler(90, 0, 0),
            MarkerAxis.Y_NEG => Quaternion.Euler(-90, 0, 0),

            _ => Quaternion.identity
        };
        
    
    }
    
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

    /*
    public void ApplyPose(Vector3 pos, Quaternion rot)
    {
        var correctedRot = rot * ToForwardRotation(forwardAxis);
        Vector3 trueOffset = positionOffset;
        if (forwardAxis == MarkerAxis.Z_NEG) trueOffset *= -1;
        if (forwardAxis == MarkerAxis.X_NEG) trueOffset = new Vector3(-positionOffset.z, positionOffset.y, positionOffset.x);
        if (forwardAxis == MarkerAxis.X_POS) trueOffset = new Vector3(positionOffset.z, positionOffset.y, positionOffset.x);
        //var correctedPos = correctedRot * positionOffset;
        //var correctedPos =  trueOffset;
        var correctedPos =  pos + correctedRot * trueOffset;
        //var correctedPos =  correctedRot * trueOffset;

        transform.SetPositionAndRotation(correctedPos, correctedRot);
    }
    */
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var (markerPos, markerRot) = InverseMarkerPose();

        var half = gizmoMarkerSize * 0.5f;
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
            markerPos + u * (gizmoMarkerSize * 0.6f),
            $"ID: {markerId}"
        );
    }
#endif
}