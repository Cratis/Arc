---
title: Arc.Core
description: Standalone Arc pipelines and lightweight HTTP hosting without ASP.NET Core or an event store.
---

Use `Cratis.Arc.Core` when you want commands, queries, validation, authorization, identity details, and tenant context without the ASP.NET Core host. A console application or worker can expose HTTP endpoints through `ArcApplication` and .NET's `HttpListener`.

## Start with a working service

The [getting-started checkpoint](getting-started.md) runs a command and query using only Core. Read the [host comparison](overview.md) to decide whether Core or the `Cratis.Arc` ASP.NET integration fits your application. Performance and Native AOT compatibility require testing your actual dependencies and publish configuration; they are not guaranteed by choosing Core.

## Topics

- [Overview](overview.md) — capabilities, boundaries, and host choice.
- [Getting started](getting-started.md) — register, activate, and run.
- [Endpoint mapping](endpoint-mapping.md) — manual GET/POST endpoints.
- [Static files](static-files.md) — public assets and SPA fallback.
- [Authentication](authentication.md) — trusted principals and forwarded-header prerequisites.
- [Authorization](authorization.md) — pipeline authentication and role checks.
- [OpenAPI](openapi.md) — built-in lightweight route documentation.
- [Invariant culture](invariant-culture.md) — default culture configuration.

## Shared features and optional integrations

[Commands](../commands/index.md), [queries](../queries/index.md), [identity](../identity/index.md), and [tenancy](../tenancy/index.md) belong to standalone Arc. Review [configuration](../configuration/index.md) and [anonymous discovery defaults](../introspection/index.md) before deployment.

Add [MongoDB](../mongodb.md) or [EF Core](../entity-framework/index.md) for persistence without requiring event sourcing. Add [Chronicle](../chronicle/index.md) only when you want its event log, projections, and integration behavior. These integrations have their own packages and setup requirements.
