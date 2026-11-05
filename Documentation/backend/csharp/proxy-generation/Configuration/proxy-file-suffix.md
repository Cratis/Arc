---
title: Cratis Arc proxies — add a file suffix
description: Add .proxy to generated TypeScript filenames in Cratis Arc, update direct imports, and understand cleanup when switching an existing project.
---

Cratis Arc can name generated TypeScript files `Name.proxy.ts` so you can distinguish them from handwritten code and match them with a `*.proxy.ts` tooling rule. Enabling the suffix changes filenames and direct import paths; generated imports and `index.ts` exports follow the new names automatically.

By default, each generated file is named after its type, or after its C# source file when `CratisProxiesUseSourceFileAsOutputFile` is on:

```text
Features/Authors/RegisterAuthor.ts
Features/Authors/AllAuthors.ts
```

With [proxy generation configured](../getting-started.md), set `CratisProxiesUseProxyFileSuffix` inside your existing project:

```xml
<PropertyGroup>
    <CratisProxiesUseProxyFileSuffix>true</CratisProxiesUseProxyFileSuffix>
</PropertyGroup>
```

The generated filenames now include the suffix:

```text
Features/Authors/RegisterAuthor.proxy.ts
Features/Authors/AllAuthors.proxy.ts
```

After generation, the files have the `.proxy.ts` names shown above. You can use that pattern in lint rules or coverage exclusions. The suffix identifies generated files, but does not make a mixed output directory safe from cleanup; use a dedicated generated-only directory.

## What changes with it

Generated type files use the suffix, including combined files named after a C# source file. A generated file imports another as `./RegisterAuthor.proxy`. Explicit module paths supplied by type mappings or package mappings stay as mapped, without an added suffix.

A barrel is an `index.ts` file that re-exports a folder's types. Generated barrels keep the name `index.ts`, but their exports use the new filenames, such as `export * from './RegisterAuthor.proxy';`. Your imports through a folder barrel keep the same path. Code that imports a generated file directly must use its suffixed path.

## Switching an existing project

Review [output behavior](output-behavior.md) before switching. Full regeneration deletes the whole output directory, and orphan-directory cleanup can remove an `index.ts` even with incremental output. The suffix does not protect handwritten files from either behavior.

1. Set `CratisProxiesUseProxyFileSuffix` to `true` to add the suffix, or `false` to return to unsuffixed names.
2. Regenerate the proxies. The generator writes the new names and removes files it recognizes as previously generated but no longer produced. This cleanup uses generated-file metadata, not the suffix alone.
3. Update handwritten imports that refer directly to generated files. Use `.proxy` in the module path when the option is on and remove it when the option is off. Leave folder-barrel imports unchanged.

Check that the output contains the expected filenames and that your frontend still resolves its imports. If you need to change the output layout at the same time, configure [namespace roots](namespace-roots.md) separately from the filename suffix.
