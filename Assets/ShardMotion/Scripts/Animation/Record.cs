using System;
using System.Collections;
using System.Collections.Generic;
using OpenCvSharp.Aruco;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class Record : MonoBehaviour
{
    [HideInInspector]
    public bool recording;
    
    [HideInInspector]
    public bool playback;

    private Coroutine _routine;

    public int fps = 24;
    private float _time = 0;
    string assetPath = "Assets/tmp.anim";

    private AnimationClip _clip;
    private Animator _animator;
    
    PlayableGraph _graph;
    AnimationClipPlayable _playable;


    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    public void PlayRecording()
    {
        if (_clip == null) return;
        
        playback = true;
        
        if (_graph.IsValid())
            _graph.Destroy();

        _animator.enabled = true;

        _graph = PlayableGraph.Create("RecordedClip");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output = AnimationPlayableOutput.Create(_graph, "AnimOutput", _animator);
        _playable = AnimationClipPlayable.Create(_graph, _clip);
        _playable.SetApplyFootIK(false);
        _playable.SetApplyPlayableIK(false);

        output.SetSourcePlayable(_playable);

        _graph.Play();
    }

    public void StopPlayingRecording()
    {
        playback = false;
        if (_graph.IsValid())
        {
            _graph.Destroy();
        }

        _animator.enabled = false;
    }

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
            WriteAnim.AddKey(_clip, transform, target.transform, target.transform.localPosition, target.transform.localRotation, _time);
        }
        EditorUtility.SetDirty(_clip);
    }

    public void StopRecording()
    {
        recording = false;
        if (_routine != null) StopCoroutine(_routine);
        AssetDatabase.SaveAssets();
    }
    
    private void OnDisable()
    {
        if (_graph.IsValid())
            _graph.Destroy();
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

            if (!targetScript.playback)
            {
                if(GUILayout.Button("Playback")) targetScript.PlayRecording();
            }
            else
            {
                if(GUILayout.Button("Stop Playback")) targetScript.StopPlayingRecording();
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
