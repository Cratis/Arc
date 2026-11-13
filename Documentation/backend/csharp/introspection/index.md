---
title: "Cratis Arc introspection: inspect runtime metadata"
description: Cratis Arc introspection exposes command and query metadata at runtime. Control access to catalogs and identity discovery in production.
---

Cratis Arc introspection exposes runtime metadata for discovered C# commands and queries, including JSON Schemas for their inputs. Use it to inspect your application's contracts or supply them to tools such as Lens and the Cratis CLI.

Discovery access is separate from permission to execute an operation: a catalog caller sees every operation included in the catalog. Before exposing these endpoints in production, choose who should be able to read the metadata and any user or tenant information returned by discovery.

## Where to find the endpoints

Normal `UseCratisArc()` activation maps these endpoints, subject to the [production access settings](#production-access), unless replacements with the same endpoint names already exist:

- `/.cratis/commands`
- `/.cratis/queries`
- `/.cratis/identity-details/schema` (also mapped without a details provider; then returns `{}`)

Together with [user and tenant discovery](../identity/development-and-topologies.md) (`/.cratis/users` and `/.cratis/tenants`), these make up Arc's five discovery endpoints.

Both Arc.Core and ASP.NET Core hosts map the command and query catalogs through `MapIntrospectionEndpoints()`, which normal Arc activation calls automatically. The identity endpoint mapper handles the identity-details schema, users and tenants. `Enabled` controls only the catalogs; `IdentityDiscovery` controls the other three discovery routes. Both default to `true`, and the access settings below apply to all mapped discovery endpoints.

## Production access

By default, discovery allows anonymous access in Development so local tooling can read it without signing in. Everywhere else, it requires an authenticated caller. Development follows the host's `IHostEnvironment.IsDevelopment()`, including environment names selected through configuration, command-line arguments or host options. Only without a registered host environment does discovery fall back to `ASPNETCORE_ENVIRONMENT`, then `DOTNET_ENVIRONMENT`.

Outside Development, if the host cannot enforce authentication, Arc leaves discovery unmapped and logs a warning. For example, an ASP.NET Core host needs a default authentication scheme and authorization services. Explicitly requiring authentication or setting roles instead makes startup fail when enforcement is unavailable. `/.cratis/me` is separate from discovery and is unaffected by these settings.

Configure `Cratis:Arc:Introspection` to require authentication explicitly and restrict discovery to callers in either the `Administrator` or `Operator` role. This configuration does not register authentication services or middleware; follow the host requirements below.

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
| `Enabled` | `true` | `false` leaves both catalog routes unmapped; identity discovery is unchanged. |
| `IdentityDiscovery` | `true` | `false` leaves users, tenants and the identity-details schema unmapped; catalogs and `/.cratis/me` are unchanged. |
| `RequireAuthentication` | not set | Not set: anonymous in Development, authenticated elsewhere. `true` requires an authenticated caller for enabled discovery routes in every environment, regardless of the host's default authorization policy, and fails startup if the host cannot authenticate callers. `false` exposes enabled discovery routes anonymously in every environment, and logs a warning on startup outside Development. |
| `Roles` | `null` | Comma-separated roles; any one satisfies the role requirement. Requires an authenticated caller in every environment, cannot be combined with `RequireAuthentication: false`, and rejects empty entries. Role names are trimmed. A caller without a listed role is denied. |
| `TrustForwardedIdentityHeaders` | `false` | Obsolete. Setting it to `true` turns on the host-wide `Cratis:Arc:TrustForwardedIdentityHeaders`. See [Forwarded identity headers](#forwarded-identity-headers). |

Arc validates role configuration even when discovery is disabled. Valid `Roles` and `RequireAuthentication` settings can remain configured, but have no effect on unmapped routes. For protected discovery, Arc.Core responds with 401 for anonymous requests and 403 for authenticated callers without a required role. On ASP.NET Core, the response follows the configured authentication scheme's challenge and forbid behavior: cookie authentication can redirect instead of returning 401 or 403 unless configured otherwise.

To omit an individual .NET command or query from the catalogs and generated API descriptions, apply `[ExcludeFromDiscovery]` to the command, read model, or query method. On an MVC controller or action, the attribute hides API Explorer/Swagger descriptions. The endpoint remains mapped and callable, so enforce permissions on the operation itself. Hiding it does not authorize or deny a request, and does not change the catalog options or their defaults.

The JVM backend's built-in catalog routes allow anonymous access and have no equivalent options. See [the HTTP contract](/arc/http-contract/#authentication-and-introspection).

### Forwarded identity headers

Without signed forwarding enabled, the built-in Microsoft Identity Platform (EasyAuth) handlers read unsigned `x-ms-client-principal*` headers. These carry identity supplied by an ingress, the proxy in front of your app. Anyone who can reach the backend directly can forge them. Arc ignores these unsigned headers until the host sets `Cratis:Arc:TrustForwardedIdentityHeaders` to `true`; another authentication scheme can still authenticate the request.

This trust setting applies to every request, not just discovery. Only enable it behind a trusted ingress that authenticates callers, strips client-supplied identity headers, writes its own values, and prevents direct access to the backend.

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

Once enabled, the built-in handlers can use these headers to authenticate callers to catalogs as well as commands and queries. The older `Cratis:Arc:Introspection:TrustForwardedIdentityHeaders` setting turns on the same host-wide trust; it does not limit trust to discovery. See [Microsoft Identity](../asp-net-core/microsoft-identity.md#trusting-the-forwarded-headers) for registration and signed-forwarding requirements.

### ASP.NET Core host

With discovery enabled, explicitly requiring authentication through `RequireAuthentication: true` or `Roles` requires a default authentication scheme and `builder.Services.AddAuthorization()` at startup. Add authentication and authorization middleware before activating Arc. Discovery combines ASP.NET Core's default authorization policy with an explicit authenticated-user requirement and any configured roles.

The built-in `MicrosoftIdentityPlatform` handler checks the host-wide opt-in inside `HandleAuthenticateAsync`, so the check also covers route groups, named policies, policy schemes that forward to the handler, and authentication that runs before routing. A subclass that overrides `HandleAuthenticateAsync` without calling the base implementation is your own authentication handler: Arc cannot see what it reads, so make it honor `ArcOptions.TrustForwardedIdentityHeaders` too.

### Arc.Core HttpListener host

With discovery enabled, explicitly requiring authentication through `RequireAuthentication: true` or `Roles` requires at least one Arc.Core authentication handler at startup. Without signed forwarding enabled, the built-in `MicrosoftIdentityPlatformAuthenticationHandler` ignores unsigned forwarded headers until the host opts in. A request carrying only those untrusted headers remains anonymous and receives 401 from protected discovery.

Arc cannot tell whether your own `IAuthenticationHandler` reads forwarded headers. If your handler establishes identity from headers that an ingress sets, make it honor the same opt-in: when `TrustForwardedIdentityHeaders` is `false`, return `AuthenticationResult.Anonymous`. See [Authentication](../core/authentication.md#microsoft-identity-platform-azure).

The `Enabled`, `IdentityDiscovery`, `RequireAuthentication`, and `Roles` options affect discovery only. They do not change command/query invocation or `/.cratis/me`; the forwarded-header trust setting is host-wide. When checking your deployed Production configuration, confirm both that anonymous callers cannot read protected discovery and that authorized callers can.

If you intend to make discovery readable without signing in outside Development, set `RequireAuthentication` to `false` and leave `Roles` unset. This exposes the enabled catalogs and identity discovery to anonymous callers; use it only when that information is intended to be accessible to them.

```json
{
  "Cratis": {
    "Arc": {
      "Introspection": {
        "RequireAuthentication": false
      }
    }
  }
}
```

## Turn off all discovery in deployed environments

If you do not want deployed hosts to expose catalogs, user or tenant lists, or the identity schema, set **both** switches to `false` in `appsettings.Production.json` (or the settings file for your deployed environment):

```json
{
  "Cratis": {
    "Arc": {
      "Introspection": {
        "Enabled": false,
        "IdentityDiscovery": false
      }
    }
  }
}
```

Alternatively, supply these environment variables:

```bash
export Cratis__Arc__Introspection__Enabled=false
export Cratis__Arc__Introspection__IdentityDiscovery=false
```

Arc maps none of its five discovery routes, skips discovery authentication enforcement checks, and logs no discovery exposure warning. `/.cratis/me` and command/query invocation remain unchanged. Local tooling cannot read the disabled discovery routes; keep the defaults in Development when you need those tools.

## What introspection does

The command and query catalogs describe discovered operations without executing them. They return operation names, namespaces, type information, documentation summaries when available, and JSON Schemas for command payloads or query arguments. The identity-details schema endpoint describes your configured identity details contract; it does not return the caller's details.

Treat a catalog's `route` as metadata, not proof that the URL is callable. Introspection honors explicit `[Path]` routes for commands and queries, but uses the default route options rather than `Cratis:Arc:GeneratedApis`. It also does not mirror every final route deduplication or replacement decision. See the [query metadata reference](queries.md) before using `route` in tooling.

## Cross-implementation shape

The C# and JVM backends expose the introspection paths in Arc's [shared HTTP contract](/arc/http-contract/), but their access controls and metadata depth differ. Do not apply these .NET configuration options to a JVM host. See [Introspection metadata depth](/arc/http-contract/#introspection-metadata-depth) before building tooling that reads both backends.

## Topics

With discovery access configured, use these references to interpret the fields your tooling reads:

| Topic | Description |
| ------- | ----------- |
| [Commands](./commands.md) | Metadata shape and behavior for `/.cratis/commands`. |
| [Queries](./queries.md) | Metadata shape and behavior for `/.cratis/queries`. |
| [Identity details schema](./identity-details-schema.md) | JSON Schema shape for `/.cratis/identity-details/schema`. |
