using UnityEngine;

public class BodyDriver : MonoBehaviour
{
    public Transform chestTracker;
    public Transform chestBone;
    public Vector3 chestOffset;

    void LateUpdate()
    {
        if (chestTracker == null || chestBone == null) return;
        
        Vector3 boneToRoot = transform.position - chestBone.position;
        
        transform.position = chestTracker.position + boneToRoot + chestOffset;
        transform.rotation = chestTracker.rotation;
    }
}
