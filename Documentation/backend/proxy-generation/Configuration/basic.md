# Basic Options

## Required

```xml
<PropertyGroup>
    <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../Web</CratisProxiesOutputPath>
</PropertyGroup>
```

`CratisProxiesOutputPath` is the only required property. It specifies the directory where generated TypeScript files are written — typically the root of your frontend project.

## Namespace Segment Skipping

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

## Source File as Output File

```xml
<PropertyGroup>
    <CratisProxiesUseSourceFileAsOutputFile>true</CratisProxiesUseSourceFileAsOutputFile>
</PropertyGroup>
```

By default, one TypeScript file is generated per C# type. When `CratisProxiesUseSourceFileAsOutputFile` is `true`, all types defined in the same `.cs` source file are combined into a single `.ts` file named after the source file.

**Example:** `AccountCommands.cs` containing `CreateAccount`, `UpdateAccount`, `DeleteAccount` generates:

Default:

```text
AccountCommands/
├── CreateAccount.ts
├── UpdateAccount.ts
└── DeleteAccount.ts
```

With `CratisProxiesUseSourceFileAsOutputFile=true`:

```text
AccountCommands/
└── AccountCommands.ts
```

> **Note:** This feature requires PDB debug symbols alongside the compiled assembly. Without PDB information the generator falls back to one file per type.

Generate proxies during development with a Debug build (`dotnet build -c Debug`), then commit the generated TypeScript. Release and publish builds can consume those committed proxies without regenerating them. This is a recommended workflow rather than a Release restriction: the generator still runs in Release whenever `CratisProxiesOutputPath` is configured.

### CLI

```bash
proxygenerator assembly.dll output-path --use-source-file-as-output-file
```

## Decorator Metadata

Generated types use `@field(...)` property decorators and `@derivedType(...)` class decorators. The decorators keep the runtime serialization metadata beside the type and property they describe, with no proxy-generator configuration required.

TypeScript 5.2 and newer support these decorators through the standard decorator transform. Leave `experimentalDecorators` unset or set it to `false`; `@cratis/fundamentals` consumes the standard decorator metadata when the generated class is defined.

Existing applications can continue using TypeScript's legacy decorator transform with `experimentalDecorators` set to `true`. The generated proxy source is the same in both modes, so you can change compiler modes without regenerating a different proxy shape.

If Babel transforms the generated proxies, configure its decorators plugin for the `2023-11` protocol. Hermes executes the JavaScript that Babel produces; Hermes does not transform decorator syntax itself, so the Babel step must run before the bundle reaches Hermes.
