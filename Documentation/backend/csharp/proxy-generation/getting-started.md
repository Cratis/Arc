---
title: Cratis Arc proxy generation — set up TypeScript clients
description: Set up Cratis Arc proxy generation to turn compiled C# endpoints into TypeScript clients, with a dedicated output folder and frontend dependencies.
---

Cratis Arc proxy generation adds TypeScript clients to an existing backend build so your frontend can use the contracts defined in C#. Start with a project that already defines [commands](../commands/index.md) or [queries](../queries/index.md); the generator does not create or host those endpoints.

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

The example assumes a sibling `Web` project. Change the path for your layout, but keep a generated-only directory owned by one backend project's generation step. Do not point it at the frontend root or share it between independent generation runs: one run can remove another's output as stale.

MSBuild preserves the output directory by default, but generation still deletes stale generated files. Setting `CratisProxiesSkipOutputDeletion` to `false` deletes the entire output directory before writing. Read [output behavior](Configuration/output-behavior.md) before changing this setting or using the executable directly, which has different defaults.

## Install frontend dependencies

In your React frontend, install:

```bash
npm install @cratis/arc @cratis/arc.react @cratis/fundamentals react
```

The default React output imports both Arc core and React hooks, even if you only instantiate the classes. Generated model decorators and value types use Fundamentals. Use mutually compatible package versions; `@cratis/arc.react` declares React 18 or 19 as a peer dependency. A browser React application also needs its usual renderer and TypeScript setup.

For clients without React hooks or React imports, set `CratisProxiesFramework` to `none`. That changes command and query output, not the model decorators or their Fundamentals dependency.

See [decorator configuration](Configuration/basic.md#decorator-metadata) for compiling generated models. These proxies do not generate a `Bindings` setup file. Wrap React consumers in the [`Arc` provider](../../../frontend/react/arc.md).

## Build and inspect the result

From the backend project, build the assembly and generate its clients:

```bash
dotnet build -c Debug
```

After compilation, the build target invokes the generator over the assembly. Look for `.ts` files under `Web/src/generated` and command/query counts in the generation log. An incremental run can skip generation when its inputs and tracked outputs are unchanged. By default, a query named `AllAuthors` gets `AllAuthors.ts`; it is not automatically placed inside `Author.ts`. Namespaces determine the output folders.

Compile the frontend with its own type-check/build command. This second checkpoint catches missing packages, decorator configuration, and imports that do not match the actual generated paths.

## Keep generation predictable

Treat generated files as output, not application source to edit by hand. Change the C# contract or generator configuration, regenerate, then compile the frontend against the result. A handwritten fix in a generated file can be lost when that file is rewritten.

Commit generated proxies if your workflow consumes them without rebuilding the backend. Incremental generation preserves existing files when their generated content hash matches; full deletion recreates metadata timestamps and can cause Git content churn.

[Source-file grouping](Configuration/basic.md#source-file-as-output-file) is opt-in and depends on per-type PDB information. Prefer Debug for generation. To verify Release without regenerating the same output:

```bash
dotnet build -c Release -p:CratisProxiesOutputPath=
```

The empty property disables the post-build target for that invocation. Release is not intrinsically excluded from generation.

With the frontend compiling, use the [generated command](commands.md) and [query](queries.md) APIs in your application. If the output folders do not match your layout, configure [namespace roots](Configuration/namespace-roots.md) before adding imports throughout the frontend.
