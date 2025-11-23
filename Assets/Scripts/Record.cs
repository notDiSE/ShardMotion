using System;
using System.Collections;
using System.Collections.Generic;
using OpenCvSharp.Aruco;
using UnityEditor;
using UnityEngine;

public class Record : MonoBehaviour
{
    [HideInInspector]
    public bool recording;

    private Coroutine _routine;

    public int fps = 24;
    private float _time = 0;
    string assetPath = "Assets/tmp.anim";

    private AnimationClip _clip;
    
    public void CreateTempClip()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);
        _clip = WriteAnim.CreateClipAsset(assetPath, true);
    }

    public void StartRecording()
    {
        if (_routine != null) StopCoroutine(_routine);
        CreateTempClip();
        recording = true;
        _time = 0;
        _routine = StartCoroutine(RecordRoutine());
    }

    private void Update()
    {
        if(recording) _time += Time.deltaTime;
    }

    IEnumerator RecordRoutine()
    {
        float delay = 1f / fps;
        while (recording)
        {
            RecordLoop();
            yield return new WaitForSeconds(delay);
        }
    }

    void RecordLoop()
    {
        foreach (ArUcoTarget target in  ArUcoRegistry.All)
        {
            if(!target.tracked) continue;
            var lp = target.transform.localPosition;
            var le = target.transform.localEulerAngles;
            WriteAnim.AddKey(_clip, transform, target.transform, lp, le, _time);
        }
        EditorUtility.SetDirty(_clip);
    }

    public void StopRecording()
    {
        recording = false;
        if (_routine != null) StopCoroutine(_routine);
        AssetDatabase.SaveAssets();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Record))]
public class RecordEditor : Editor
{
    Record targetScript;

    public void Awake()
    {
        targetScript = (Record)target;
    }

    public override void OnInspectorGUI()
    {
        if (!targetScript.recording)
        {
            if (GUILayout.Button("Record"))
            {
                targetScript.StartRecording();
            }
        }
        else
        {
            if (GUILayout.Button("Stop Recording"))
            {
                targetScript.StopRecording();
            }
        }
        base.OnInspectorGUI();
    }
}
#endif
