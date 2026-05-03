# Configuration

The proxy generator is configured through MSBuild properties and item groups in your `.csproj` file. Configuration is split into the following topics:

- [Basic Options](basic.md) — output path, segments to skip, source file output
- [Library Mode](library-mode.md) — generate TypeScript for every public type in the assembly
- [Type Exclusions](type-exclusions.md) — exclude specific types or namespaces from generation
- [Namespace Roots](namespace-roots.md) — pin a namespace as the folder root
- [Assembly-to-Package Mappings](assembly-package-mappings.md) — import from external npm packages instead of regenerating
- [Routing](routing.md) — control how API routes are built
- [Output Behavior](output-behavior.md) — incremental vs. full regeneration

## Quick Reference

| Property / Item | Default | Topic |
|---|---|---|
| `CratisProxiesOutputPath` | *(required)* | [Basic](basic.md) |
| `CratisProxiesSegmentsToSkip` | `0` | [Basic](basic.md) |
| `CratisProxiesUseSourceFileAsOutputFile` | `false` | [Basic](basic.md) |
| `CratisProxiesLibraryMode` | `false` | [Library Mode](library-mode.md) |
| `<ExcludeType TypeName="..."/>` | — | [Type Exclusions](type-exclusions.md) |
| `<ExcludeNamespace Namespace="..."/>` | — | [Type Exclusions](type-exclusions.md) |
| `<NamespaceRoot Namespace="..."/>` | — | [Namespace Roots](namespace-roots.md) |
| `<AssemblyToPackageMapping .../>` | — | [Assembly-to-Package](assembly-package-mappings.md) |
| `CratisProxiesSkipCommandNameInRoute` | `false` | [Routing](routing.md) |
| `CratisProxiesSkipQueryNameInRoute` | `false` | [Routing](routing.md) |
| `CratisProxiesApiPrefix` | `api` | [Routing](routing.md) |
| `CratisProxiesSkipIndexGeneration` | `false` | [Output Behavior](output-behavior.md) |
| `CratisProxiesSkipOutputDeletion` | `true` | [Output Behavior](output-behavior.md) |
| `CratisProxiesSkipFileIndexTracking` | `false` | [Output Behavior](output-behavior.md) |
