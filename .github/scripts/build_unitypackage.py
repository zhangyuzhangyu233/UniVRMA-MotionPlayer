#!/usr/bin/env python3
"""Build a Unity ``.unitypackage`` from a UPM package folder — no Unity Editor required.

A ``.unitypackage`` is simply a gzip-compressed POSIX tar. Every asset gets its own
folder named after the asset's GUID, holding three entries::

    <guid>/asset        raw file bytes (or an empty directory, for folders)
    <guid>/asset.meta   the serialized .meta file
    <guid>/pathname     project-relative destination path, e.g. Assets/Foo/Bar.cs

Unity reconstructs the import list from those three entries, so the package can be
generated on a plain Linux runner from the GUIDs already committed in the repository.
The release pipeline therefore needs no Unity license, no Docker image and no
batch-mode import.

Usage
-----
    python3 build_unitypackage.py \
        --package Packages/com.univrma.motionplayer \
        --out dist/UniVRMA_MotionPlayer_v0.1.0-alpha.unitypackage \
        --asset-prefix "Assets/UniVRMA Motion Player" \
        --include Runtime --include Editor
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import io
import re
import sys
import tarfile
from pathlib import Path

GUID_RE = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.MULTILINE)

IGNORE_NAMES = {".DS_Store", "Thumbs.db", "desktop.ini"}
IGNORE_SUFFIXES = (".tmp", ".bak", ".orig", ".tgz", ".unitypackage")


def fail(msg: str) -> None:
    print(f"[unitypackage] ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def read_guid(meta: Path) -> str:
    try:
        text = meta.read_text(encoding="utf-8", errors="replace")
    except OSError as exc:
        fail(f"cannot read {meta}: {exc}")
    match = GUID_RE.search(text)
    if not match:
        fail(f"no 'guid:' line found in {meta}")
    return match.group(1).lower()


def synth_guid(pathname: str) -> str:
    """Deterministic GUID for folders that do not ship a .meta (e.g. the Assets root)."""
    return hashlib.md5(f"unitypackage:{pathname}".encode("utf-8")).hexdigest()


def folder_meta(guid: str) -> bytes:
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    ).encode("utf-8")


def collect(pkg: Path, includes: list[str], prefix: str) -> list[tuple[str, str, str, object]]:
    """Return (dest_path, guid, kind, payload) records, sorted and de-duplicated.

    kind is one of: ``folder`` (payload = meta bytes), ``file`` (payload = Path),
    ``meta`` (payload = meta bytes).
    """
    records: list[tuple[str, str, str, object]] = []

    # Destination root folder, e.g. Assets/UniVRMA Motion Player
    root_guid = synth_guid(prefix)
    records.append((prefix, root_guid, "folder", folder_meta(root_guid)))

    for include in includes:
        directory = pkg / include
        if not directory.is_dir():
            fail(f"included directory does not exist: {directory}")

        # the include root itself is an asset folder too
        root_meta = directory.with_name(directory.name + ".meta")
        if root_meta.is_file():
            records.append((f"{prefix}/{include}", read_guid(root_meta), "folder", root_meta.read_bytes()))
        else:
            g = synth_guid(f"{prefix}/{include}")
            records.append((f"{prefix}/{include}", g, "folder", folder_meta(g)))

        for path in sorted(directory.rglob("*"), key=lambda p: p.relative_to(pkg).as_posix().lower()):
            rel = path.relative_to(pkg).as_posix()
            if path.name in IGNORE_NAMES or path.name.endswith(IGNORE_SUFFIXES):
                continue
            if path.name.endswith(".meta"):
                continue  # consumed through its owner
            dest = f"{prefix}/{rel}"

            meta_file = path.with_name(path.name + ".meta")
            if path.is_dir():
                if meta_file.is_file():
                    records.append((dest, read_guid(meta_file), "folder", meta_file.read_bytes()))
                else:
                    guid = synth_guid(dest)  # Unity regenerates folder metas happily
                    records.append((dest, guid, "folder", folder_meta(guid)))
                continue

            if not meta_file.is_file():
                fail(f"missing .meta for {path} (Unity requires one per asset)")
            guid = read_guid(meta_file)
            records.append((dest, guid, "file", path))
            records.append((dest, guid, "meta", meta_file.read_bytes()))

    dedup: dict[tuple[str, str], tuple[str, str, str, object]] = {}
    for dest, guid, kind, payload in records:
        dedup[(guid, kind)] = (dest, guid, kind, payload)
    return [dedup[k] for k in sorted(dedup)]


def tar_add(tar: tarfile.TarFile, name: str, payload: object, kind: str) -> None:
    info = tarfile.TarInfo(name)
    info.mtime = 0
    info.uid = info.gid = 0
    info.uname = info.gname = ""
    if kind == "dir":
        info.type = tarfile.DIRTYPE
        info.mode = 0o755
        tar.addfile(info)
        return
    data = payload if isinstance(payload, bytes) else Path(payload).read_bytes()  # type: ignore[arg-type]
    info.size = len(data)
    info.mode = 0o644
    tar.addfile(info, io.BytesIO(data))


def build(pkg: Path, out: Path, prefix: str, includes: list[str]) -> tuple[int, int]:
    if not (pkg / "package.json").is_file():
        fail(f"{pkg} is not a UPM package (package.json missing)")

    records = collect(pkg, includes, prefix)
    if not records:
        fail("nothing to package")

    by_guid: dict[str, dict[str, object]] = {}
    for dest, guid, kind, payload in records:
        slot = by_guid.setdefault(guid, {"pathname": dest})
        slot[kind] = payload

    out.parent.mkdir(parents=True, exist_ok=True)
    tmp = out.with_name(out.name + ".tmp")
    try:
        with open(tmp, "wb") as raw:
            with gzip.GzipFile(fileobj=raw, mode="wb", compresslevel=9, mtime=0) as gz:
                with tarfile.open(fileobj=gz, mode="w", format=tarfile.USTAR_FORMAT) as tar:
                    for guid in sorted(by_guid):
                        slot = by_guid[guid]
                        tar_add(tar, f"{guid}/pathname", str(slot["pathname"]).encode("utf-8"), "file")
                        if "folder" in slot:
                            tar_add(tar, f"{guid}/asset", b"", "dir")
                            tar_add(tar, f"{guid}/asset.meta", slot["folder"], "file")
                        else:
                            tar_add(tar, f"{guid}/asset", slot["file"], "file")
                            tar_add(tar, f"{guid}/asset.meta", slot["meta"], "file")
        tmp.replace(out)
    except OSError as exc:
        fail(f"cannot write {out}: {exc}")

    verify(out)
    n_assets = sum(1 for slot in by_guid.values() if "file" in slot)
    n_folders = len(by_guid) - n_assets
    return n_assets, n_folders


def verify(path: Path) -> None:
    """Re-open the archive and prove every GUID folder is import-complete."""
    with tarfile.open(path, "r:gz") as tar:
        names = set(tar.getnames())
        guids = {n.split("/")[0] for n in names if "/" in n}
        for guid in sorted(guids):
            for required in ("pathname", "asset", "asset.meta"):
                if f"{guid}/{required}" not in names:
                    fail(f"verification failed: {guid} has no {required}")
        pathnames = {}
        for guid in guids:
            with tar.extractfile(f"{guid}/pathname") as fh:  # type: ignore[arg-type]
                pathnames[guid] = fh.read().decode("utf-8")
        if len(set(pathnames.values())) != len(pathnames):
            fail("verification failed: duplicate destination paths")
        print(f"[unitypackage] verified {len(guids)} GUID folders / {len(names)} tar entries")


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Build a Unity .unitypackage from a UPM package.")
    ap.add_argument("--package", required=True, type=Path, help="UPM package folder")
    ap.add_argument("--out", required=True, type=Path, help="output .unitypackage path")
    ap.add_argument("--asset-prefix", default="Assets/UniVRMA Motion Player",
                    help="destination folder inside the Unity project")
    ap.add_argument("--include", action="append", default=None,
                    help="sub-folder to include (repeatable). Default: Runtime, Editor")
    args = ap.parse_args(argv)

    prefix = args.asset_prefix.strip("/").replace("\\", "/")
    includes = args.include or ["Runtime", "Editor"]
    assets, folders = build(args.package, args.out, prefix, includes)
    size = args.out.stat().st_size
    print(f"[unitypackage] wrote {args.out}")
    print(f"[unitypackage] {assets} assets, {folders} folders, {size / 1024:.1f} KiB")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
