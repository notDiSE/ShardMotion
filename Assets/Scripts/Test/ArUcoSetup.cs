
using UnityEngine;
using OpenCvSharp.Aruco;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ArUcoSetup", menuName = "CV/ArUco Setup", order = 0)]
public class ArUcoSetup : ScriptableObject
{
    public CamCalib calibration;
    public float tagSizeMeters = 0.05f;
    public PredefinedDictionaryName dictionary = PredefinedDictionaryName.Dict6X6_250;
    public Camera sceneCamera;
    public bool autoUpdateTransforms = true;
    public List<Entry> mappings = new();
    [System.Serializable]
    public class Entry { public int markerId; public GameObject target; }
}