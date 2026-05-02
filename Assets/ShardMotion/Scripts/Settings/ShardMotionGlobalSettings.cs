using OpenCvSharp.Aruco;
using UnityEngine;

namespace ShardMotion.Settings
{
    [CreateAssetMenu(fileName = "ShardMotionGlobalSettings", menuName = "Scriptable Objects/ShardMotionGlobalSettings")]
    public class ShardMotionSettings : ScriptableObject
    {
        [Header("General")]
        public PredefinedDictionaryName dictionary;
    
        [Header("Smoothing")] [Range(0.01f, 1f)]
        public float positionSmoothing = 0.25f;

        [Range(0.01f, 1f)] 
        public float rotationSmoothing = 0.25f;

    }
}
