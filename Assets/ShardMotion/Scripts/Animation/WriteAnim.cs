// Assets/Editor/AnimWrite.cs

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace ShardMotion.Animation
{
    /// <summary>
    /// Used to write to animation clip
    /// </summary>
    public static class WriteAnim
    {
        #if UNITY_EDITOR
        static Quaternion prevRot;
        static bool hasPrev;

        public static void ResetState()
        {
            hasPrev = false;
            prevRot = Quaternion.identity;
        }

        /// <summary>
        /// Add key to animation at given time
        /// </summary>
        /// <param name="clip">Animation clip, to add key to</param>
        /// <param name="root">Root Transform of the recorder</param>
        /// <param name="target">Transform to record</param>
        /// <param name="localPos">position of object</param>
        /// <param name="localRot">rotation of object</param>
        /// <param name="time"> time in animation </param>
        public static void AddKey(AnimationClip clip, Transform root, Transform target, Vector3 localPos, Quaternion localRot, float time)
        {
            if (!clip || !root || !target) return; // not valid

            var path = AnimationUtility.CalculateTransformPath(target, root); // get the path to object from root transform

            localRot = Quaternion.Normalize(localRot); // normalize rotation 

            // The rotation did full rotation (animation would do 360, should be fixed instead)
            if (hasPrev && Quaternion.Dot(prevRot, localRot) < 0f)
            {
                // quaternion gets flipped == same rotation
                localRot.x = -localRot.x;
                localRot.y = -localRot.y;
                localRot.z = -localRot.z;
                localRot.w = -localRot.w;
            }

            prevRot = localRot;
            hasPrev = true;

            // Add keys for position and rotation as curve
            AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localPosition.x"), time, localPos.x);
            AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localPosition.y"), time, localPos.y);
            AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localPosition.z"), time, localPos.z);

            AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.x"), time, localRot.x);
            AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.y"), time, localRot.y);
            AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.z"), time, localRot.z);
            AddKey(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.w"), time, localRot.w);

            EditorUtility.SetDirty(clip);
        }

        /// <summary>
        /// Creates new animation clip
        /// </summary>
        /// <param name="assetPath">Where to save</param>
        /// <param name="loop">Should the animation loop</param>
        /// <returns></returns>
        public static AnimationClip CreateClipAsset(string assetPath, bool loop = false)
        {
            var clip = new AnimationClip { legacy = false };

            if (loop)
            {
                var s = AnimationUtility.GetAnimationClipSettings(clip); 
                s.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, s);
            }
            
            // clip created as asset at path
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
            return clip;
        }

        /// <summary>
        /// Add the keyframe as curve
        /// </summary>
        /// <param name="clip">Clip to add to</param>
        /// <param name="binding">binding identifiing the property</param>
        /// <param name="time">time in animation</param>
        /// <param name="value">value in the keyframe</param>
        static void AddKey(AnimationClip clip, EditorCurveBinding binding, float time, float value)
        {
            // Recomputes the curve depending on new key
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            InsertOrReplaceKey(curve, time, value);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        /// <summary>
        ///  inserts new keyframe to curve
        /// </summary>
        /// <param name="curve">curve to add to</param>
        /// <param name="time">time in curve to add to</param>
        /// <param name="value">vlaue of keframe</param>
        static void InsertOrReplaceKey(AnimationCurve curve, float time, float value)
        {
            // for each keyframe in curve
            for (int i = 0; i < curve.length; i++)
            {
                // if the time in curve is aproximately the same as time I should add in
                if (Mathf.Approximately(curve.keys[i].time, time))
                {
                    // keyframe is already present at this time, replace
                    var k = curve.keys[i];
                    k.value = value;
                    curve.MoveKey(i, k); // only way to modify existing curve keyframe
                    return;
                }
            }
            // add new keyframe to curve
            curve.AddKey(new Keyframe(time, value));
        }
        #endif
    }
}
