---
title: Cratis Arc.Core lightweight hosting
description: Cratis Arc.Core runs command and query pipelines in a console or worker service, with lightweight HTTP hosting and no ASP.NET Core or event store.
---

`Cratis.Arc.Core` lets a console application or worker expose commands and queries over HTTP without ASP.NET Core or an event store. Use `ArcApplication`, backed by .NET's `HttpListener`, when you want Arc's pipelines in a lightweight service.

## Start with a working service

Follow [getting started](getting-started.md) to create a console service and call its command and query with `curl`. If you need Kestrel, MVC, or ASP.NET middleware, read the [host comparison](overview.md) before choosing a package. Core does not by itself guarantee performance or Native AOT compatibility; test those with your actual dependencies and publish configuration.

## Topics

- [Overview](overview.md) — capabilities, boundaries, and host choice.
- [Getting started](getting-started.md) — register, activate, and run.
- [Endpoint mapping](endpoint-mapping.md) — manual GET/POST endpoints.
- [Static files](static-files.md) — public assets and SPA fallback.
- [Authentication](authentication.md) — trusted principals and forwarded-header prerequisites.
- [Authorization](authorization.md) — protect commands and queries with roles and policies.
- [OpenAPI](openapi.md) — built-in lightweight route documentation.
- [Invariant culture](invariant-culture.md) — default culture configuration.

## Shared features and optional integrations

The same service can use [command validation](../commands/index.md), [queries](../queries/index.md), [identity details](../identity/index.md), and [tenant context](../tenancy/index.md). Before deploying it, configure [authentication](authentication.md) and review [configuration](../configuration/index.md) and [discovery access defaults](../introspection/index.md).

Add [MongoDB](../mongodb/index.md) or [EF Core](../entity-framework/index.md) for persistence without requiring event sourcing. Add [Chronicle](../chronicle/index.md) only when you want its event log, projections, and integration behavior. These integrations have their own packages and setup requirements.
