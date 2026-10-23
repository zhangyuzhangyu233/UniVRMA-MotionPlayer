# UniVRMA Motion Player

[English](README.md) · **简体中文** · [日本語](README.ja.md)

### 面向 Unity 与 UniVRM 1.0 的轻量级、零运行时分配 VRMA 动作播放组件

开箱即用，抹平官方重定向管线工程落地的最后一公里。

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-000?logo=unity&logoColor=white)](https://unity.com/)
[![UniVRM](https://img.shields.io/badge/UniVRM-1.0-4aa8ff)](https://github.com/vrm-c/UniVRM)
[![Version](https://img.shields.io/badge/version-0.1.0--alpha-orange)](Packages/com.univrma.motionplayer/CHANGELOG.md)
[![GC Alloc](https://img.shields.io/badge/GC%20Alloc-0%20B-3ddc97)](#关于零-gc-的工程实现)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

![UniVRMA Motion Player 横幅](docs/banner_zh_2400x960.png)

---

## 解决的核心痛点

| UniVRM 原生管线工程断点 | 引入 UniVRMA 后的表现 |
| :--- | :--- |
| 场景预制体在 Play 态初始化时，因 ControlRig 尚未就绪偶发 `NullReferenceException` | 组件自动接管生命周期，底层自动唤醒并安全绑定骨架实例 |
| 跨身材/身高模型复用动作时，绝对骨盆位移导致角色离地浮空或穿地下陷 | `StableHipsPoseProvider` 装饰器将骨盆重定向锚定于受体模型基准高度，双脚稳定贴地 |
| 角色旋转或移动后，Transform 空间叠加导致动作轴向反转或翻转走样 | 动作采样实例部署在世界原点的独立 `[VRMA_Pool]` 隔离空间中，彻底杜绝层级变换污染 |
| 动画播放结束或调用 `Stop()` 后，骨架突变重置为僵硬的 90° T-Pose | 管线时序注入 70° 自然下垂角，在动画和 IK 解算后精准覆盖，保持自然站姿 |
| 动作根骨骼位移（Root Motion）与业务层 Transform 产生位移冲突 | 动作采样池与渲染实例完全解耦，运动逻辑保持独立透明 |

一套专用组件，系统性解决五项工程落地断点，无需编写额外粘合代码。

---

## 核心工程特性

- **运行时零内存分配（Zero Allocation Pipeline）**  
  `Play(int)` 仅通过更新 `Vrm10Instance.Runtime.VrmAnimation` 实例指针驱动播放。无动态 `Instantiate`，无闭包委托捕获，无 LINQ 查询。在 Unity Profiler 中实测 **GC Alloc 恒等于 0 B**。

- **跨体型骨骼重定向稳定器（Height-Agnostic Stabilization）**  
  不同体型（如儿童、少年、成人）可 100% 复用同一套标准 VRMA 动画资产，免去针对不同体型单独重定向烘焙的工作量。装饰器完整保留各骨骼旋转动态，同时将垂直高度锚定于受体模型自身基线。

- **时序级姿态劫持（Pipeline-Order Rest Pose）**  
  在 Unity 渲染管线的 `LateUpdate` 阶段（执行时序晚于 Animator / Animation 内部矩阵解算）将双臂旋转覆盖为 70° 自然下垂角（支持 30°~90° 参数调节），有效消除机械死板感，且不干扰后续 IK 与 Look-At 处理。

- **动态对象池热插拔（Dynamic Pool & Hot-Plugging）**  
  除预置动作列表外，支持在运行时直接传入通过 Addressables 或外部下载的 `.vrma` 预制体，系统自动在毫秒级内完成初始化、骨骼映射绑定与平滑载入。

- **双轨事件解耦架构（Dual-Track Event System）**  
  提供底层 C# 原生 `event Action<int>`（高性能零装箱）供核心逻辑调度；同时序列化暴露 `UnityEvent<int>`，方便技术美术与策划在 Inspector 面板中无代码串联音效、粒子特效与 UI 表现。

- **极简轻量与零侵入性（Minimal Footprint）**  
  全包仅包含 2 个运行时核心类与 1 个配套编辑器扩展，仅依赖官方 UniVRM 1.0 公开接口，对宿主工程 100% 零侵入。

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

## 约束与边界

- 专为 VRM 1.0 标准设计，不兼容 VRM 0.x 资产；
- 当前版本暂不包含动作融合与淡入淡出曲线；
- `PlayByName` 包含字符串比较开销，高频性能路径请优先调用 `Play(int index)`；
- 待机角仅作用于上臂旋转，完整待机动作建议搭配循环待机 VRMA 使用。

---

## 参与贡献

欢迎通过 Issue 或 Pull Request 提交改进建议与缺陷报告。提交问题时请尽量附带最小复现工程（包含测试 VRM 与 VRMA 文件）。

- [缺陷报告](.github/ISSUE_TEMPLATE/bug_report.yml)
- [功能提案](.github/ISSUE_TEMPLATE/feature_request.yml)
- [版本历史](Packages/com.univrma.motionplayer/CHANGELOG.md)

---

## 授权协议与版权声明

- **核心代码库**：基于 [MIT License](LICENSE) 开源，允许自由商用、集成与二次开发。
- **展示图立绘**：展示图人物为原创角色 **冰川蓝**，插画版权所有 **© 2026 @冰川蓝蓝 — 保留所有权利**。立绘资产不属于 MIT 授权范围，仅用于本项目展示。

欢迎 Star 支持项目持续演进与维护。
