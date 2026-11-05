---
title: Cratis Arc proxies — generate shared library models
description: Use Cratis Arc library mode to generate TypeScript models that no endpoint references, then choose class or interface output for consumers.
---

Cratis Arc library mode generates TypeScript models from a shared .NET library even when no endpoint references them. Use it to keep those model definitions generated from C# instead of maintaining a separate TypeScript copy.

Normal generation starts with commands, queries and strongly typed identity details, then follows the models they reference. Library mode adds a collection pass over project assemblies; it does not replace endpoint generation.

## Enable library mode

With [proxy generation configured](../getting-started.md), add this fragment inside your existing project:

```xml
<PropertyGroup>
    <CratisProxiesLibraryMode>true</CratisProxiesLibraryMode>
</PropertyGroup>
```

After generation, check the output for a model that no command or query references. The extra collection pass includes public non-abstract types, enums and interfaces. It follows their properties and base classes, so a collected concrete class also brings in its abstract base class.

Open generic definitions and generic type parameters are skipped because they have no concrete shape to emit. Library mode generates model properties and inheritance, not .NET method implementations.

[Exclusions](type-exclusions.md) still apply. Use them to keep implementation types out of a shared package. Types from [package-mapped assemblies](assembly-package-mappings.md) are imported rather than regenerated.

## Default classes versus plain interfaces

Library mode leaves the output format unchanged: models default to TypeScript classes with decorators that supply runtime type information to Cratis Fundamentals. Keep those classes when consumers need model constructors or deserialization metadata.

For consumers that only construct and read plain objects, choose [interface output](emit-interfaces.md) with `CratisProxiesEmitInterfaces` or the executable's `--emit-interfaces` flag. Interfaces have no runtime constructors or deserialization metadata, so they cannot replace model constructors in query or identity clients. The flag changes model rendering, not the command and query templates or their runtime dependencies.

## Combine output controls

To limit the shared models and choose where they appear, combine library mode with exclusions and namespace roots. This fragment excludes `MyApp.Internal*` and strips `MyApp.Models` without adding a base folder:

```xml
<PropertyGroup>
    <CratisProxiesLibraryMode>true</CratisProxiesLibraryMode>
</PropertyGroup>

<ItemGroup>
    <ExcludeNamespace Include="internal" Namespace="MyApp.Internal*" />
    <NamespaceRoot Include="models" Namespace="MyApp.Models" Folder="" />
</ItemGroup>
```

With `Folder=""`, a model in `MyApp.Models.Billing` is written under `Billing/`. `Include` identifies the MSBuild item; the generator uses its `Namespace` and `Folder` metadata for the mapping. If your library needs several output roots, use the matching rules in [namespace roots](namespace-roots.md).
