---
title: Library mode
description: Generate a shared model surface beyond endpoint-referenced types.
---

Normal generation starts with endpoints and strongly typed identity details, then follows their referenced models. Library mode also collects types that no endpoint uses, so a shared library can expose its model surface to TypeScript consumers.

## Enable library mode

Add this fragment inside your existing project:

```xml
<PropertyGroup>
    <CratisProxiesLibraryMode>true</CratisProxiesLibraryMode>
</PropertyGroup>
```

The collection pass walks project assemblies, including public non-abstract types, enums, and interfaces. Open generic definitions are skipped, along with their type parameters: neither has a concrete shape to emit. Abstract classes are not collected directly, but are still generated when something reachable derives from one — a concrete type's base class is followed, so an inheritance chain arrives intact rather than with its roots missing. This is model generation, not a translation of arbitrary .NET methods or implementations.

[Exclusions](type-exclusions.md) still apply, and [package-mapped types](assembly-package-mappings.md) are imported rather than regenerated.

## Default classes versus plain interfaces

Library mode does **not** imply interface output. Models default to TypeScript classes with decorators and runtime Fundamentals metadata, just as in ordinary proxy generation.

The executable separately accepts `--emit-interfaces` for shape-only model output. Interfaces have no runtime constructors or deserialization metadata; use this for types you construct/read as plain objects, not as a drop-in replacement for model constructors in query or identity clients. Command/query templates are not converted into dependency-free interfaces by this flag.

See [emit interfaces](emit-interfaces.md) for when to use interface output and how to enable it via the MSBuild property or CLI flag.

## Combine output controls

This project fragment excludes implementation namespaces and intentionally strips `MyApp.Models` without adding a base folder:

```xml
<PropertyGroup>
    <CratisProxiesLibraryMode>true</CratisProxiesLibraryMode>
</PropertyGroup>

<ItemGroup>
    <ExcludeNamespace Include="internal" Namespace="MyApp.Internal*" />
    <NamespaceRoot Include="models" Namespace="MyApp.Models" Folder="" />
</ItemGroup>
```

`Folder=""` is valid: `MyApp.Models.Billing` maps to `Billing/`. `Include` is the MSBuild item identity; `Namespace` and `Folder` are the generator's metadata. See [namespace roots](namespace-roots.md) for matching rules.
