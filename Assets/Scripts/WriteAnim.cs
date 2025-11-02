// Assets/Editor/AnimWrite.cs
using UnityEngine;
using UnityEditor;

public static class WriteAnim
{
    public static void AddKey(AnimationClip clip, Transform root, Transform target, Vector3 localPos, Vector3 localEuler, float time)
    {
        if (!clip || !root || !target) return;
        var path = AnimationUtility.CalculateTransformPath(target, root);

        AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localPosition.x"), time, localPos.x);
        AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localPosition.y"), time, localPos.y);
        AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localPosition.z"), time, localPos.z);

        AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"), time, localEuler.x);
        AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.y"), time, localEuler.y);
        AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"), time, localEuler.z);

        clip.EnsureQuaternionContinuity();
        EditorUtility.SetDirty(clip);
    }

    public static AnimationClip CreateClipAsset(string assetPath, bool loop = false)
    {
        var clip = new AnimationClip { legacy = false };
        if (loop)
        {
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);
        }
        AssetDatabase.CreateAsset(clip, assetPath);
        AssetDatabase.SaveAssets();
        return clip;
    }

    static void AddKey(AnimationClip clip, EditorCurveBinding binding, float time, float value)
    {
        var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
        InsertOrReplaceKey(curve, time, value);
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }

    static void InsertOrReplaceKey(AnimationCurve curve, float t, float v)
    {
        for (int i = 0; i < curve.length; i++)
        {
            if (Mathf.Approximately(curve.keys[i].time, t))
            {
                var k = curve.keys[i];
                k.value = v;
                curve.MoveKey(i, k);
                return;
            }
        }
        curve.AddKey(new Keyframe(t, v));
    }
}
