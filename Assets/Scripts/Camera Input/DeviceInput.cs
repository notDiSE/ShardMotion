using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "DeviceInputSource", menuName = "Scriptable Objects/Device Input")]
public class DeviceInput : InputSource
{
    public int deviceIndex;
}

#if UNITY_EDITOR
[CustomEditor(typeof(DeviceInput))]
public class DeviceInputEditor : Editor
{
    DeviceInput targetScript;

    public void Awake()
    {
        targetScript = (DeviceInput)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Change Device Index")) ;
    }
}
#endif
