using UnityEngine;
using UniVRM10;

namespace UniVRMA.Core
{
    public class StableHipsPoseProvider : INormalizedPoseProvider
    {
        private readonly INormalizedPoseProvider _inner;
        private readonly Vector3 _fixedRawHipsPosition;

        public StableHipsPoseProvider(INormalizedPoseProvider inner, Vector3 fixedRawHipsPosition)
        {
            _inner = inner;
            _fixedRawHipsPosition = fixedRawHipsPosition;
        }

        public Quaternion GetNormalizedLocalRotation(HumanBodyBones bone, HumanBodyBones parentBone)
        {
            return _inner.GetNormalizedLocalRotation(bone, parentBone);
        }

        public Vector3 GetRawHipsPosition()
        {
            return _fixedRawHipsPosition;
        }
    }
}