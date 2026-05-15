# Namespace Roots

A namespace root pins a namespace as the base of the output folder hierarchy and places its output under a named base folder. When a type's namespace begins with a configured root, the root prefix is stripped and the remainder is placed under the specified folder.

This is an alternative to [`CratisProxiesSegmentsToSkip`](basic.md#namespace-segment-skipping) that works by name rather than by a fixed segment count, making it more resilient when namespace depths vary across a project.

## Configuration

```xml
<ItemGroup>
    <NamespaceRoot Namespace="MyApp.Features" Folder="features" />
</ItemGroup>
```

| Attribute | Description |
|---|---|
| `Namespace` | The C# namespace prefix to match |
| `Folder` | The output sub-folder to place matching types under |

**Example:** With namespace root `MyApp.Features` → `features`:

| C# namespace | Output folder |
|---|---|
| `MyApp.Features.Auth.Registration` | `features/Auth/Registration/` |
| `MyApp.Features.Billing.Invoices` | `features/Billing/Invoices/` |
| `MyApp.Features.Auth` | `features/Auth/` |
| `MyApp.Features` | `features/` |
| `MyApp.Other` | *(falls back to segment-skip logic — namespace root does not apply)* |

## Multiple Roots

You can declare multiple roots. The longest matching namespace wins:

```xml
<ItemGroup>
    <NamespaceRoot Namespace="MyApp.Features"    Folder="features" />
    <NamespaceRoot Namespace="MyApp.SharedTypes" Folder="shared" />
</ItemGroup>
```

## Priority

Namespace roots **only take effect for types whose namespace matches the root**. Types that do not match any configured root fall back to `CratisProxiesSegmentsToSkip` as normal.

## CLI

Pass one or more `--namespace-root` flags using `=` to separate the namespace from the folder:

```bash
proxygenerator assembly.dll output-path \
  --namespace-root=MyApp.Features=features \
  --namespace-root=MyApp.SharedTypes=shared
```
