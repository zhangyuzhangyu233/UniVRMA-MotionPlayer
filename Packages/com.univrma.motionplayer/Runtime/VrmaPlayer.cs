using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UniVRM10;
using UniVRMA.Core;
using UniHumanoid;

namespace UniVRMA
{
    [DisallowMultipleComponent]
    public class VrmaPlayer : MonoBehaviour, IVrmaPlayer
    {
        [Header("VRM 1.0 模型实例 (留空则自动从自身或父级获取)")]
        [SerializeField] private Vrm10Instance vrm10;

        [Header("VRMA 官方预制体列表 (.vrma)")]
        [SerializeField] private GameObject[] vrmaPrefabs;

        [Header("自然少女站姿手臂下垂外展角度 (推荐 65~75°，默认 70°)")]
        [Range(30f, 90f)]
        [SerializeField] private float armRestAngle = 70f;

        [Header("事件通知（UnityEvents")]
        [SerializeField] private UnityEvent<int> onClipStartedEvent;
        [SerializeField] private UnityEvent<int> onClipStoppedEvent;

        public event Action<int> OnClipStarted;
        public event Action<int> OnClipStopped;

        private GameObject _poolRoot;
        private readonly List<GameObject> _instances = new List<GameObject>();
        private int _currentIndex = -1;

        public int CurrentIndex => _currentIndex;
        public bool IsPlaying => _currentIndex >= 0;
        public int ClipCount => _instances.Count;
        public float ArmRestAngle
        {
            get => armRestAngle;
            set => armRestAngle = value;
        }
        private void InitializePool()
        {
            if (vrmaPrefabs == null || vrmaPrefabs.Length == 0) return;

            _poolRoot = new GameObject("[VRMA_Pool]");
            _poolRoot.transform.position = Vector3.zero;
            _poolRoot.transform.rotation = vrm10 != null ? vrm10.transform.rotation : Quaternion.identity;

            _instances.Clear();
            for (int i = 0; i < vrmaPrefabs.Length; i++)
            {
                var inst = InstantiateVrmaClip(vrmaPrefabs[i], i + 1);
                _instances.Add(inst);
            }

        }

        public void Play(int index)
        {
            if (_instances == null || index < 0 || index >= _instances.Count) return;
            var target = _instances[index];
            if (target == null) return;

            if (_currentIndex >= 0 && _currentIndex < _instances.Count && _instances[_currentIndex] != null)
            {
                var prevAnim = _instances[_currentIndex].GetComponent<Animation>();
                if (prevAnim != null) prevAnim.Stop();
                _instances[_currentIndex].SetActive(false);

                int oldIdx = _currentIndex;
                OnClipStopped?.Invoke(oldIdx);
                onClipStoppedEvent?.Invoke(oldIdx);
            }

            _currentIndex = index;
            target.SetActive(true);
        
            VrmaControlRigUtility.EnsureControlRig(vrm10);
            if (vrm10 != null && vrm10.Runtime != null && vrm10.Runtime.ControlRig != null)
            {
                vrm10.Runtime.VrmAnimation = target.GetComponent<Vrm10AnimationInstance>();
            }

            var anim = target.GetComponent<Animation>();
            if (anim != null)
            {
                anim.Rewind();
                anim.Play();
            }

            OnClipStarted?.Invoke(index);
            onClipStartedEvent?.Invoke(index);
        }

        public void Play(GameObject vrmaPrefab)
        {
            if (vrmaPrefab == null) return;

            int existingIndex = -1;
            if (vrmaPrefabs != null)
            {
                for (int i = 0; i < vrmaPrefabs.Length; i++)
                {
                    if (vrmaPrefabs[i] == vrmaPrefab)
                    {
                        existingIndex = i;
                        break;
                    }
                }
            }

            if (existingIndex >= 0)
            {
                Play(existingIndex);
            }
            else
            {
                var inst = InstantiateVrmaClip(vrmaPrefab, _instances.Count + 1);
                _instances.Add(inst);
                Play(_instances.Count - 1);
            }
        }

