---
title: Proxy generator configuration
description: MSBuild properties, mapping items, defaults, and CLI differences.
---

Configure generation through properties and items **inside the existing `.csproj`'s `Project` element**. The examples in these reference pages are project fragments, not standalone project files. Item `Include` values supply MSBuild identities; the generator consumes each item's named metadata.

## Choose a topic

- [Basic options](basic.md): output path, namespace segments, source-file grouping, decorators.
- [Library mode](library-mode.md): collect models beyond endpoint references; separate CLI interface output.
- [Type exclusions](type-exclusions.md): omit types or namespace patterns.
- [Namespace roots](namespace-roots.md): remap output folders without changing API routes.
- [Assembly-to-package mappings](assembly-package-mappings.md): import shared model packages.
- [Type mapping](../type-mapping.md): primitive mappings and `TypeToTsType` overrides.
- [Routing](routing.md): conventional model-bound route options and runtime alignment.
- [Output behavior](output-behavior.md): incremental writes, full deletion, and cleanup limitations.

## MSBuild reference

| Property or item | Default / required metadata | Topic |
| --- | --- | --- |
| `CratisProxiesOutputPath` | Empty disables generation | [Basic](basic.md) |
| `CratisProxiesSegmentsToSkip` | Empty resolves to `0` | [Basic](basic.md) |
| `CratisProxiesUseSourceFileAsOutputFile` | Off unless `true` | [Basic](basic.md) |
| `CratisProxiesLibraryMode` | `false` | [Library mode](library-mode.md) |
| `ExcludeType` item | `Include`, `TypeName` | [Exclusions](type-exclusions.md) |
| `ExcludeNamespace` item | `Include`, `Namespace` | [Exclusions](type-exclusions.md) |
| `NamespaceRoot` item | `Include`, `Namespace`, `Folder` (may be empty) | [Namespace roots](namespace-roots.md) |
| `AssemblyToPackageMapping` item | `Include`, `Assembly`, `Package` | [Package mappings](assembly-package-mappings.md) |
| `TypeToTsType` item | `Include`, `TypeName`, `TsType`; optional `Package` | [Type mapping](../type-mapping.md) |
| `CratisProxiesSkipCommandNameInRoute` | `false` | [Routing](routing.md) |
| `CratisProxiesSkipQueryNameInRoute` | `false` | [Routing](routing.md) |
| `CratisProxiesApiPrefix` | `api` | [Routing](routing.md) |
| `CratisProxiesSkipIndexGeneration` | Off unless `true` | [Output behavior](output-behavior.md) |
| `CratisProxiesSkipOutputDeletion` | `true` in MSBuild; direct executable differs | [Output behavior](output-behavior.md) |
| `CratisProxiesSkipFileIndexTracking` | `false`; setting `true` is currently ignored by the executable | [Output behavior](output-behavior.md) |

The executable accepts `--emit-interfaces` separately; the current MSBuild target does not expose that switch. See [library mode](library-mode.md#default-classes-versus-plain-interfaces).
