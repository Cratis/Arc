# Assembly-to-Package Mappings

When your project references types from an external assembly that already has a corresponding TypeScript npm package, you can tell the proxy generator to import those types from the package instead of regenerating them locally.

## Use Case

Consider this solution structure:

```text
MyCompany.Shared/           ← Shared class library + npm package (@mycompany/shared)
MyCompany.Inventory/        ← Application referencing Shared
MyCompany.Purchasing/       ← Application referencing Shared
```

Without a mapping, the proxy generator would regenerate `Money.ts` and `ProductId.ts` inside each application's frontend — duplicating types that already exist in `@mycompany/shared`. With a mapping, those types are imported from the package:

```typescript
import { Money, ProductId } from '@mycompany/shared';
```

## Configuration

```xml
<ItemGroup>
    <AssemblyToPackageMapping Assembly="MyCompany.Shared" Package="@mycompany/shared" />
</ItemGroup>
```

Multiple shared libraries:

```xml
<ItemGroup>
    <AssemblyToPackageMapping Assembly="MyCompany.Shared"   Package="@mycompany/shared" />
    <AssemblyToPackageMapping Assembly="MyCompany.UiModels" Package="@mycompany/ui-models" />
</ItemGroup>
```

| Attribute | Description |
|---|---|
| `Assembly` | C# assembly name (without `.dll` extension) |
| `Package` | npm package name to import from |

## Behavior

- Types from the mapped assembly are **not** generated as local TypeScript files.
- Any command, query, or type that references a mapped type imports it from the configured package.
- The mapping applies to all types in the assembly — classes, records, enums, and interfaces.

## CLI

```bash
proxygenerator MyCompany.Inventory.dll output-path \
  --assembly-to-package=MyCompany.Shared=@mycompany/shared \
  --assembly-to-package=MyCompany.UiModels=@mycompany/ui-models
```
