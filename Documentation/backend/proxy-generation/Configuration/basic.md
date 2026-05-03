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

```
Api/MyFeature/
Domain/MyFeature/
Read/MyFeature/
```

With skipping:

```
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

```
AccountCommands/
├── CreateAccount.ts
├── UpdateAccount.ts
└── DeleteAccount.ts
```

With `CratisProxiesUseSourceFileAsOutputFile=true`:

```
AccountCommands/
└── AccountCommands.ts
```

> **Note:** This feature requires PDB debug symbols alongside the compiled assembly. Without PDB information the generator falls back to one file per type.

### CLI

```bash
proxygenerator assembly.dll output-path --use-source-file-as-output-file
```
