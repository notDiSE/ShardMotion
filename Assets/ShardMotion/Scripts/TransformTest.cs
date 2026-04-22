using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TransformTest : MonoBehaviour
{
    public Transform objectAOriginal;
    public Transform objectBOriginal;
    public Transform objectACopy;
    public Transform objectBCopy;
    
    private void Update()
    {
        RelativePose relativePose = RelativePoseMath.Capture(objectAOriginal, objectBOriginal);
        RelativePoseMath.Apply(objectACopy, objectBCopy, relativePose);
        
    }
}


