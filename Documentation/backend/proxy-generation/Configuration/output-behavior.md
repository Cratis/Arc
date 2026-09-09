---
title: Output behavior
description: Incremental writes, destructive cleanup, and barrel generation settings.
---

## Defaults by entry point

| Entry point               | Output deletion default                                                               |
| ------------------------- | ------------------------------------------------------------------------------------- |
| NuGet MSBuild integration | `CratisProxiesSkipOutputDeletion=true`: retain the directory and update incrementally |
| Direct executable         | Deletes the output directory unless `--skip-output-deletion` is supplied              |

Use a dedicated, generated-only directory with one generation owner. Incremental mode does **not** disable orphan cleanup or guarantee preservation of a mixed source tree.

## Incremental generation

This fragment belongs inside your existing project:

```xml
<PropertyGroup>
    <CratisProxiesSkipOutputDeletion>true</CratisProxiesSkipOutputDeletion>
</PropertyGroup>
```

The generator compares generated content hashes with inline metadata on existing files. A matching hash lets it skip the write and preserve both filesystem and inline timestamps. Renamed or removed artifacts are still cleaned up by [file tracking](../file-index-tracking.md).

Do not edit generated files manually. The metadata/hash shortcut is not a merge tool, and changes to generated content can be overwritten on later builds.

## Full regeneration

To rebuild a **disposable output directory** from scratch:

```xml
<PropertyGroup>
    <CratisProxiesSkipOutputDeletion>false</CratisProxiesSkipOutputDeletion>
</PropertyGroup>
```

:::caution
This recursively deletes the entire output directory, including handwritten files and indexes, before writing proxies. Never point this mode at your frontend source root.
:::

Generated metadata embeds the generation time in file content. Deleting existing files prevents the hash shortcut from preserving that timestamp, so unchanged backend contracts can still produce Git diffs on regeneration.

## Index file generation

By default, the generator manages `index.ts` exports for generated files. Disable that update phase with:

```xml
<PropertyGroup>
    <CratisProxiesSkipIndexGeneration>true</CratisProxiesSkipIndexGeneration>
</PropertyGroup>
```

This skips barrel creation/update, **not deletion**. Full output deletion still removes indexes, and orphan-directory cleanup can delete an `index.ts` even when this property is true. Keep a handwritten public barrel outside the generator's output tree if you need ownership isolation.

## File tracking limitation

`CratisProxiesSkipFileIndexTracking` is still exposed by the build package and forwards `--skip-file-index-tracking`. The current executable **does not parse that flag**. Setting it to `true` does not disable metadata scanning or stale-file deletion. There is no supported orphan-cleanup opt-out in the current executable.

The old JSON file index and `--project-directory` setting are not used by current tracking. See [file tracking and preservation limits](../file-index-tracking.md) before changing cleanup settings.

## Direct executable

CLI examples in this section assume `proxygenerator` is an application-provided alias or wrapper for the packaged .NET executable; the build package does not install that shell command. The first arguments are the compiled assembly path, output path, and optional namespace segment count.

```bash
proxygenerator assembly.dll output-path --skip-output-deletion --skip-index-generation
```

This retains the output directory and skips barrel updates, but still removes orphaned marked files. Use only a disposable or generator-owned `output-path`.
