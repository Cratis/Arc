# ArcCore TestApp

This folder contains the `ArcCore` test application and its frontend.

## AOT publish script

The local `package.json` includes a script for publishing with Native AOT, speed optimization, and mangled type names:

```bash
yarn publish:aot:mangled
```

This runs:

```bash
dotnet restore ArcCore.csproj -r osx-arm64 && dotnet publish ArcCore.csproj -c Release -r osx-arm64 /p:PublishAot=true /p:SelfContained=true /p:OptimizationPreference=Speed /p:IlcGenerateMangledNames=true /p:EnableAotAnalyzer=false /p:EnableTrimAnalyzer=false /p:EnableSingleFileAnalyzer=false /p:EnableConfigurationBindingGenerator=false --no-restore
```

## What it enables

- Native AOT publish
- Self-contained publish
- Speed-focused optimization
- Mangled generated type names
- Analyzer and configuration-binding suppressions needed by the current Arc.Core AOT test surface

The project file in `ArcCore.csproj` also carries the same properties so the app is configured consistently for publish.

## Current caveat

The test app currently carries targeted suppressions for trim and AOT warnings coming from dependencies and Arc.Core reflection-heavy code paths. That is acceptable for this isolated NativeAOT test bed, but it is not a signal that Arc.Core is fully annotated for general NativeAOT consumption.
