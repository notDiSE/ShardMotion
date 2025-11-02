using System;
using UnityEditor;
using UnityEngine;

public class TestWriteToAnim : MonoBehaviour
{
    public Transform root;
    public Transform target;
#if UNITY_EDITOR
    public AnimationClip clip;
    public string assetPath = "Assets/GeneratedClip.anim";
    public bool loop;
    public float time;

    void Update()
    {
        time += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.C))
        {
            CreateAnimationClip();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTimeline();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Keyframe();
        }
    }

    public void ResetTimeline()
    {
        time = 0;
    }

    public void Keyframe()
    {
        if (!clip) return;
        var lp = target.localPosition;
        var le = target.localEulerAngles;
        Debug.Log(lp.x + ", " + le.x + ", " + le.y);
        WriteAnim.AddKey(clip, root, target, lp, le, time);
        AssetDatabase.SaveAssets();
    }

    public void CreateAnimationClip()
    {
        if (!clip)
            clip = WriteAnim.CreateClipAsset(assetPath, loop);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TestWriteToAnim))]
public class TestWriteToAnimEditor : Editor
{
    TestWriteToAnim targetScript;

    public void Awake()
    {
        targetScript = (TestWriteToAnim)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Create Animation Clip")) targetScript.CreateAnimationClip();
        if(GUILayout.Button("Reset Timeline")) targetScript.ResetTimeline();
        if(GUILayout.Button("Keyframe")) targetScript.Keyframe();
    }
}
#endif
