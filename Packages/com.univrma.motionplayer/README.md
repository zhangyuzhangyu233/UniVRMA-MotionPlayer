# UniVRMA Motion Player

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![Version](https://img.shields.io/badge/version-0.1.0--alpha-green.svg)](package.json)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![VRM](https://img.shields.io/badge/UniVRM-1.0-red.svg)](https://github.com/vrm-c/UniVRM)

An ultra-lightweight, high-performance, and production-ready **VRMA (VRM Animation) Motion Player** for Unity and UniVRM 1.0.

专为 Unity 打造的轻量级、高性能、工业级 **VRMA (VRM 1.0 官方动画)** 动作播放器。开箱即用地解决官方原生重定向空引用（NRE）、模型跨身高 Hips 沉陷/浮空、世界坐标轴向反转、以及 T-Pose 机械僵硬等技术痛点。

---

## 🌟 Key Features / 核心特性

- ⚡ **Zero-Overhead Retargeting / 零开销运行时重定向**  
  Directly interfaces with UniVRM 1.0's native `Vrm10Retarget` pipeline via dynamic pointer switching. 0 GC allocation during clip switches.  
  直接对接 UniVRM 1.0 原生 `Vrm10Retarget` 骨骼重定向系统，通过运行时动画指针热切换，动作切换过程 **0 GC 开销**。

- 🛡️ **Stable Hips Height / 骨盆自适应防沉陷 (Decorator Pattern)**  
  Built-in `StableHipsPoseProvider` anchors physical root height to the avatar's baseline. Ensures feet stay glued to the floor regardless of character height differences (Loli, Teen, Adult).  
  内置 `StableHipsPoseProvider` 装饰器模式，精准锁定骨盆物理基准高度，彻底根治不同体型/腿长模型播放动作时的浮空与陷入地面问题。

- 🧘 **Natural Arm Rest Pose / 70° 少女自然站姿**  
  Gracefully applies a 70° natural arm drape angle during `LateUpdate` pipeline stage when idle, eliminating the robotic T-Pose.  
  管线时序精准劫持：在 `LateUpdate` 阶段将双臂自然下垂至 70° 优雅姿态，告别机械死板的 90° T-Pose。

- 🔌 **Dynamic Object Pool / 动态对象池与绝对坐标隔离**  
  Clips are instantiated into an isolated world-space `[VRMA_Pool]` to prevent root motion coordinate pollution. Supports dynamic expansion on-the-fly (`Play(GameObject prefab)`).  
  在世界原点建立独立 `[VRMA_Pool]` 对象池，杜绝目标模型 Transform 对根骨骼位移的污染；支持运行时传入未配置的预制体动态热插拔。

- 🎮 **Editor Visual Control Panel / 编辑器可视化控制台**  
  Interactive custom inspector providing real-time playback buttons, stop, and rest pose debugging in Play Mode without writing code.  
  配套定制化 Inspector 调试面板，运行态下无需敲代码即可一键点播动作、打断与测试站姿。

- 🔔 **Dual-Track Event System / 双轨事件解耦系统**  
  Exposes both C# native `event Action<int>` (for high-performance 0-GC logic) and serialized `UnityEvent<int>` (for no-code audio/VFX designer binding).  
  双轨事件架构：兼顾程序员的高性能强类型委托调用与策划/技术美术在 Inspector 上的无代码连线。

---

## 📦 Installation / 安装方式

### 方式一：UPM Git URL (推荐 / Recommended)
在 Unity 菜单栏打开：`Window` -> `Package Manager` -> 点击左上角 `+` 号 -> `Add package from git URL...`，输入：
```text
https://github.com/zhangyuzhangyu233/UniVRMA-MotionPlayer.git
```

### 方式二：离线压缩包 / UPM Tarball (`.tgz`)
从 Releases 下载 `com.univrma.motionplayer-0.1.0-alpha.tgz`，在 Package Manager 中选择：
`Add package from tarball...` 并选择该文件即可。

### 方式三：传统单文件资产包 (`.unitypackage`)
直接双击导入 `UniVRMA_MotionPlayer_v0.1.0-alpha.unitypackage` 即可自动导入至项目中。

---

## 🚀 Quick Start / 快速上手

### 1. 场景配置
1. 在场景中的 VRM 1.0 模型物体上挂载 `VrmaPlayer` 组件；
2. 将 `.vrma` 动作预制体拖入 `vrmaPrefabs` 列表中（留空也可在运行时动态点播）；
3. 进入 Play Mode，即可在 Inspector 控制台或代码中自由驱动！

### 2. 代码调用示例
```csharp
using UnityEngine;
using UniVRMA;

public class CharacterControllerDemo : MonoBehaviour
{
    [SerializeField] private GameObject vrmaPlayerObject;
    private IVrmaPlayer _player;

    private void Awake()
    {
        // 推荐面向接口编程 (DIP 依赖倒置)
        _player = vrmaPlayerObject.GetComponent<IVrmaPlayer>();

        // 监听动作生命周期
        _player.OnClipStarted += index => Debug.Log($"动作开始: {index}");
        _player.OnClipStopped += index => Debug.Log($"动作停止: {index}");
    }

    public void OnPlayGreetingClicked()
    {
        // 1. 按索引极速点播 (0 字符串开销, 0 GC)
        _player.Play(0);

        // 2. 或按动作名称模糊匹配点播
        // _player.PlayByName("Greeting");
    }

    public void OnStopClicked()
    {
        // 停止动作并平滑回归自然待机站姿
        _player.Stop();
    }
}
```

---

## 📑 API Reference / 接口规范

核心接口定义于 `IVrmaPlayer`：

| 属性 / 方法 | 类型 | 说明 |
| :--- | :--- | :--- |
| `CurrentIndex` | `int` (get) | 当前播放的动作索引（未播放为 -1） |
| `IsPlaying` | `bool` (get) | 当前是否处于动作播放态 |
| `ClipCount` | `int` (get) | 当前对象池中的动作总数 |
| `ArmRestAngle` | `float` (get/set) | 自然待机时手臂外展下垂角度（推荐 65°~75°） |
| `Play(int index)` | `void` | 根据索引快速播放动作 |
| `Play(GameObject prefab)`| `void` | 动态传入预制体播放（自动扩容对象池） |
| `PlayByName(string name)`| `void` | 根据动作资源名模糊匹配播放 |
| `Stop()` | `void` | 切断当前动作并恢复站姿 |
| `ResetToRestPose()` | `void` | 强制将骨架重置为自然站姿 |
| `OnClipStarted` | `event Action<int>` | 动作开始播放委托回调 |
| `OnClipStopped` | `event Action<int>` | 动作播放结束/打断委托回调 |

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
本项目基于 MIT 协议完全开源，允许自由商用、修改与再发布。
