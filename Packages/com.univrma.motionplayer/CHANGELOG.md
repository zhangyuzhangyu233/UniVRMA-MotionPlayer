# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0-alpha] - 2026-10-23

### Added
- Core `IVrmaPlayer` interface definition for decoupled architecture and dependency inversion (DIP).
- Production-grade `VrmaPlayer` component with zero-GC motion playback and seamless UniVRM 1.0 integration.
- `StableHipsPoseProvider` decorator resolving cross-model avatar height discrepancies and preventing vertical sinking/floating.
- `VrmaControlRigUtility` with cached reflection metadata and natural 70-degree arm resting pose enforcement in `LateUpdate`.
- Isolated world-space `[VRMA_Pool]` with dynamic auto-expanding object pool supporting runtime `Play(GameObject prefab)` hot-plugging.
- Dual-track event notifications: native C# `event Action<int>` and serialized `UnityEvent<int>`.
- Custom interactive Unity Editor inspector (`VrmaPlayerEditor`) with real-time Play Mode controls and diagnostics.
