using OpenCvSharp.Aruco;
using UnityEngine;

public static class ShardMotionConfig
{
    private static ShardMotionSettings _instance;

    public static ShardMotionSettings Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<ShardMotionSettings>("ShardMotionGlobalSettings");
            return _instance;
        }
    }
    
    public static PredefinedDictionaryName Dictionary => Instance.dictionary;
    public static float PositionSmoothing => Instance.positionSmoothing;
    public static float RotationSmoothing => Instance.rotationSmoothing;
    
}