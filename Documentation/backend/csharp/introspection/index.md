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

The command and query catalog endpoints are mapped in both Arc.Core and ASP.NET Core hosting scenarios through `MapIntrospectionEndpoints()`. Normal Arc activation calls it automatically. The identity-details schema is mapped separately by the identity endpoint mapper and is not controlled by the catalog options: identity setup and clients can still need its schema when catalog discovery is disabled.

## Production access

By default, both catalog endpoints are enabled and explicitly anonymous, including in Production. They expose operation names, types, and routes regardless of whether a caller may execute the operations. An ASP.NET fallback policy does not protect the anonymous default.

Configure `Cratis:Arc:Introspection` to change that boundary:

```json
{
  "Cratis": {
    "Arc": {
      "Introspection": {
        "Enabled": true,
        "RequireAuthentication": true,
        "Roles": "Administrator,Operator"
      }
    }
  }
}
```

`Enabled` defaults to `true`; set it to `false` to leave both catalog routes unmapped. `RequireAuthentication` defaults to `false`; set it to `true` to require a signed-in caller. `Roles` defaults to `null`; if set, any one of the comma-separated roles grants access. Roles require `RequireAuthentication: true`. `TrustForwardedIdentityHeaders` defaults to `false` and applies only to Arc.Core; do not enable it unless a trusted ingress authenticates callers and strips client-supplied identity headers. The ASP.NET Core host needs a default authentication scheme and `builder.Services.AddAuthorization()`; the Arc.Core HttpListener host needs an authentication handler other than the built-in forwarded-header handler, or an explicit `TrustForwardedIdentityHeaders: true` opt-in. Without either, startup rejects the protected configuration. The built-in Microsoft Identity Platform handler reads identity headers without verifying a signature; for a protected catalog it ignores them unless you opt in. **Only opt in behind a trusted ingress that authenticates callers, strips client-supplied identity headers, and prevents direct listener access.** For an Arc.Core host using that ingress, add `"TrustForwardedIdentityHeaders": true` under `Cratis:Arc:Introspection`. ASP.NET Core uses its authorization middleware; ensure you do not bypass that middleware in a custom pipeline. A role mismatch returns 403; an anonymous request returns 401.

These options do **not** change `/.cratis/identity-details/schema`, command/query invocation, or the anonymous [user and tenant discovery](../identity/development-and-topologies.md) endpoints. For the identity schema or other metadata you consider private, restrict those paths at trusted ingress and prevent direct backend access. Test anonymous and authorized requests against your deployed Production configuration.

## What introspection does

Introspection returns metadata, not business data. It helps you:

- Discover command handlers and query performers.
- Inspect convention-derived routes and operation names.
- Read operation summaries populated from type metadata.
- Build tooling, diagnostics, and client-side discovery workflows.

This is discovered-operation metadata, not the final mapped/deduplicated runtime route table. In particular, query introspection does not apply custom `[Path]` routes; see the [query metadata reference](queries.md) before using `route` as a callable URL.

## Cross-implementation shape

Introspection is part of Arc's [shared HTTP contract](/arc/http-contract/), which both the C# and JVM backends honour. The endpoint paths are common; the C# host can configure catalog access as described above. The *depth* of the reported metadata is also different. See [Introspection metadata depth](/arc/http-contract/#introspection-metadata-depth) before building tooling that has to run against both backends.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Commands](./commands.md) | Metadata shape and behavior for `/.cratis/commands`. |
| [Queries](./queries.md) | Metadata shape and behavior for `/.cratis/queries`. |
| [Identity details schema](./identity-details-schema.md) | JSON Schema shape for `/.cratis/identity-details/schema`. |
