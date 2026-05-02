using UnityEngine;

namespace ShardMotion
{
    public class MotionPredictor
    {
        public Pose Pose { get; private set; }
        Vector3 v;
        Vector3 w;
        bool has;
        float alphaPos, alphaRot;
        float lastT;

        public MotionPredictor(float alphaPos = 0.25f, float alphaRot = 0.25f)
        {
            this.alphaPos = Mathf.Clamp01(alphaPos);
            this.alphaRot = Mathf.Clamp01(alphaRot);
        }

        public void UpdateMeasured(Pose m, float t)
        {
            if (!has)
            {
                Pose = m; v = Vector3.zero; w = Vector3.zero; has = true; lastT = t; return;
            }
            float dt = Mathf.Max(1e-4f, t - lastT);

            var dp = (m.position - Pose.position) / dt;
            v = Vector3.Lerp(v, dp, 0.5f);

            var dq = m.rotation * Quaternion.Inverse(Pose.rotation);
            dq.ToAngleAxis(out float ang, out Vector3 axis);
            if (ang > 180f) ang -= 360f;
            var av = axis * Mathf.Deg2Rad * (ang / dt);
            w = Vector3.Lerp(w, av, 0.5f);

            Pose = new Pose(
                Vector3.Lerp(Pose.position, m.position, alphaPos),
                Quaternion.Slerp(Pose.rotation, m.rotation, alphaRot)
            );

            lastT = t;
        }

        public Pose Predict(float t)
        {
            if (!has) return Pose;
            float dt = Mathf.Max(0f, t - lastT);
            var p = Pose.position + v * dt;

            float ang = w.magnitude * dt;
            Quaternion dq = ang > 1e-6f ? Quaternion.AngleAxis(ang * Mathf.Rad2Deg, w.normalized) : Quaternion.identity;
            var r = dq * Pose.rotation;

            return new Pose(p, r);
        }
    }
}