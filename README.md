# Cratis Arc

Arc is an opinionated CQRS application framework for ASP.NET Core with commands, queries, validation, authorization, and TypeScript proxy generation.

Arc hosts application behavior, executes command and query pipelines, exposes recognized contracts over HTTP, and generates TypeScript clients for them. Its packages also cover observable queries, validation, identity and tenancy, React integration, current-state persistence, OpenAPI, analyzers, and testing. Chronicle event sourcing and Cratis Components are optional integrations.

[![NuGet](https://img.shields.io/nuget/v/Cratis.Arc?logo=nuget)](https://www.nuget.org/packages/Cratis.Arc)
[![NPM](https://img.shields.io/npm/v/@cratis/arc?label=@cratis/arc&logo=npm)](https://www.npmjs.com/package/@cratis/arc)
[![.NET Build](https://github.com/Cratis/Arc/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/Cratis/Arc/actions/workflows/dotnet-build.yml)
[![JavaScript Build](https://github.com/Cratis/Arc/actions/workflows/javascript-build.yml/badge.svg)](https://github.com/Cratis/Arc/actions/workflows/javascript-build.yml)

## Start here

- [Browse the Arc documentation](https://www.cratis.io/arc/)
- [Choose a documented application path](#choose-a-path)
- [Understand Arc's independent boundary](#arc-does-not-require-event-sourcing)
- [Start an Arc host](#start-an-arc-host)
- [Inspect packages and repository layout](#packages-and-repository-layout)

## What Arc owns

| Boundary | Arc provides |
| --- | --- |
| Hosting | ASP.NET Core hosting plus an Arc.Core HTTP host for workers, consoles, and custom services |
| Commands | Model-bound and controller-based execution, validation, authorization, filters, scopes, typed results, and generated endpoints |
| Queries | Model-bound and controller-based reads, paging and sorting, observable updates, WebSocket or Server-Sent Events transport, and query diagnostics |
| Validation | Data annotations, FluentValidation, concept/command/query validators, generated client rules, and optional server preflight from React forms |
| Identity and tenancy | Pluggable authentication, identity details, role-based authorization, and header, query, claim, subdomain, fixed, or development tenant resolution |
| Generated contracts | TypeScript proxies, referenced types, validation metadata, identity details, incremental output cleanup, and package/type mapping |
| Frontend packages | TypeScript command/query runtimes, React hooks and forms, dialogs, observable-query composition, and optional React/MVVM integration |
| Persistence integration | MongoDB and Entity Framework Core current-state adapters plus optional Chronicle-backed event-sourced behavior |
| Evaluation and tooling | OpenAPI, runtime introspection, analyzers, source generators, command scenarios, proxy-generation checks, Vite helpers, and ESLint rules |

Each row is a documented capability area, not a promise of compatibility with every package, host, provider, browser, or product version.

## Choose a path

Arc documentation is organized around the job you need to complete:

- [Execute commands](https://github.com/Cratis/Arc/blob/main/Documentation/backend/commands/index.md) — model-bound or controller-based changes, pipelines, validation, authorization, filters, and response handling.
- [Expose queries and observable results](https://github.com/Cratis/Arc/blob/main/Documentation/backend/queries/index.md) — request/response reads, paging, sorting, streaming updates, diagnostics, and generated clients.
- [Build the frontend](https://github.com/Cratis/Arc/blob/main/Documentation/frontend/index.mdx) — TypeScript runtimes, React hooks and forms, dialogs, identity, messaging, and optional MVVM packages.
- [Configure identity and access](https://github.com/Cratis/Arc/blob/main/Documentation/understanding-identity-and-access.mdx) — authentication, identity details, authorization, roles, and frontend visibility boundaries.
- [Resolve tenants](https://github.com/Cratis/Arc/blob/main/Documentation/backend/tenancy/index.md) — request resolvers, scoped tenant context, and provider-specific database or namespace mapping.
- [Use current-state persistence](https://github.com/Cratis/Arc/blob/main/Documentation/arc-without-event-sourcing.md) — application services, MongoDB, or Entity Framework Core without requiring an event log.
- [Add Chronicle event sourcing](https://github.com/Cratis/Arc/blob/main/Documentation/backend/chronicle/index.md) — events, projections, reducers, aggregates, reactors, concurrency, and Chronicle-specific testing.
- [Inspect and verify the application boundary](https://github.com/Cratis/Arc/blob/main/Documentation/backend/introspection/index.md) — introspection, OpenAPI, analyzers, generated metadata, and command scenarios.

The canonical Arc page remains the front door; product-owned documentation and source carry the detail.

## Arc does not require event sourcing

Arc.Core does not depend on Chronicle. Commands and queries can use current-state persistence or application services without an event log. Choose the persistence and integrations that fit each application boundary.

The Chronicle integration is optional and supplies event-sourced behavior when configured. Arc retains its command, query, validation, authorization, and generated-contract boundary.

## Relationship to Components

Components is a React component library aligned with Arc application patterns. See the [Components documentation](https://www.cratis.io/components/) for its packages and setup.

Applications may use Arc without Components and remain responsible for their own frontend, accessibility, browser, and design-system verification.

## Start an Arc host

Arc's .NET packages embed this product-family README. The example below uses the umbrella `Cratis.Arc` package to start an Arc host; when viewing this README from a specialized package page, use that package's manifest and reference documentation for its own installation scope.

Use the .NET SDK declared by Arc's current [`global.json`](https://github.com/Cratis/Arc/blob/main/global.json). Install the host package:

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

Starting the host confirms the basic Arc setup. Continue with the [Arc documentation](https://www.cratis.io/arc/), and use [GitHub Issues](https://github.com/Cratis/Arc/issues) when the observed behavior does not match the documentation.

## Packages and repository layout

| Surface | Role |
| --- | --- |
| `Cratis.Arc` / `Cratis.Arc.Core` | .NET hosting, command/query pipelines, validation, identity, tenancy, HTTP, introspection, and application conventions |
| `Cratis.Arc.ProxyGenerator.Build` | Build-time TypeScript proxy generation for recognized application artifacts |
| `Cratis.Arc.MongoDB` / `Cratis.Arc.EntityFrameworkCore*` | Optional current-state persistence and observation adapters |
| `Cratis.Arc.Chronicle` | Optional event-sourced command, projection, reducer, aggregate, reactor, tenancy, and compliance integration |
| `Cratis.Arc.OpenApi` / `Cratis.Arc.Swagger` | Optional OpenAPI document transformation for Arc concepts and endpoints |
| `Cratis.Arc.Testing` / `Cratis.Arc.Chronicle.Testing` | Command-scenario and optional Chronicle event/read-model testing helpers |
| `@cratis/arc` | TypeScript command, query, validation, identity, and messaging runtime used by generated proxies |
| `@cratis/arc.react` | React hooks, forms, dialogs, identity, messaging, and query composition for Arc contracts |
| `@cratis/arc.react.mvvm` | Optional React/MVVM and dependency-injection integration |
| `@cratis/arc.vite` / `@cratis/eslint-plugin-arc` | Vite metadata/query helpers and Arc-specific frontend lint rules |
| [`Source/DotNET`](https://github.com/Cratis/Arc/tree/main/Source/DotNET) | .NET framework source |
| [`Source/JavaScript`](https://github.com/Cratis/Arc/tree/main/Source/JavaScript) | TypeScript and React package source |
| [`Documentation`](https://github.com/Cratis/Arc/tree/main/Documentation) | Product-owned documentation rendered on cratis.io |
| [`TestApps`](https://github.com/Cratis/Arc/tree/main/TestApps) | Sample and integration applications used by repository checks |

Package existence does not imply compatibility with every frontend, runtime, persistence provider, or product version. Check the package manifests and documentation for the versions you use.

## Documentation map

- [Arc documentation](https://www.cratis.io/arc/)
- [Product-owned documentation source](https://github.com/Cratis/Arc/tree/main/Documentation)
- [Arc releases](https://github.com/Cratis/Arc/releases)
- [Arc issues](https://github.com/Cratis/Arc/issues)

## Contributing

Arc is a framework-library repository. Changes to public APIs, analyzers, generated output, and package shapes can affect consumers and require the owning repository's compatibility and release review.

Repository development currently requires the SDK and toolchain versions declared by [`global.json`](https://github.com/Cratis/Arc/blob/main/global.json) and [`package.json`](https://github.com/Cratis/Arc/blob/main/package.json). Follow the [Cratis contribution guide](https://github.com/Cratis/.github/blob/main/contributing.md) and the [framework instructions](https://github.com/Cratis/Arc/blob/main/.ai/rules/framework.md) before changing public surfaces.

Before submitting documentation-only work, verify its links, anchors, and examples explicitly; current automated documentation checks are path-scoped. Source changes must pass the owning repository's applicable build, specification, TypeScript, and documentation gates.

* [.NET SDK 10.0.400+](https://dotnet.microsoft.com/en-us/). Arc's analyzers and source generators are built
  against Roslyn 5.9.0, which ships with .NET SDK 10.0.400 and newer. On an older SDK band the compiler
  refuses to load them (CS9057), silently disabling proxy generation and the ARC*/ARCCHR* analyzers.
  Consuming the `Cratis.Arc` packages carries the same floor. Raising this floor is a minor version bump.
* [Node 16+](https://nodejs.org/en)
* [Yarn](https://yarnpkg.com)

## Community and repository

| Path | Destination |
| --- | --- |
| Questions and discussion | [Cratis Discord](https://discord.gg/kt4AMpV8WV) |
| Bugs and feature requests | [GitHub Issues](https://github.com/Cratis/Arc/issues) |
| Releases | [GitHub Releases](https://github.com/Cratis/Arc/releases) |
| Documentation | [www.cratis.io/arc](https://www.cratis.io/arc/) |
| Security reports | [Private security reporting](mailto:oss@cratis.io?subject=Security%3A) |
| License | [`LICENSE`](https://github.com/Cratis/Arc/blob/main/LICENSE) |