        public void PlayByName(string clipName, bool isLoop = false)
        {
            if (string.IsNullOrWhiteSpace(clipName) || _instances == null) return;

            for (int i = 0; i <_instances.Count; i++)
            {
                if (_instances[i] != null && _instances[i].name.IndexOf(clipName,StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Play(i);
                    return;
                }
            }

            Debug.LogWarning($"[UniVRMA] ⚠️ 未找到匹配名称的 VRMA 动作: {clipName}");
        }

        private void LateUpdate()
        {
            if (_currentIndex < 0 && vrm10 != null && vrm10.Runtime != null && vrm10.Runtime.ControlRig != null)
            {
                VrmaControlRigUtility.ApplyArmRestAngle(vrm10.Runtime.ControlRig, armRestAngle);
            }
        }

        public void Stop()
        {
            if (_currentIndex >= 0 && _currentIndex < _instances.Count && _instances[_currentIndex] != null)
            {
                var prevAnim = _instances[_currentIndex].GetComponent<Animation>();
                if (prevAnim != null) prevAnim.Stop();
                _instances[_currentIndex].SetActive(false);

                int oldIdx = _currentIndex;
                _currentIndex = -1;
                OnClipStopped?.Invoke(oldIdx);
                onClipStoppedEvent?.Invoke(oldIdx);
            }
            else
            {
                _currentIndex = -1;
            }

            if (vrm10 != null && vrm10.Runtime != null)
            {
                vrm10.Runtime.VrmAnimation = null;
                ResetToRestPose();
            } 
        }

        public void ResetToRestPose()
        {
            if (vrm10 == null || vrm10.Runtime == null) return;
            var rig = vrm10.Runtime.ControlRig;
            if (rig == null) return;

            Vrm10Retarget.EnforceTPose((rig, rig));
            VrmaControlRigUtility.ApplyArmRestAngle(rig, armRestAngle);
        }

        public void Awake()
        {
            if (vrm10 == null)
            {
                vrm10 = GetComponent<Vrm10Instance>();
                if (vrm10 == null) vrm10 = GetComponentInParent<Vrm10Instance>();
            }

            VrmaControlRigUtility.EnsureControlRig(vrm10);
        }

        private void Start()
        {
            VrmaControlRigUtility.EnsureControlRig(vrm10);
            InitializePool();
            ResetToRestPose();
        }

        private void OnDestroy()
        {
            if (_poolRoot != null) Destroy(_poolRoot);    
        }

        private GameObject InstantiateVrmaClip(GameObject prefab, int displayIndex)
        {
            if (prefab == null) return null;

            if (_poolRoot == null)
            {
                _poolRoot = new GameObject("[VRMA_Pool]");
                _poolRoot.transform.position = Vector3.zero;
                _poolRoot.transform.rotation = vrm10 != null ? vrm10.transform.rotation : Quaternion.identity;
            }

            var go = Instantiate(prefab, _poolRoot.transform);
            go.name = $"VRMA_Inst_{displayIndex}_{prefab.name}";

            var animInst = go.GetComponent<Vrm10AnimationInstance>();
            if (animInst != null)
            {
                animInst.ShowBoxMan(false);
                if (animInst.BoxMan != null) animInst.BoxMan.enabled = false;

                var humanoid = go.GetComponent<Humanoid>();
                if (humanoid == null) humanoid = go.AddComponent<Humanoid>();
                humanoid.AssignBonesFromAnimator();

                var provider = new InitRotationPoseProvider(go.transform, humanoid);
                var initalHipPos = ((INormalizedPoseProvider)provider).GetRawHipsPosition();
                var stableProvider = new StableHipsPoseProvider(provider, initalHipPos);
                animInst.ControlRig = (stableProvider, provider);
            }

            go.SetActive(false);
            return go;
        }
    }
}
