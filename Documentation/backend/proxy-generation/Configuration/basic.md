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

## Metadata Registration

For web projects, keep the default output: generated types use `@field(...)` property decorators and `@derivedType(...)` class decorators. This keeps the serialization metadata beside the type and property it describes.

If your frontend toolchain does not support the legacy decorator transforms, opt into explicit metadata registration:

```xml
<PropertyGroup>
    <CratisProxiesUseExplicitMetadataRegistration>true</CratisProxiesUseExplicitMetadataRegistration>
</PropertyGroup>
```

The generated class properties no longer carry decorators. Instead, the generator emits ordinary registration calls after the class declaration:

```typescript
import { field } from '@cratis/fundamentals';

export class Location {
    longitude!: number;
}

field(Number)(Location.prototype, 'longitude');
```

Derived types use the equivalent `derivedType(...)(Type)` call. The metadata remains the same, but the generated file no longer requires legacy decorator transforms. This mode is useful for React Native and Expo applications running on Hermes, where those transforms may be unavailable or order-sensitive.

When invoking the proxy generator CLI directly, add the corresponding flag:

```bash
proxygenerator assembly.dll output-path --use-explicit-metadata-registration
```
