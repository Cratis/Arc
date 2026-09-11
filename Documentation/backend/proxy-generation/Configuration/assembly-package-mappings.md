---
title: Assembly-to-package mappings
description: Import external model types from an existing npm package.
---

When your project references types from an external assembly that already has a corresponding TypeScript npm package, you can tell the proxy generator to import those types from the package instead of regenerating them locally.

## Use case

Consider this solution structure:

```text
MyCompany.Shared/           ← Shared class library + npm package (@mycompany/shared)
MyCompany.Inventory/        ← Application referencing Shared
MyCompany.Purchasing/       ← Application referencing Shared
```

Without a mapping, the proxy generator would regenerate `Money.ts` and `ProductSummary.ts` inside each application's frontend — duplicating types that already exist in `@mycompany/shared`. With a mapping, those types are imported from the package:

```typescript
import { Money, ProductSummary } from '@mycompany/shared';
```

## Configuration

```xml
<ItemGroup>
    <AssemblyToPackageMapping Include="shared" Assembly="MyCompany.Shared" Package="@mycompany/shared" />
</ItemGroup>
```

Multiple shared libraries:

```xml
<ItemGroup>
    <AssemblyToPackageMapping Include="shared" Assembly="MyCompany.Shared" Package="@mycompany/shared" />
    <AssemblyToPackageMapping Include="ui-models" Assembly="MyCompany.UiModels" Package="@mycompany/ui-models" />
</ItemGroup>
```

| Attribute  | Description                                           |
| ---------- | ----------------------------------------------------- |
| `Include`  | MSBuild item identity; not the assembly mapping value |
| `Assembly` | C# assembly name (without `.dll` extension)           |
| `Package`  | npm package name to import from                       |

## Behavior

- Types from the mapped assembly are **not** generated as local TypeScript files.
- Any command, query, or type that references a mapped type imports it from the configured package.
- The mapping covers generated model types and enums in the assembly. Concepts and built-in primitive mappings still follow [type mapping](../type-mapping.md); a concept ID is not automatically a separately emitted class.
- Install the target npm package separately. It must export compatible types and, where deserialization needs them, runtime constructors. The generator does not build or publish that package.

The XML blocks are fragments inside your existing `Project` element; the TypeScript import is illustrative of package output.

## CLI

With the [executable alias prerequisite](output-behavior.md#direct-executable):

```bash
proxygenerator MyCompany.Inventory.dll output-path --skip-output-deletion \
  --assembly-to-package=MyCompany.Shared=@mycompany/shared \
  --assembly-to-package=MyCompany.UiModels=@mycompany/ui-models
```
