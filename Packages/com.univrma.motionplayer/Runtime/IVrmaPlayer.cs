using System;
using UnityEngine;

namespace UniVRMA
{
    public interface IVrmaPlayer
    {
        #region 状态查询

        int CurrentIndex { get; }
        bool IsPlaying { get; }
        int ClipCount { get; }
        float ArmRestAngle { get; set; }

        #endregion

        #region 动作控制

        void Play(int index);

        void Play(GameObject vrmaPrefab);

        void PlayByName(string clipName, bool isLoop = false);

        void Stop();
        void ResetToRestPose();

        #endregion

        #region 事件通知

        event Action<int> OnClipStarted;
        event Action<int> OnClipStopped;

        #endregion
    }
}