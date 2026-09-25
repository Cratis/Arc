---
title: Introspection
description: Inspect your application's commands, queries, and identity schema at runtime, and decide how to expose those endpoints in production.
---

Arc exposes introspection endpoints that let you inspect the command and query surface of your application at runtime.

## Where to find the endpoints

Normal `UseCratisArc()` activation maps these endpoints unless replacements with the same endpoint names already exist.

- `/.cratis/commands`
- `/.cratis/queries`
- `/.cratis/identity-details/schema` (also mapped without a details provider; then returns `{}`)

These endpoints are mapped in both Arc.Core and ASP.NET Core hosting scenarios through `MapIntrospectionEndpoints()`.
Automatically called when just using the out-of-the-box general builders for setting up an Arc or a Cratis application.

## Production access

These endpoints have **explicit anonymous metadata** and no Development-only environment check. They expose operation names, types, routes, and schemas regardless of whether a caller may execute the operations. Normal identity activation also maps anonymous [user and tenant discovery](../identity/development-and-topologies.md) endpoints.

An ASP.NET fallback policy does not protect explicitly anonymous endpoints. If metadata is private, restrict these exact paths at trusted ingress and prevent direct backend access. Do not assume a production-disable `ArcOptions` switch exists: none is currently provided for this mapping. Test anonymous requests against the deployed Production configuration.

## What introspection does

Introspection returns metadata, not business data. It helps you:

- Discover command handlers and query performers.
- Inspect convention-derived routes and operation names.
- Read operation summaries populated from type metadata.
- Build tooling, diagnostics, and client-side discovery workflows.

This is discovered-operation metadata, not the final mapped/deduplicated runtime route table. In particular, query introspection does not apply custom `[Path]` routes; see the [query metadata reference](queries.md) before using `route` as a callable URL.

## Cross-implementation shape

Introspection is part of Arc's [shared HTTP contract](/arc/http-contract/), which both the C# and JVM backends honour. The endpoints and their anonymous access are common; the *depth* of the reported metadata is not. See [Introspection metadata depth](/arc/http-contract/#introspection-metadata-depth) before building tooling that has to run against both backends.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Commands](./commands.md) | Metadata shape and behavior for `/.cratis/commands`. |
| [Queries](./queries.md) | Metadata shape and behavior for `/.cratis/queries`. |
| [Identity details schema](./identity-details-schema.md) | JSON Schema shape for `/.cratis/identity-details/schema`. |
