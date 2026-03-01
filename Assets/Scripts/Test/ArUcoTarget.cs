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
    public Vector3 positionOffset;

    public float gizmoMarkerSize = 0.08f;
    public float gizmoArrowLength = 0.12f;
    public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 1f);

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

    //void OnEnable() => ArUcoRegistry.Register(this);
    void OnDisable() => ArUcoRegistry.Unregister(this);
    
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
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var rot = transform.localRotation * ToForwardRotation(forwardAxis);
        if (forwardAxis == MarkerAxis.X_NEG || forwardAxis == MarkerAxis.X_POS) rot = Quaternion.Euler(new Vector3(rot.eulerAngles.x, -rot.eulerAngles.y, rot.eulerAngles.z));
        var pos = transform.localPosition - rot * positionOffset;

        var half = gizmoMarkerSize * 0.5f;

        var r = rot * Vector3.right;
        var u = rot * Vector3.up;
        var f = rot * Vector3.forward;

        var p0 = pos + (-r - u) * half;
        var p1 = pos + ( r - u) * half;
        var p2 = pos + ( r + u) * half;
        var p3 = pos + (-r + u) * half;

        UnityEditor.Handles.color = Application.isPlaying  ? (tracked ? gizmoColor : Color.grey)  : gizmoColor;
        UnityEditor.Handles.DrawAAPolyLine(3f, p0, p1, p2, p3, p0);

        UnityEditor.Handles.ArrowHandleCap(
            0,
            pos,
            Quaternion.LookRotation(f, u),
            gizmoArrowLength,
            EventType.Repaint
        );

        UnityEditor.Handles.Label(
            pos + u * (gizmoMarkerSize * 0.6f),
            $"ID: {markerId}"
        );
    }
#endif
    
}