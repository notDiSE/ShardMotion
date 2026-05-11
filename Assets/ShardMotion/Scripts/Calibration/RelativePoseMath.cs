using UnityEngine;

namespace ShardMotion.Calibration
{
    /// <summary>
    /// Used to store relative pose while position calibrating
    /// </summary>
    public readonly struct RelativePose
    {
        public readonly Vector3 LocalPositionToB;
        public readonly Quaternion LocalRotationToB;

        public RelativePose(Vector3 localPositionToB, Quaternion localRotationToB)
        {
            LocalPositionToB = localPositionToB;
            LocalRotationToB = localRotationToB;
        }
    }

    /// <summary>
    /// Math on top of relative poses
    /// </summary>
    public static class RelativePoseMath
    {
        /// <summary>
        ///  captures the current relative pose between two objects
        /// </summary>
        /// <param name="a">object A</param>
        /// <param name="b">object B</param>
        /// <returns></returns>
        public static RelativePose Capture(Transform a, Transform b) => new RelativePose( b.InverseTransformPoint(a.position), Quaternion.Inverse(b.rotation) * a.rotation);

        /// <summary>
        /// Applies captured relative pose
        /// </summary>
        /// <param name="a">Object A</param>
        /// <param name="b">object B</param>
        /// <param name="rel">relative pose</param>
        public static void Apply(Transform a, Transform b, in RelativePose rel)
        {
            a.SetPositionAndRotation(b.TransformPoint(rel.LocalPositionToB), b.rotation * rel.LocalRotationToB);
        }
    }
}