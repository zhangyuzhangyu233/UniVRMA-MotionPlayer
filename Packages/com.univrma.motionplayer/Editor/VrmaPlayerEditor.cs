using UnityEditor;
using UnityEngine;

namespace UniVRMA.Editor
{
    [CustomEditor(typeof(VrmaPlayer))]
    public class VrmaPlayerEditor : UnityEditor.Editor
    {
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            InitStyles();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.LabelField("🎮 UniVRMA 运行时动作控制台", _headerStyle);
            EditorGUILayout.Space(5);

            var player = (VrmaPlayer)target;

            if (Application.isPlaying)
            {
                // 运行态状态显示
                if (player.IsPlaying)
                {
                    EditorGUILayout.HelpBox($"▶ 正在播放动作 [Index: {player.CurrentIndex}] (总动作数: {player.ClipCount})", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"⏹ 处于待机状态 (总动作数: {player.ClipCount})", MessageType.None);
                }

                EditorGUILayout.Space(5);

                // 动作点播按钮列表
                if (player.ClipCount > 0)
                {
                    EditorGUILayout.LabelField("动作快捷点播 (Quick Play):", EditorStyles.boldLabel);
                    for (int i = 0; i < player.ClipCount; i++)
                    {
                        var isCurrent = player.IsPlaying && player.CurrentIndex == i;
                        GUI.backgroundColor = isCurrent ? new Color(0.4f, 0.8f, 1f) : Color.white;

                        string btnText = isCurrent ? $"▶ [正在播放] 动作 #{i}" : $"播放 动作 #{i}";
                        if (GUILayout.Button(btnText, GUILayout.Height(26)))
                        {
                            player.Play(i);
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("⏹ 停止播放 (Stop)", GUILayout.Height(30)))
                {
                    player.Stop();
                }

                GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
                if (GUILayout.Button("🧍 重置站姿 (Rest Pose)", GUILayout.Height(30)))
                {
                    player.ResetToRestPose();
                }

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("💡 提示：进入运行态 (Play Mode) 后，此处将激活实时动作点播、打断与姿态调试控制台。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }
    }
}
