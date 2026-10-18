---
title: Basic options
description: Output location, namespace trimming, source-file grouping, and model decorators.
---

The XML blocks on this page are fragments to place inside your existing `.csproj`'s `Project` element.

## Required

```xml
<PropertyGroup>
    <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../Web/src/generated</CratisProxiesOutputPath>
</PropertyGroup>
```

`CratisProxiesOutputPath` is the only required property. It specifies the directory where generated TypeScript files are written — preferably a dedicated generated-only folder, not the frontend root. An empty value disables the post-build target. Review [output behavior](output-behavior.md) before selecting a shared or handwritten source directory.

## Namespace segment skipping

```xml
<PropertyGroup>
    <CratisProxiesSegmentsToSkip>1</CratisProxiesSegmentsToSkip>
</PropertyGroup>
```

`CratisProxiesSegmentsToSkip` controls how many leading namespace segments are stripped when mapping C# namespaces to output folders.

**Example:** With namespaces `Api.MyFeature`, `Domain.MyFeature`, and `Read.MyFeature` and `SegmentsToSkip=1`:

Without skipping:

```text
Api/MyFeature/
Domain/MyFeature/
Read/MyFeature/
```

With skipping:

```text
MyFeature/
```

For more control over namespace-to-folder mapping, see [Namespace Roots](namespace-roots.md).

## Source file as output file

```xml
<PropertyGroup>
    <CratisProxiesUseSourceFileAsOutputFile>true</CratisProxiesUseSourceFileAsOutputFile>
</PropertyGroup>
```

By default, each artifact gets its own file: command and model type names determine their filenames, while queries use method names. When `CratisProxiesUseSourceFileAsOutputFile` is `true`, artifacts whose owning types resolve to the same source filename and output folder are combined into a `.ts` file named after that source file. This does not copy the C# directory tree.

**Illustrative layout:** With namespace `MyApp.Accounts` and one segment skipped, `AccountCommands.cs` containing `CreateAccount`, `UpdateAccount`, and `DeleteAccount`, each with resolvable debug information, generates:

Default:

```text
Accounts/
├── CreateAccount.ts
├── UpdateAccount.ts
└── DeleteAccount.ts
```

With `CratisProxiesUseSourceFileAsOutputFile=true`:

```text
Accounts/
└── AccountCommands.ts
```

> **Note:** The resolver reads an embedded portable PDB or an adjacent portable `.pdb`. Resolution is **per type**, using method debug-document information in the input assembly. Having a PDB is not sufficient for every type: enums and other types without resolvable methods remain in their own files. Types from other assemblies are not guaranteed a source-file mapping. No sibling/namespace guess fills that gap.

Queries use their declaring read-model/controller type's source mapping. The first resolvable method document supplies a type's filename; partial types are not split across files. Source grouping changes filenames, not namespace-derived folders. Avoid unrelated source files with identical basenames in the same output folder, and inspect imports after switching modes.

Generate proxies during development with a Debug build (`dotnet build -c Debug`), then commit the generated TypeScript. Release and publish builds can consume those committed proxies without regenerating them. This is a recommended workflow rather than a Release restriction: the generator still runs in Release whenever `CratisProxiesOutputPath` is configured.

### CLI

With the [executable alias prerequisite](output-behavior.md#direct-executable):

```bash
proxygenerator assembly.dll output-path --skip-output-deletion --use-source-file-as-output-file
```

## File header

Use your application's copyright and license text rather than a framework license. Set `CratisProxiesFileHeader` in the project:

```xml
<PropertyGroup>
    <CratisProxiesFileHeader>Copyright (c) Example Company. All rights reserved.\nGenerated client: {fileName}</CratisProxiesFileHeader>
</PropertyGroup>
```

The generator prefixes each line with `//` followed by a space. Literal `\n`, MSBuild `%0A`, and actual Windows or Unix newlines separate lines. `{fileName}` expands to the output filename, including its extension (for example, `CreateAccount.proxy.ts`). Header text is transported to the generator as an encoded argument, not interpolated into the shell command.

Header selection follows this order:

1. A non-empty `CratisProxiesFileHeader` supplies the template. The special value `none` (case-insensitive) disables the header, including the EditorConfig fallback.
2. An unset or empty property uses `file_header_template` from `.editorconfig`, starting in the C# project directory and walking upward. The nearest applicable value wins; `root = true` stops the search after that file.
3. Without either configuration, no copyright or license header is emitted. The generated-file notice and tracking metadata remain.

For example, this `.editorconfig` also configures the proxy header:

```ini
root = true

[*.{cs,vb}]
file_header_template = Copyright (c) Example Company. All rights reserved.\nAll rights reserved.
```

The fallback honors only `[*]`, `[*.cs]`, and extension-list sections such as `[*.{cs,vb}]` containing the exact `cs` extension. Within one file, the last matching section's value wins. Path-specific patterns and TypeScript-only sections are not used. An empty template or `unset` (case-insensitive) suppresses inherited templates. Projects using Cratis's configured EditorConfig template retain their Cratis header.

The configured header applies to commands, snapshot and observable queries, models, interfaces, enums, flags enums, and newly created `index.ts` files. Grouped output gets one header using the combined filename. Existing index comments and imports are preserved, so changing this setting does not replace headers in existing index files.

## Decorator metadata

Generated types use `@field(...)` property decorators and `@derivedType(...)` class decorators. The decorators keep the runtime serialization metadata beside the type and property they describe, with no proxy-generator configuration required.

TypeScript 5.2 and newer support these decorators through the standard decorator transform. Leave `experimentalDecorators` unset or set it to `false`; `@cratis/fundamentals` consumes the standard decorator metadata when the generated class is defined.

Existing applications can continue using TypeScript's legacy decorator transform with `experimentalDecorators` set to `true`. The generated proxy source is the same in both modes, so you can change compiler modes without regenerating a different proxy shape.

If Babel transforms the generated proxies, configure its decorators plugin for the `2023-11` protocol. Hermes executes the JavaScript that Babel produces; Hermes does not transform decorator syntax itself, so the Babel step must run before the bundle reaches Hermes.
