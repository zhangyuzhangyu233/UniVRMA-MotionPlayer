using System.Reflection;
using UnityEngine;
using UniVRM10;

namespace UniVRMA.Core
{
    public class VrmaControlRigUtility
    {
        private static readonly FieldInfo UseControlRigField = typeof(Vrm10Instance).GetField("m_useControlRig", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo RuntimeField = typeof(Vrm10Instance).GetField("m_runtime", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void EnsureControlRig(Vrm10Instance vrm)
        {
            if (vrm == null) return;

            UseControlRigField?.SetValue(vrm, true);

            if (vrm.Runtime != null && vrm.Runtime.ControlRig == null)
            {
                RuntimeField?.SetValue(vrm, null);
            }

            var rig = vrm.transform.Find("Runtime Control Rig");
            if (rig != null)
            {
                rig.localPosition = Vector3.zero;
                rig.localRotation = Quaternion.identity;
                rig.localScale = Vector3.one;
            }
        }

        public static void ApplyArmRestAngle(Vrm10RuntimeControlRig rig, float angle)
        {
            if (rig == null) return;

            var leftUpperArm = rig.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (leftUpperArm != null)
            {
                leftUpperArm.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }

            var rightUpperArm = rig.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (rightUpperArm != null)
            {
                rightUpperArm.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}