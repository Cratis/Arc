---
title: Cratis Arc setup for ASP.NET Core
description: Set up Cratis Arc in an ASP.NET Core host, then configure request binding, authentication, and authorization for your application's endpoints.
---

Cratis Arc integrates command and query execution with ASP.NET Core, so you can use Arc alongside the host's middleware, MVC controllers, and authentication. Install `Cratis.Arc`, register it with `WebApplicationBuilder.AddCratisArc()`, and activate it with `WebApplication.UseCratisArc()`. No event store is required.

Start with [configuration](configuration.md) for host settings. Before exposing private operations, configure an authentication scheme and [authorization requirements](authorization.md); adding Arc does not make every endpoint private. OpenAPI and Swagger support come in separate optional packages.

## Features

- [Configuration](configuration.md): set Arc options in appsettings.json or code.
- [Authorization](authorization.md): protect HTTP endpoints and Arc pipeline calls at their respective boundaries.
- [Microsoft Identity](microsoft-identity.md): authenticate forwarded principals behind a trusted ingress.
- [Acting on behalf of a user](acting-on-behalf-of.md): authorize as the represented user while retaining the actor for audit.
- [FromRequest Attribute](from-request.md): combine JSON, route, and query values in one MVC input model.
- [Swagger](swagger.md): describe Arc endpoints and check schema limits before generating clients.
- [Validation](validation.md): validate input with FluentValidation.
- [Without Wrappers](without-wrappers.md): choose which endpoints return unwrapped responses.
- [Invariant Culture](invariant-culture.md): give culture-sensitive parsing and formatting consistent defaults.

## When to Use ASP.NET Core Integration

Choose this host when your application needs ASP.NET Core's middleware ecosystem, Kestrel, MVC or Razor, or static-file serving. It is also the host for Arc's Swagger UI integration. You can keep those web concerns in ASP.NET Core while Arc handles commands and queries.

## When to Use Arc.Core Instead

Use [Arc.Core](../core/overview.md) without ASP.NET Core when you want Arc's command and query pipelines in a console application, background service, or its own `HttpListener` host. You do not get the ASP.NET middleware ecosystem with that host. Check performance and Native AOT compatibility with your actual deployment dependencies before choosing on those grounds.

## See Also

- [Arc.Core Overview](../core/overview.md) - Lightweight alternative without ASP.NET Core
- [Commands](../commands/index.md) - Command handling patterns
- [Queries](../queries/index.md) - Query patterns and conventions
- [Tenancy](../tenancy/index.md) - Tenant isolation and management
