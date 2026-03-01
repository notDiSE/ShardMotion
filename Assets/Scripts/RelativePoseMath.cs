using UnityEngine;

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

public static class RelativePoseMath
{
    public static RelativePose Capture(Transform a, Transform b)
        => new RelativePose(
            b.InverseTransformPoint(a.position),
            Quaternion.Inverse(b.rotation) * a.rotation
        );

    public static void Apply(Transform a, Transform b, in RelativePose rel)
    {
        a.SetPositionAndRotation(
            b.TransformPoint(rel.LocalPositionToB),
            b.rotation * rel.LocalRotationToB
        );
    }
}