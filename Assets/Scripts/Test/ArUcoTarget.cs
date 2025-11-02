using UnityEngine;

public class ArUcoTarget : MonoBehaviour
{
    public int markerId;

    void OnEnable() => ArUcoRegistry.Register(this);
    void OnDisable() => ArUcoRegistry.Unregister(this);

    public void ApplyPose(Vector3 pos, Quaternion rot) => transform.SetPositionAndRotation(pos, rot);
}