using UnityEditor;
using UnityEngine;
using OpenCvSharp;

public static class CvCheck {
    [MenuItem("Tools/OpenCV/Check Build")]
    static void Run() {
        
        Debug.Log(Cv2.GetBuildInformation());
    }
}
