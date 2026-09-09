---
title: Namespace roots
description: Map namespace prefixes to generated output folders.
---

A namespace root pins a namespace as the base of the output folder hierarchy and places its output under a named base folder. When a type's namespace equals a configured root or starts with that root followed by a dot, the root prefix is stripped and the remainder is placed under the specified folder.

This is an alternative to [`CratisProxiesSegmentsToSkip`](basic.md#namespace-segment-skipping) that works by name rather than by a fixed segment count, making it more resilient when namespace depths vary across a project.

## Configuration

```xml
<ItemGroup>
    <NamespaceRoot Include="features" Namespace="MyApp.Features" Folder="features" />
</ItemGroup>
```

| Attribute   | Description                                                                                          |
| ----------- | ---------------------------------------------------------------------------------------------------- |
| `Include`   | MSBuild item identity; the generator reads the metadata below, not this name                         |
| `Namespace` | The C# namespace prefix to match                                                                     |
| `Folder`    | Output base folder; an empty value intentionally strips the namespace prefix without adding a folder |

**Example:** With namespace root `MyApp.Features` → `features`:

| C# namespace                       | Output folder                                                        |
| ---------------------------------- | -------------------------------------------------------------------- |
| `MyApp.Features.Auth.Registration` | `features/Auth/Registration/`                                        |
| `MyApp.Features.Billing.Invoices`  | `features/Billing/Invoices/`                                         |
| `MyApp.Features.Auth`              | `features/Auth/`                                                     |
| `MyApp.Features`                   | `features/`                                                          |
| `MyApp.Other`                      | _(falls back to segment-skip logic — namespace root does not apply)_ |

## Multiple roots

You can declare multiple roots. The longest matching namespace wins:

```xml
<ItemGroup>
    <NamespaceRoot Include="features" Namespace="MyApp.Features" Folder="features" />
    <NamespaceRoot Include="shared" Namespace="MyApp.SharedTypes" Folder="shared" />
</ItemGroup>
```

## Priority

Namespace roots **only take effect for types whose namespace matches the root**. Types that do not match any configured root fall back to `CratisProxiesSegmentsToSkip` as normal.

These XML blocks belong inside the existing project's `Project` element. Namespace roots affect **output folders only**, not conventional API routes.

## CLI

With the [executable alias prerequisite](output-behavior.md#direct-executable), pass one or more `--namespace-root` flags using `=` to separate the namespace from the folder:

```bash
proxygenerator assembly.dll output-path --skip-output-deletion \
  --namespace-root=MyApp.Features=features \
  --namespace-root=MyApp.SharedTypes=shared
```
