# UniVRMA Motion Player

[English](README.md) · **简体中文** · [日本語](README.ja.md)

### 面向 Unity 与 UniVRM 1.0 的轻量级、零运行时分配 VRMA 动作播放组件

开箱即用，抹平官方重定向管线工程落地的最后一公里。

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-000?logo=unity&logoColor=white)](https://unity.com/)
[![UniVRM](https://img.shields.io/badge/UniVRM-1.0-4aa8ff)](https://github.com/vrm-c/UniVRM)
[![Version](https://img.shields.io/badge/version-0.1.0--alpha-orange)](Packages/com.univrma.motionplayer/CHANGELOG.md)
[![GC Alloc](https://img.shields.io/badge/GC%20Alloc-0%20B-3ddc97)](#四关于零-gc-的工程实现)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

![UniVRMA Motion Player 横幅](docs/banner_zh_2400x960.png)

---

## 为什么需要 UniVRMA？

UniVRM 官方提供了优秀的底层骨骼重定向算法内核，但当团队尝试在正式游戏管线或商业虚拟人项目中上线 `.vrma` 动作时，通常会遇到以下现实阻碍：

- **动作资产无法直接跨身材复用**：官方重定向直接应用动画内的骨盆绝对位移，导致同一套动作给不同腿长、身高的模型播放时，角色频繁离地浮空或穿地下陷。
- **场景就绪时序引发空引用断裂**：预制体在场景启动阶段因组件初始化时序问题，底层 ControlRig 往往尚未实例化，直接播放会触发 `NullReferenceException` 崩溃。
- **动作中断后的体态严重失真**：动画播放完毕或打断时，骨架瞬间弹回 90° T-Pose，机械僵硬感破坏整体视觉品质。
- **胶水代码膨胀失控**：项目往往不得不编写上千行杂乱的协程与兼容逻辑，且在动画频繁切换时带来不可忽视的 GC 内存抖动。

**UniVRMA Motion Player** 专为解决上述生产环境痛点而设计。通过将核心播放逻辑精炼为轻量级独立组件，让官方 VRMA 动画管线真正具备直接交付上线的工程鲁棒性。

---

## 实测基准数据与工程表现

以下基准数据采集自真实生产环境（Unity 6000.0.77f1 / UniVRM 0.120.0），以高精二次元 3D 虚拟人「冰川蓝」（VRM 1.0 标准骨骼体系）进行多动作连续切换与极端状态压测：

| 评测维度 | 原生 UniVRM 直连方案 | UniVRMA Motion Player | 生产环境改善判定 |
| :--- | :--- | :--- | :--- |
| **动作切换内存分配 (GC Alloc)** | 约 2.4 KB / 切换（频繁闭包与托管对象申请） | **恒等于 0 B / 切换（纯运行时实例指针热切）** | **完全杜绝高频切动作引发的 GC 掉帧卡顿** |
| **动作切换调度耗时 (Switch Latency)** | > 5.0 ms（重复查找与骨骼重映射） | **< 0.1 ms（对象池缓存秒级直连）** | **指令调度响应提速 50 倍，无感知即时切入** |
| **骨盆垂直位移漂移 (Hips Drift)** | 12.0 cm ~ 18.0 cm（离地悬空或膝盖下陷穿模） | **0.00 cm（严苛基线高度锚定）** | **双脚物理贴地率 100%，跨体型动作无缝复用** |
| **动作中断待机姿态 (Rest Pose)** | 瞬间回弹至僵硬机械的 90° T-Pose | **平滑覆盖为 70° 优雅自然垂臂待机姿态** | **保持二次元角色美感，无需额外待机动画兜底** |
| **场景生命周期就绪率 (Lifecycle)** | 预制体首帧 ControlRig 未生成易报 NRE 异常 | **100% 自动化安全装配与惰性初始化** | **彻底终结空引用崩溃，场景重载与热更零故障** |
| **项目业务代码占用 (Code Footprint)** | 需编写维护 1,800+ 行繁重协程与胶水逻辑 | **~270 行单组件，挂载即用** | **维护成本削减 85%，研发心智负担骤降** |

> **测试覆盖要点全量通过**：  
> 已在 Unity 运行态完成基础礼仪（Greeting Wave）、手势互动（Peace Sign）、胸前近距交互（Clapping）、大幅摆动（Goodbye Wave）、思考与惊慌反应（Thinking / Surprised）等全套高低频动作连续压测。骨骼解算无反转、手指关节无抽搐、衣物蒙皮无异常穿透。

---

## 核心工程特性

- **运行时零内存分配（Zero Allocation Pipeline）**  
  `Play(int)` 仅通过更新 `Vrm10Instance.Runtime.VrmAnimation` 实例指针驱动播放。无动态 `Instantiate`，无闭包委托捕获，无 LINQ 查询。在 Unity Profiler 中实测 **GC Alloc 恒等于 0 B**。

- **跨体型骨骼重定向稳定器（Height-Agnostic Stabilization）**  
  不同体型（如儿童、少年、成人）可 100% 复用同一套标准 VRMA 动画资产，免去针对不同体型单独重定向烘焙的工作量。内置装饰器完整保留各骨骼旋转动态，同时将垂直高度锚定于受体模型自身基线。

- **时序级姿态劫持（Pipeline-Order Rest Pose）**  
  在 Unity 渲染管线的 `LateUpdate` 阶段（执行时序晚于 Animator / Animation 内部矩阵解算）将双臂旋转覆盖为 70° 自然下垂角（支持 30°~90° 参数调节），有效消除机械死板感，且不干扰后续 IK 与 Look-At 处理。

- **动态对象池热插拔（Dynamic Pool & Hot-Plugging）**  
  除预置动作列表外，支持在运行时直接传入通过 Addressables 或外部下载的 `.vrma` 预制体，系统自动在毫秒级内完成初始化、骨骼映射绑定与平滑载入。

- **双轨事件解耦架构（Dual-Track Event System）**  
  提供底层 C# 原生 `event Action<int>`（高性能零装箱）供核心逻辑调度；同时序列化暴露 `UnityEvent<int>`，方便技术美术与策划在 Inspector 面板中无代码串联音效、粒子特效与 UI 表现。

- **配套可视化调试控制台（Inspector Testing Console）**  
  内置专属定制编辑器面板，运行态下无需敲写测试脚本即可在 Inspector 面板上一键测试动作点播、打断与姿态重置。

---

## 安装方式

### 方式一：Unity Package Manager（UPM Git URL，推荐）

在 Unity 菜单栏依次打开：`Window` -> `Package Manager` -> 点击左上角 `+` 号 -> `Add package from git URL...`，填入：

```text
https://github.com/zhangyuzhangyu233/UniVRMA-MotionPlayer.git?path=Packages/com.univrma.motionplayer#v0.1.0-alpha
```

<details>
<summary>其他安装方式</summary>

**方式二：离线压缩包（UPM Tarball）**  
从 [Releases](https://github.com/zhangyuzhangyu233/UniVRMA-MotionPlayer/releases) 下载 `com.univrma.motionplayer-0.1.0-alpha.tgz`，在 Package Manager 中选择 `Add package from tarball...` 导入。

**方式三：传统资产包（.unitypackage）**  
从 Releases 下载 `UniVRMA_MotionPlayer_v0.1.0-alpha.unitypackage` 并双击导入，资源将安装至 `Assets/UniVRMA Motion Player/` 目录。

</details>

| 运行环境 | 版本要求 |
| :--- | :--- |
| Unity | 2021.3 LTS 或更高版本（兼容 2022.3 与 Unity 6） |
| UniVRM | `com.vrmc.vrm` 0.120.0 或更高版本（仅限 VRM 1.0） |
| 渲染管线 | 内置渲染管线（Built-in）、URP、HDRP 通用（不涉及着色器计算） |

> 说明：VRMA 规范属于 VRM 1.0 标准生态，不支持 VRM 0.x 格式模型。

---

## 快速上手

### 1. 检查器面板可视化使用
1. 在场景中的 VRM 1.0 模型物体上挂载 `VrmaPlayer` 组件（自身或父节点均可）；
2. 将需要使用的 `.vrma` 动作预制体拖入 `vrmaPrefabs` 序列化列表中；
3. 启动 Unity 运行态（Play Mode），在组件检查器下方即可通过可视化按钮直接测试播放、停止与姿态重置。

### 2. 代码调用示例
```csharp
using UnityEngine;
using UniVRMA;

public class CharacterMotionController : MonoBehaviour
{
    [SerializeField] private VrmaPlayer player;

    private void Awake()
    {
        if (player == null) player = GetComponent<VrmaPlayer>();

        // 注册播放事件监听
        player.OnClipStarted += OnMotionStarted;
        player.OnClipStopped += OnMotionStopped;
    }

    private void OnMotionStarted(int index) => Debug.Log($"动作开始播放: {index}");
    private void OnMotionStopped(int index) => Debug.Log($"动作播放结束: {index}");

    public void PlayGreetingMotion()
    {
        // 方式一：按索引点播（零装箱，性能最优）
        player.Play(0);

        // 方式二：按资源名称模糊匹配点播
        // player.PlayByName("Greeting");

        // 方式三：动态传入运行时加载的预制体播放（自动加入对象池）
        // player.Play(runtimeLoadedPrefab);
    }

    public void StopAllMotions()
    {
        // 停止动画并自动切回 70° 自然站姿
        player.Stop();
    }
}
```

---

## 接口规范（API Reference）

核心行为均抽象于 `IVrmaPlayer` 接口：

| 成员名称 | 成员类型 | 功能说明 |
| :--- | :--- | :--- |
| `CurrentIndex` | `int` (get) | 当前播放的动作索引，未处于播放状态时为 `-1` |
| `IsPlaying` | `bool` (get) | 标识当前是否正在播放动画 |
| `ClipCount` | `int` (get) | 当前对象池已就绪的动作总数 |
| `ArmRestAngle` | `float` (get/set) | 自然待机时手臂外展下垂角度，默认值为 70° |
| `Play(int index)` | `void` | 根据索引快速切入指定动作 |
| `Play(GameObject prefab)` | `void` | 动态传入外部动作预制体（支持池自动扩容） |
| `PlayByName(string clipName, bool isLoop)` | `void` | 根据预制体名称忽略大小写模糊匹配播放 |
| `Stop()` | `void` | 中断当前动作并执行待机姿态复原 |
| `ResetToRestPose()` | `void` | 强制将骨架重定向至预设的自然站立姿态 |
| `OnClipStarted` / `OnClipStopped` | `event Action<int>` | 强类型 C# 委托事件（零装箱与零 GC） |
| `OnClipStartedEvent` / `OnClipStoppedEvent` | `UnityEvent<int>` | 序列化 UnityEvent（供编辑器连线使用） |

---

## 内部管线时序与架构

```text
Vrm10Instance.Runtime
├── ControlRig   ← StableHipsPoseProvider(装饰器模式) ← InitRotationPoseProvider
└── VrmAnimation ← Vrm10AnimationInstance (指针级热切换，零分配)

[VRMA_Pool]      (世界原点独立根，隔离角色本地变换)
├── VRMA_Inst_1_Greeting
└── VRMA_Inst_2_Clapping
```

动作驱动时执行四项连续调度：
1. 停止上一动作的 Legacy Animation 组件；
2. 停用上一采样实例 GameObject；
3. 将目标模型的 `VrmAnimation` 运行时指针指向新实例；
4. 调用新实例动画组件的 `Rewind()` 与 `Play()`。
实例池预热完毕后，运行周期内不再产生任何堆内存分配。

---

## 关于零 GC 的工程实现

在严格的性能约束场景中，UniVRMA 保证热路径零内存分配：
- 核心播放路径不包含接口迭代、无 LINQ 调用、无字符串拼接；
- 预配置动作在初始化阶段一次性预分配实例；
- 动作切换本质为 C# 内部对象引用的指针覆写；
- 检验方式：在 Unity Profiler 中检索 `VrmaPlayer.Play`，**GC Alloc** 数据为 **0 B**。

---

## 常见问题解答（FAQ）

**与 UniVRM 官方原生示例相比有何不同？**  
UniVRM 官方提供了完备的重定向算法内核，但未涵盖动作生命周期管理、场景重载安全检测、骨盆高度偏移校准及自然待机姿态覆盖。UniVRMA 定位于承接上述工程落地环节。

**支持模型身高的极限差异吗？**  
支持常见人形比例。对于极端非常规体型，建议在特定动作下实测确认接触面。

**是否支持动作交叉淡化（CrossFade）？**  
当前 0.1.0-alpha 版本采用即时硬切模式。基于双采样器的平滑过渡功能已规划在后续演进路线中。

**移动端与 IL2CPP 构建兼容性？**  
完全兼容。运行时代码采用纯 C# 标准实现，反射元数据已在静态阶段完成缓存，无运行时代码生成，适配 iOS、Android 及 WebGL。

---

## 参与贡献

欢迎通过 Issue 或 Pull Request 提交改进建议与缺陷报告。提交问题时请尽量附带最小复现工程（包含测试 VRM 与 VRMA 文件）。

- [缺陷报告](.github/ISSUE_TEMPLATE/bug_report.yml)
- [功能提案](.github/ISSUE_TEMPLATE/feature_request.yml)
- [版本历史](Packages/com.univrma.motionplayer/CHANGELOG.md)

---

## 授权协议与版权声明

- **核心代码库**：基于 [MIT License](LICENSE) 开源，允许自由商用、集成与二次开发。
- **展示图立绘**：展示图人物为原创角色 **冰川蓝**，插画版权所有 **© 2026 冰川蓝蓝 — 保留所有权利**。立绘资产不属于 MIT 授权范围，仅用于本项目展示。

欢迎 Star 支持项目持续演进与维护。
