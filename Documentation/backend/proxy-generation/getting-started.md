---
title: Set up proxy generation
description: Connect an existing Arc backend build to a dedicated TypeScript output folder.
---

Start with an Arc project that already defines [commands](../commands/index.md) or [queries](../queries/index.md). This guide adds proxy generation; it does not create or host those endpoints.

## Add the build package

Reference [Cratis.Arc.ProxyGenerator.Build](https://www.nuget.org/packages/Cratis.Arc.ProxyGenerator.Build) in each project whose compiled endpoints you intend to generate. Use a version compatible with your Arc packages and pin it through your normal dependency management.

## Choose a dedicated output folder

Add this MSBuild fragment **inside your existing `.csproj`'s `Project` element**:

```xml
<PropertyGroup>
    <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../Web/src/generated</CratisProxiesOutputPath>
    <CratisProxiesSkipOutputDeletion>true</CratisProxiesSkipOutputDeletion>
</PropertyGroup>
```

The example assumes a sibling `Web` project. Change the path for your layout. Prefer a generated-only directory, with one generation owner, rather than the frontend root. MSBuild defaults to incremental output, but stale generated files are still deleted. [Output behavior](Configuration/output-behavior.md) explains the destructive operations and direct CLI defaults.

## Install frontend dependencies

In your React frontend, install:

```bash
npm install @cratis/arc @cratis/arc.react @cratis/fundamentals react
```

Generated command and query files import **both** Arc core and React hooks unconditionally, even if you only instantiate the classes. Generated model decorators and value types use Fundamentals. Use mutually compatible package versions; `@cratis/arc.react` declares React 18 or 19 as a peer dependency. A browser React application also needs its usual renderer and TypeScript setup.

See [decorator configuration](Configuration/basic.md#decorator-metadata) for compiling generated models. These proxies do not generate a `Bindings` setup file. Wrap React consumers in the [`Arc` provider](../../frontend/react/arc.md).

## Build and inspect the result

From the backend project, run this checkpoint:

```bash
dotnet build -c Debug
```

After compilation, the build target invokes the generator over the assembly. Look for its command/query counts in the build log and `.ts` files under `Web/src/generated`. By default, a query named `AllAuthors` gets `AllAuthors.ts`; it is not automatically placed inside `Author.ts`. Folder paths come from namespaces.

Compile the frontend with its own type-check/build command. This second checkpoint catches missing packages, decorator configuration, and imports that do not match the actual generated paths.

## Keep generation predictable

Commit generated proxies if your workflow consumes them without rebuilding the backend. Incremental generation preserves existing files when their generated content hash matches; full deletion recreates metadata timestamps and can cause Git content churn.

[Source-file grouping](Configuration/basic.md#source-file-as-output-file) is opt-in and depends on per-type PDB information. Prefer Debug for generation. To verify Release without regenerating the same output:

```bash
dotnet build -c Release -p:CratisProxiesOutputPath=
```

The empty property disables the post-build target for that invocation. Release is not intrinsically excluded from generation.

Next, inspect the [generated command](commands.md) and [query](queries.md) APIs, or configure [namespace roots](Configuration/namespace-roots.md).
