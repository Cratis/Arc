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

| Option | Default | Effect |
| --- | --- | --- |
| `Enabled` | `true` | `false` leaves both catalog routes unmapped. |
| `RequireAuthentication` | `false` | `true` requires an authenticated caller, regardless of the host's default authorization policy. |
| `Roles` | `null` | Comma-separated roles; any one grants access. Requires `RequireAuthentication: true` and no empty entries. A caller without a listed role is denied. |
| `TrustForwardedIdentityHeaders` | `false` | Obsolete. Setting it to `true` turns on the host-wide `Cratis:Arc:TrustForwardedIdentityHeaders`. See [Forwarded identity headers](#forwarded-identity-headers). |

Startup validation rejects `Roles` combinations that do not meet these requirements. Arc.Core responds with 401 for anonymous requests and 403 for authenticated callers without a required role. On ASP.NET Core, the response depends on the configured authentication scheme's challenge and forbid behavior: cookie authentication can redirect instead of returning 401 or 403 unless configured otherwise.

To omit an individual .NET command or query from the catalogs and generated OpenAPI,
apply `[ExcludeFromDiscovery]` to the command, read model, query method, or controller
action. This leaves invocation unchanged and is **not** an authorization mechanism.
The catalog options and their defaults are unchanged.

The JVM backend has no equivalent options: its catalog endpoints are always anonymous. See [the HTTP contract](/arc/http-contract/#authentication-and-introspection).

### Forwarded identity headers

The Microsoft Identity Platform (EasyAuth) mechanism reads `x-ms-client-principal*` headers without verifying a signature. Anyone who can reach the backend directly can forge them. Arc therefore ignores them on every request, including the protected catalogs, until the host sets `Cratis:Arc:TrustForwardedIdentityHeaders` to `true`. Without the opt-in, a request carrying only these headers is anonymous and a protected catalog returns 401.

**Only opt in behind a trusted ingress that authenticates callers, strips client-supplied identity headers, writes its own values, and prevents direct access to the backend.**

```json
{
  "Cratis": {
    "Arc": {
      "TrustForwardedIdentityHeaders": true,
      "Introspection": {
        "RequireAuthentication": true
      }
    }
  }
}
```

The opt-in is host-wide: once the host trusts the headers, they authenticate the catalogs exactly as they authenticate commands and queries. See [Microsoft Identity](../asp-net-core/microsoft-identity.md#trusting-the-forwarded-headers).

### ASP.NET Core host

With `RequireAuthentication: true`, startup requires a default authentication scheme and `builder.Services.AddAuthorization()`. The catalog combines ASP.NET Core's default authorization policy with an explicit authenticated-user requirement and any configured roles.

The built-in `MicrosoftIdentityPlatform` handler checks the host-wide opt-in inside `HandleAuthenticateAsync`, so the check also covers route groups, named policies, policy schemes that forward to the handler, and authentication that runs before routing. A subclass that overrides `HandleAuthenticateAsync` without calling the base implementation is your own authentication handler: Arc cannot see what it reads, so make it honor `ArcOptions.TrustForwardedIdentityHeaders` too.

### Arc.Core HttpListener host

With `RequireAuthentication: true`, startup requires at least one Arc.Core authentication handler. The built-in `MicrosoftIdentityPlatformAuthenticationHandler` ignores the forwarded headers until the host opts in, so a request that carries only those headers returns 401.

Arc cannot tell whether your own `IAuthenticationHandler` reads forwarded headers. If your handler establishes identity from headers that an ingress sets, make it honor the same opt-in: when `TrustForwardedIdentityHeaders` is `false`, return `AuthenticationResult.Anonymous`. See [Authentication](../core/authentication.md#microsoft-identity-platform-azure).

These options do **not** change `/.cratis/identity-details/schema`, command/query invocation, or the anonymous [user and tenant discovery](../identity/development-and-topologies.md) endpoints. For the identity schema or other metadata you consider private, restrict those paths at trusted ingress and prevent direct backend access. Test anonymous and authorized requests against your deployed Production configuration.

## What introspection does

Introspection returns metadata, not business data. It helps you:

- Discover command handlers and query performers.
- Inspect explicit `[Path]` routes or convention-derived routes and operation names.
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
