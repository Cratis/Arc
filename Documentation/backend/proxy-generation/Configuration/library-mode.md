# Library Mode

By default, the proxy generator only produces TypeScript for types that are **directly used** by commands and queries — types referenced as properties or return values flow in transitively. Any other public types in your assembly are ignored.

**Library mode** generates TypeScript for **every public type** in the assembly, regardless of whether it appears in any command or query. This is useful for shared libraries or packages where the TypeScript consumer needs the full type surface.

## Enabling Library Mode

```xml
<PropertyGroup>
    <CratisProxiesLibraryMode>true</CratisProxiesLibraryMode>
</PropertyGroup>
```

### CLI

```bash
proxygenerator assembly.dll output-path --library-mode
```

## Behavior

When library mode is on:

- All public, non-abstract classes and records in every project assembly are collected and generated as TypeScript interfaces.
- All public interfaces are included.
- All public enums are included.
- Abstract classes are skipped (they cannot be instantiated).
- Types excluded via [`ExcludeType` or `ExcludeNamespace`](type-exclusions.md) are still skipped.
- Types from assemblies mapped via [`AssemblyToPackageMapping`](assembly-package-mappings.md) are still imported from their package rather than regenerated.

## Combining with Other Options

Library mode pairs naturally with type exclusions and namespace roots:

```xml
<PropertyGroup>
    <CratisProxiesLibraryMode>true</CratisProxiesLibraryMode>
</PropertyGroup>

<ItemGroup>
    <!-- Exclude internal implementation types -->
    <ExcludeNamespace Namespace="MyApp.Internal*" />

    <!-- Root the output at the feature namespace -->
    <NamespaceRoot Namespace="MyApp.Features" />
</ItemGroup>
```
