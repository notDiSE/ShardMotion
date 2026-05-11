using OpenCvSharp.Aruco;
using UnityEngine;

namespace ShardMotion.Settings
{
    /// <summary>
    /// Acts as global static interface for settings, holds reference to settings so there is no need to keep searching resources 
    /// </summary>
    public static class ShardMotionConfig
    {
        private static ShardMotionSettings _instance;

        public static ShardMotionSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<ShardMotionSettings>("ShardMotionGlobalSettings"); // finds referecne
                return _instance;
            }
        }
    
        // static publicaly exposed values from settings 
        public static PredefinedDictionaryName Dictionary => Instance.dictionary;
        public static float PositionSmoothing => Mathf.Clamp01(1f -Instance.positionSmoothing); // lerp is opposite
        public static float RotationSmoothing => Mathf.Clamp01(1f - Instance.rotationSmoothing); // slerp is opposite
    
    }
}