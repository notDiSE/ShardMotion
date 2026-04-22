using UnityEditor;
using UnityEngine;

public class CamCalib : ScriptableObject
{
    public double[] K = new double[9];
    public double[] Dist = new double[5];

    [MenuItem("Assets/Create/CV/CamCalib")]
    static void CreateAsset()
    {
        var a = CreateInstance<CamCalib>();
        AssetDatabase.CreateAsset(a, "Assets/CamCalib.asset");
        AssetDatabase.SaveAssets();
        Selection.activeObject = a;
    }
}