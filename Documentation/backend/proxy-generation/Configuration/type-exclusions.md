# Type Exclusions

You can prevent specific types or entire namespaces from appearing in the generated TypeScript output by declaring `ExcludeType` or `ExcludeNamespace` item groups.

Exclusions apply to:

- Commands and queries (excluded types are not generated as proxy classes)
- Types collected transitively from command/query properties
- All types collected in [library mode](library-mode.md)

## Excluding a Specific Type

Use `ExcludeType` with the fully qualified C# type name (namespace + class name):

```xml
<ItemGroup>
    <ExcludeType TypeName="MyApp.Internal.SecretPayload" />
    <ExcludeType TypeName="MyApp.Infrastructure.InfrastructureContext" />
</ItemGroup>
```

## Excluding an Entire Namespace

Use `ExcludeNamespace` with a glob pattern. The `*` wildcard matches any sequence of characters:

```xml
<ItemGroup>
    <!-- All types whose namespace starts with MyApp.Internal -->
    <ExcludeNamespace Namespace="MyApp.Internal*" />

    <!-- All types whose namespace starts with MyApp.Tests -->
    <ExcludeNamespace Namespace="MyApp.Tests*" />
</ItemGroup>
```

### Glob Pattern Rules

| Pattern | Matches |
|---|---|
| `MyApp.Internal*` | Any namespace that begins with `MyApp.Internal` |
| `MyApp.*.Internal` | Any namespace matching that structure exactly (e.g. `MyApp.Foo.Internal`) |
| `MyApp.Internal` | The exact namespace `MyApp.Internal` only |

The `*` wildcard matches any sequence of characters including `.`, so `MyApp*` matches `MyApp`, `MyApp.Features`, `MyApp.Features.Auth`, etc.

## Combining Both

You can freely mix `ExcludeType` and `ExcludeNamespace` in the same item group:

```xml
<ItemGroup>
    <ExcludeType TypeName="MyApp.Features.Auth.InternalToken" />
    <ExcludeNamespace Namespace="MyApp.Internal*" />
    <ExcludeNamespace Namespace="MyApp.Tests*" />
</ItemGroup>
```

## CLI

Pass one or more `--exclude-type` and/or `--exclude-namespace` flags:

```bash
proxygenerator assembly.dll output-path \
  --exclude-type=MyApp.Internal.SecretPayload \
  --exclude-namespace=MyApp.Tests*
```
