using UnityEngine;

namespace ShardMotion.Examples
{
    /// <summary>
    /// Component, that fixes the position of chest, while using it for tracking the whole body
    /// </summary>
    public class BodyDriver : MonoBehaviour
    {
        /// <summary>
        /// Reference to the object with ArUcoTarget(s)
        /// </summary>
        public Transform chestTracker;
        /// <summary>
        /// Reference to the body of chest in rig
        /// </summary>
        public Transform chestBone;
        /// <summary>
        /// Chest can be positionally offset
        /// </summary>
        public Vector3 chestOffset;

        void LateUpdate()
        {
            if (chestTracker == null || chestBone == null) return;
            
            Vector3 boneToRoot = transform.position - chestBone.position; // the rig must be offset by the difference from the bone to origin
            
            transform.position = chestTracker.position + boneToRoot + chestOffset;
            transform.rotation = chestTracker.rotation;
        }
    }
    
}
