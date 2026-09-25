---
title: Type exclusions
description: Omit selected types and namespaces from proxy output.
---

Exclusions apply to command/query descriptor types, transitively collected models, and [library-mode](library-mode.md) types. They control generation, **not backend authorization or endpoint exposure**. Ensure remaining proxies do not reference an excluded type without a suitable replacement mapping.

## Exclude specific types

Place this fragment inside your existing project's `Project` element:

```xml
<ItemGroup>
    <ExcludeType Include="secret-payload" TypeName="MyApp.Internal.SecretPayload" />
    <ExcludeType Include="infrastructure-context" TypeName="MyApp.Infrastructure.InfrastructureContext" />
</ItemGroup>
```

`Include` supplies the MSBuild item identity. `TypeName` supplies the fully qualified C# type name the generator matches. Query exclusion is based on the descriptor's owning type, not an individual method name.

## Exclude namespaces

You can combine type exclusions and namespace patterns in the same item group:

```xml
<ItemGroup>
    <ExcludeType Include="internal-token" TypeName="MyApp.Auth.InternalToken" />
    <ExcludeNamespace Include="internal" Namespace="MyApp.Internal*" />
    <ExcludeNamespace Include="tests" Namespace="MyApp.Tests*" />
</ItemGroup>
```

The generator reads `Namespace` metadata, not the `Include` identity.

| Pattern            | Matches                                                             |
| ------------------ | ------------------------------------------------------------------- |
| `MyApp.Internal*`  | Any namespace starting with `MyApp.Internal`                        |
| `MyApp.*.Internal` | Namespaces such as `MyApp.Foo.Internal` or `MyApp.Foo.Bar.Internal` |
| `MyApp.Internal`   | The exact namespace only                                            |

`*` matches any sequence of characters, including dots. Be deliberate: `MyApp.Internal*` also matches `MyApp.InternalTools`.

## CLI

With the [executable alias prerequisite](output-behavior.md#direct-executable), use repeatable flags. Quote wildcards so the shell does not expand them:

```bash
proxygenerator assembly.dll output-path --skip-output-deletion \
  --exclude-type=MyApp.Internal.SecretPayload \
  '--exclude-namespace=MyApp.Tests*'
```
