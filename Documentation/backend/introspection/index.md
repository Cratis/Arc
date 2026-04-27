# Introspection

Arc exposes introspection endpoints that let you inspect the command and query surface of your application at runtime.

## Where to find the endpoints

When you call `UseCratisArc()`, Arc maps introspection endpoints automatically.

- `/.cratis/commands`
- `/.cratis/queries`

These endpoints are mapped in both Arc.Core and ASP.NET Core hosting scenarios through `MapIntrospectionEndpoints()`.
Automatically called when just using the out-of-the-box general builders for setting up an Arc or a Cratis application.

## What introspection does

Introspection returns metadata, not business data. It helps you:

- Discover available command and query endpoints.
- Understand route paths and operation names.
- Read operation summaries populated from type metadata.
- Build tooling, diagnostics, and client-side discovery workflows.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Commands](./commands.md) | Metadata shape and behavior for `/.cratis/commands`. |
| [Queries](./queries.md) | Metadata shape and behavior for `/.cratis/queries`. |
