using OpenCvSharp.Aruco;
using UnityEngine;

namespace ShardMotion.Settings
{
    /// <summary>
    /// Holds settings data
    /// </summary>
    [CreateAssetMenu(fileName = "ShardMotionGlobalSettings", menuName = "Scriptable Objects/ShardMotionGlobalSettings")]
    public class ShardMotionSettings : ScriptableObject
    {
        [Header("General")]
        public PredefinedDictionaryName dictionary;
    
        [Header("Smoothing")] [Range(0.01f, 0.99f)]
        public float positionSmoothing = 0.75f;

        [Range(0.01f, 1f)] 
        public float rotationSmoothing = 0.75f;

    }
}
