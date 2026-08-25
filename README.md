# Cratis Arc

Arc is an opinionated CQRS application framework for ASP.NET Core with commands, queries, validation, authorization, and TypeScript proxy generation.

Arc exposes recognized command and query contracts through ASP.NET Core and can generate TypeScript client code for those artifacts.

[![NuGet](https://img.shields.io/nuget/v/Cratis.Arc?logo=nuget)](https://www.nuget.org/packages/Cratis.Arc)
[![NPM](https://img.shields.io/npm/v/@cratis/arc?label=@cratis/arc&logo=npm)](https://www.npmjs.com/package/@cratis/arc)
[![.NET Build](https://github.com/Cratis/Arc/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/Cratis/Arc/actions/workflows/dotnet-build.yml)
[![JavaScript Build](https://github.com/Cratis/Arc/actions/workflows/javascript-build.yml/badge.svg)](https://github.com/Cratis/Arc/actions/workflows/javascript-build.yml)

## Start here

- [Browse the canonical Arc documentation](https://cratis.io/arc/)
- [Understand Arc's independent boundary](#arc-does-not-require-event-sourcing)
- [Start an Arc host](#start-an-arc-host)
- [Inspect packages and repository layout](#packages-and-repository-layout)

## What Arc owns

| Boundary | Arc provides |
| --- | --- |
| Commands | Explicit application intentions, validation, authorization, execution, and generated HTTP endpoints |
| Queries | Purpose-shaped reads exposed through model-bound or controller-based query surfaces |
| Generated contracts | TypeScript proxies for recognized command and query artifacts |
| ASP.NET Core integration | Hosting, dependency injection, identity, tenancy, OpenAPI, and application conventions |
| Frontend packages | TypeScript runtime support for generated proxies plus React and React/MVVM integration packages |
| Persistence integration | Separate integrations for current-state persistence and optional Chronicle-backed behavior |

Detailed API, configuration, provider, and frontend documentation lives in the canonical documentation. This README is the shortest path into that documentation rather than a second manual.

## Arc does not require event sourcing

Arc.Core does not depend on Chronicle. Commands and queries can use current-state persistence or application services without an event log. The owning repository and canonical Arc page preserve that boundary explicitly.

The Chronicle integration is optional and supplies event-sourced behavior when configured. Arc retains its command, query, validation, authorization, and generated-contract boundary.

## Relationship to Components

Components is a React component library aligned with Arc application patterns. Its exact component and integration surface is documented by the [Components product documentation](https://cratis.io/components/).

Applications may use Arc without Components and remain responsible for their own frontend, accessibility, browser, and design-system verification.

## Start an Arc host

Arc's .NET packages embed this product-family README. The example below uses the umbrella `Cratis.Arc` package to start an Arc host; when viewing this README from a specialized package page, use that package's manifest and reference documentation for its own installation scope.

The current Arc analyzers and source generators require .NET SDK 10.0.301 or newer. Install the host package:

```bash
dotnet add package Cratis.Arc
```

Create and run an Arc host:

```csharp
using Cratis.Arc;

var builder = ArcApplication.CreateBuilder(args);
builder.AddCratisArc();

var app = builder.Build();
app.UseCratisArc();
await app.RunAsync();
```

Starting this host proves the Arc application setup can build and run on the selected SDK/profile. Continue from the [canonical Arc page](https://cratis.io/arc/) for the currently admitted documentation profile, and use [GitHub Issues](https://github.com/Cratis/Arc/issues) when observed behavior does not match it.

## Packages and repository layout

| Surface | Role |
| --- | --- |
| `Cratis.Arc` / `Cratis.Arc.Core` | .NET command, query, hosting, validation, authorization, and generation surfaces |
| `Cratis.Arc.ProxyGenerator.Build` | Build-time TypeScript proxy generation for recognized application artifacts |
| `@cratis/arc` | TypeScript runtime support consumed by application-specific generated command and query proxies |
| `@cratis/arc.react` | React hooks and compositions for Arc contracts |
| `@cratis/arc.react.mvvm` | React/MVVM integration for Arc contracts |
| [`Source/DotNET`](https://github.com/Cratis/Arc/tree/main/Source/DotNET) | .NET framework source |
| [`Source/JavaScript`](https://github.com/Cratis/Arc/tree/main/Source/JavaScript) | TypeScript and React package source |
| [`Documentation`](https://github.com/Cratis/Arc/tree/main/Documentation) | Product-owned documentation rendered on cratis.io |
| [`TestApps`](https://github.com/Cratis/Arc/tree/main/TestApps) | Sample and integration applications used by repository checks |

Package existence does not imply compatibility with every frontend, runtime, persistence provider, or product version. Use the exact package manifests, current documentation, and exercised profile for the combination you adopt.

## Documentation map

- [Canonical Arc documentation](https://cratis.io/arc/)
- [Product-owned documentation source](https://github.com/Cratis/Arc/tree/main/Documentation)
- [Arc releases](https://github.com/Cratis/Arc/releases)
- [Arc issues](https://github.com/Cratis/Arc/issues)

## Contributing

Arc is a framework-library repository. Changes to public APIs, analyzers, generated output, and package shapes can affect consumers and require the owning repository's compatibility and release review.

Repository development currently requires the SDK and toolchain versions declared by [`global.json`](https://github.com/Cratis/Arc/blob/main/global.json) and [`package.json`](https://github.com/Cratis/Arc/blob/main/package.json). Follow the [Cratis contribution guide](https://github.com/Cratis/.github/blob/main/contributing.md) and the [framework instructions](https://github.com/Cratis/Arc/blob/main/.ai/rules/framework.md) before changing public surfaces.

Before submitting documentation-only work, verify its links, anchors, and examples explicitly; current automated documentation checks are path-scoped. Source changes must pass the owning repository's applicable build, specification, TypeScript, and documentation gates.

## Community and repository

| Path | Destination |
| --- | --- |
| Questions and discussion | [Cratis Discord](https://discord.gg/kt4AMpV8WV) |
| Bugs and feature requests | [GitHub Issues](https://github.com/Cratis/Arc/issues) |
| Releases | [GitHub Releases](https://github.com/Cratis/Arc/releases) |
| Documentation | [cratis.io/arc](https://cratis.io/arc/) |
| Security reports | [Private security reporting](mailto:oss@cratis.io?subject=Security%3A) |
| License | [`LICENSE`](https://github.com/Cratis/Arc/blob/main/LICENSE) |
