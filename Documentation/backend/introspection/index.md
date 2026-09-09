# Introspection

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

## Topics

| Topic | Description |
| ------- | ----------- |
| [Commands](./commands.md) | Metadata shape and behavior for `/.cratis/commands`. |
| [Queries](./queries.md) | Metadata shape and behavior for `/.cratis/queries`. |
| [Identity details schema](./identity-details-schema.md) | JSON Schema shape for `/.cratis/identity-details/schema`. |
