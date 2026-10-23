# UniVRMA Motion Player

**English** · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

### Lightweight, Zero-Allocation VRMA Motion Playback Component for Unity and UniVRM 1.0

Production-ready out of the box, bridging the critical gap in official VRMA runtime adoption.

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-000?logo=unity&logoColor=white)](https://unity.com/)
[![UniVRM](https://img.shields.io/badge/UniVRM-1.0-4aa8ff)](https://github.com/vrm-c/UniVRM)
[![Version](https://img.shields.io/badge/version-0.1.0--alpha-orange)](Packages/com.univrma.motionplayer/CHANGELOG.md)
[![GC Alloc](https://img.shields.io/badge/GC%20Alloc-0%20B-3ddc97)](#zero-gc-engineering-guarantee)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

![UniVRMA Motion Player banner](docs/banner_en_2400x960.png)

---

## Core Engineering Solutions

| Upstream Pipeline Hurdles | Solved with UniVRMA |
| :--- | :--- |
| `NullReferenceException` thrown during Play Mode initialization on scene-placed prefabs | Automated lifecycle management; lazy control rig regeneration and safe bone binding |
| Absolute vertical root motion causes avatars of different heights to float or sink into the floor | `StableHipsPoseProvider` decorator anchors hips to the avatar's baseline height per frame |
| Hierarchy transformations or avatar turns cause 180° motion flipping or flipped bone axes | Motion instances reside in an isolated `[VRMA_Pool]` at world origin, preventing hierarchy pollution |
| Animation stop snaps the skeleton directly into a stiff, robotic 90° T-pose | Pipeline-order 70° natural arm drape injected in `LateUpdate` after animation and IK evaluation |
| Root motion transforms conflict with business-level GameObject hierarchies | Complete decoupling between motion sampling pool and rendering instances |

A dedicated single-component solution resolving five production obstacles without requiring glue code.

---

## Technical Highlights

- **Zero Runtime Allocation (Zero-GC Pipeline)**  
  `Play(int)` operates purely by updating the `Vrm10Instance.Runtime.VrmAnimation` pointer. No dynamic `Instantiate`, no closure allocations, and no LINQ queries. Profiling confirms **GC Alloc = 0 B** during clip switching.

- **Height-Agnostic Stabilization**  
  A single VRMA clip can be shared across models of varying proportions (children, teens, adults) without vertical drift. The decorator preserves full rotation fidelity while pinning hips to the avatar's native ground baseline.

- **Pipeline-Order Rest Pose Injection**  
  Applies an ergonomic 70° arm resting drape (configurable between 30°–90°) during `LateUpdate`, executing strictly after Animator and Animation matrix evaluation, eliminating T-pose artifacts without conflicting with IK or Look-At solvers.

- **Dynamic Object Pooling & Hot-Plugging**  
  In addition to serialized clip lists, runtime injection (`Play(GameObject prefab)`) is supported for Addressables or downloaded assets, automatically handling instantiation, bone mapping, and pooling on demand.

- **Dual-Track Decoupled Architecture**  
  Features native C# `event Action<int>` delegates for zero-boxing performance in game logic, alongside serialized `UnityEvent<int>` bindings for technical artists and designers in the Inspector.

- **Minimalist & Zero Intrusion**  
  Consists of only two runtime classes and one editor inspector, depending solely on UniVRM 1.0 public APIs, with zero footprint on existing project architecture.

---

## Installation

### Method 1: Unity Package Manager (UPM Git URL, Recommended)

In Unity, navigate to: `Window` -> `Package Manager` -> Click `+` -> `Add package from git URL...`, and enter:

```text
https://github.com/zhangyuzhangyu233/UniVRMA-MotionPlayer.git?path=Packages/com.univrma.motionplayer#v0.1.0-alpha
```

<details>
<summary>Alternative Installation Methods</summary>

**Method 2: UPM Tarball (.tgz)**  
Download `com.univrma.motionplayer-0.1.0-alpha.tgz` from [Releases](https://github.com/zhangyuzhangyu233/UniVRMA-MotionPlayer/releases), then use `Add package from tarball...` in Package Manager.

**Method 3: Traditional Asset Package (.unitypackage)**  
Download `UniVRMA_MotionPlayer_v0.1.0-alpha.unitypackage` and double-click to import. Assets will be placed under `Assets/UniVRMA Motion Player/`.

</details>

| Requirement | Minimum Version |
| :--- | :--- |
| Unity | 2021.3 LTS+ (Fully compatible with 2022.3 and Unity 6) |
| UniVRM | `com.vrmc.vrm` 0.120.0+ (VRM 1.0) |
| Render Pipeline | Built-in, URP, and HDRP compatible (no shader dependencies) |

> Note: VRMA specification is strictly part of the VRM 1.0 standard. VRM 0.x models are not supported.

---

## Quick Start

### 1. Inspector Configuration
1. Attach the `VrmaPlayer` component to any GameObject containing `Vrm10Instance` (or its parent);
2. Assign your `.vrma` motion prefabs into the `VRMA Prefabs` serialized array;
3. Enter Play Mode. Interactive playback, stop, and rest pose buttons will become active directly in the Inspector.

### 2. Code Example
```csharp
using UnityEngine;
using UniVRMA;

public class CharacterMotionController : MonoBehaviour
{
    [SerializeField] private VrmaPlayer player;

    private void Awake()
    {
        if (player == null) player = GetComponent<VrmaPlayer>();

        // Register event callbacks
        player.OnClipStarted += OnMotionStarted;
        player.OnClipStopped += OnMotionStopped;
    }

    private void OnMotionStarted(int index) => Debug.Log($"Motion started: {index}");
    private void OnMotionStopped(int index) => Debug.Log($"Motion stopped: {index}");

    public void PlayGreetingMotion()
    {
        // 1. Play by index (zero allocation, recommended for hot paths)
        player.Play(0);

        // 2. Play by clip asset name
        // player.PlayByName("Greeting");

        // 3. Play dynamically loaded asset (automatically pooled)
        // player.Play(runtimePrefab);
    }

    public void StopAllMotions()
    {
        // Halts current animation and recovers the 70° natural resting pose
        player.Stop();
    }
}
```

---

## API Reference

Core contracts are exposed via the `IVrmaPlayer` interface:

| Member | Type | Description |
| :--- | :--- | :--- |
| `CurrentIndex` | `int` (get) | Currently active motion index; `-1` when idle |
| `IsPlaying` | `bool` (get) | Indicates whether a motion is currently playing |
| `ClipCount` | `int` (get) | Total count of initialized clips in the pool |
| `ArmRestAngle` | `float` (get/set) | Resting arm drape angle (default 70°) |
| `Play(int index)` | `void` | Plays motion by index |
| `Play(GameObject prefab)` | `void` | Plays dynamically injected prefab (expands pool if new) |
| `PlayByName(string name, bool loop)` | `void` | Plays motion matching asset name (case-insensitive) |
| `Stop()` | `void` | Stops playback and returns avatar to rest pose |
| `ResetToRestPose()` | `void` | Immediately enforces the natural standing pose |
| `OnClipStarted` / `OnClipStopped` | `event Action<int>` | Strong-typed zero-boxing C# delegate events |
| `OnClipStartedEvent` / `OnClipStoppedEvent` | `UnityEvent<int>` | Serialized events for Inspector wiring |

---

## Internal Pipeline & Architecture

```text
Vrm10Instance.Runtime
├── ControlRig   ← StableHipsPoseProvider (Decorator) ← InitRotationPoseProvider
└── VrmAnimation ← Vrm10AnimationInstance (Pointer swapping, 0 GC)

[VRMA_Pool]      (Isolated world-space root decoupled from character transforms)
├── VRMA_Inst_1_Greeting
└── VRMA_Inst_2_Clapping
```

During playback, four atomic operations are performed:
1. Stop previous Legacy Animation component;
2. Deactivate previous sampling instance;
3. Assign target `VrmAnimation` pointer to the new instance;
4. Call `Rewind()` and `Play()` on the target instance.
Following warm-up, no heap allocations occur during runtime playback.

---

## Zero-GC Engineering Guarantee

UniVRMA enforces strict zero-allocation constraints:
- Hot paths contain no interface enumerations, LINQ invocations, or string concatenations;
- Configured clips are instantiated once during `Awake`/`Start`;
- Clip switching is equivalent to reference pointer assignment;
- Verification: Open Unity Profiler, filter CPU Usage by `VrmaPlayer.Play`, confirm **GC Alloc = 0 B**.

---

## FAQ

**How does this differ from the official UniVRM samples?**  
UniVRM provides the core retargeting algorithms but leaves lifecycle pooling, prefab instantiation stability, vertical offset compensation, and resting pose coverage to developers. UniVRMA productionizes these gaps.

**Can models with extreme proportions be used?**  
Standard humanoid proportions are fully supported. For non-standard models, verify floor contact under relevant animations.

**Is animation cross-fading supported?**  
The 0.1.0-alpha release utilizes immediate switching. Dual-sampler blending is planned on the roadmap.

**Compatibility with IL2CPP and Mobile Platforms?**  
Fully compatible. Written in standard C# without runtime code generation, verified on iOS, Android, and desktop platforms.

---

## Constraints

- Exclusively targets VRM 1.0; VRM 0.x models are not supported;
- Blending transitions between clips are not included in this release;
- `PlayByName` incurs string comparison overhead; use `Play(int index)` in performance-critical code;
- Resting angle applies solely to upper arms; full idle motion requires dedicated idle VRMA clips.

---

## Contributing

Issue reports and pull requests are welcomed. When reporting bugs, please provide minimal reproduction assets (VRM model and VRMA clip).

- [Bug Report](.github/ISSUE_TEMPLATE/bug_report.yml)
- [Feature Request](.github/ISSUE_TEMPLATE/feature_request.yml)
- [Changelog](Packages/com.univrma.motionplayer/CHANGELOG.md)

---

## License & Copyright

- **Codebase**: Licensed under the [MIT License](LICENSE). Free for commercial and private integration.
- **Showcase Illustration**: The featured character is **Glacier Blue (冰川蓝)**. Illustration Copyright **© 2026 @冰川蓝蓝 — All rights reserved**. The artwork is not covered by the MIT license and is strictly for project display.

Star support is appreciated to help maintain and evolve the project.
