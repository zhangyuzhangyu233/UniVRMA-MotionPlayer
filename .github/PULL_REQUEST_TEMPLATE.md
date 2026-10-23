## What changed

<!-- One or two sentences. Link the issue: "Closes #12". -->

## Why

<!-- The bug or limitation this addresses. If it is a behaviour change, say so. -->

## Scope

- [ ] Runtime (`Runtime/`)
- [ ] Editor (`Editor/`)
- [ ] Package metadata (`package.json`, `CHANGELOG.md`, `README.md`)
- [ ] CI / release pipeline
- [ ] Docs only
- [ ] I also updated `CHANGELOG.md`

## How it was verified

<!-- Unity version, render pipeline, models tested, Play Mode steps, screenshots/GIFs. -->

| Item | Value |
| :--- | :--- |
| Unity | |
| UniVRM | |
| Pipeline | Built-in / URP / HDRP |
| Devices | Editor / Quest 2 / Android / WebGL |

## Zero-GC / performance check

<!-- If this touches per-frame code, confirm no new managed allocations in Play Mode. -->

- [ ] No new per-frame allocations in `Play()` / `LateUpdate()`
- [ ] Not applicable (docs / editor / CI only)

## Checklist

- [ ] I read `CONTRIBUTING.md` (or the README contribution notes).
- [ ] Public API changes are reflected in `README.md` and `CHANGELOG.md`.
- [ ] No `.meta` files were deleted or GUIDs changed unintentionally.
- [ ] New assets/code do not include unlicensed third-party material.

---

<!-- Thanks! A maintainer will review within a few days. -->
