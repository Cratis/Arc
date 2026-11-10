---
title: "Cratis Arc MCP options, attributes and endpoints reference"
description: Reference for Cratis Arc MCP: McpOptions defaults, McpProfile, the builder and attribute API, startup validation rules, HTTP endpoints and statuses.
---

Assembly: `Cratis.Arc.Mcp` (depends on `ModelContextProtocol.AspNetCore`).

## Extension methods

| Method | Purpose |
|---|---|
| `IArcBuilder.WithMcp(Action<McpBuilder>? configure)` | Installs MCP serving with the stateless HTTP transport. Requires capability hosting. |
| `IEndpointRouteBuilder.MapCratisArcMcp()` | Maps the MCP endpoint and the protected resource metadata endpoint. |

## McpBuilder

| Member | Purpose |
|---|---|
| `Configure(Action<McpOptions>)` | Sets options. |
| `Expose<TCommand>(name, description)` | Exposes a command as a tool. |
| `ExposeQuery(fullyQualifiedQueryName, name, description)` | Exposes a query. The name is `<read model full type name>.<method name>`. |
| `AddToolSource<T>()` | Registers an `IMcpToolSource` for curated tools. |
| `AddToolAuthorizer<T>()` | Registers a scoped `IMcpToolAuthorizer`. |

## ExposeOverMcpAttribute

`[ExposeOverMcp(name)]` with a `Description` property. Applies to a command class or a read model query method. One per target, not inherited. A tool without a description fails startup.

## McpOptions

| Property | Default | Notes |
|---|---|---|
| `Path` | `"/mcp"` | Must start with `/`. |
| `Profile` | `McpProfile.PatOnly` | See below. |
| `ApplicationName` | `null` | Used for default scopes and the `realm` in `WWW-Authenticate`. |
| `ReadScope` | `<ApplicationName>:read` (null without an application name) | Required by every query tool. |
| `WriteScope` | `<ApplicationName>:write` (null without an application name) | Required by every command tool. Always a write scope. |
| `WriteScopes` | empty | Additional write scopes. No blanks, repeats or `ReadScope`. |
| `ScopesSupported` | empty | Published in metadata. Empty publishes the read and write scopes. |
| `PublicOrigin` | `null` | Valid origin. Required for `OAuthOnly` and `Combined`. Always an allowed `Origin`. |
| `Resource` | `null` | For `OAuthOnly` and `Combined`, must equal `PublicOrigin` plus `Path`. |
| `AuthorizationServers` | empty | For `OAuthOnly` and `Combined`, at least one. |
| `AllowedOrigins` | empty | Extra allowed `Origin` values. |
| `MaxRequestBodySize` | `1048576` | Bytes. Must be greater than 0. |
| `ExclusiveTools` | `true` | `false` serves server-registered tools without the Arc ceiling and logs a startup warning. |

A valid origin uses `https`, or `http` for loopback only, with no path, query, fragment or user info. Each `AuthorizationServers` entry follows the same rule and also allows no query or fragment.

### McpProfile

| Value | Accepts |
|---|---|
| `PatOnly` | Personal access tokens. |
| `OAuthOnly` | OAuth grants. |
| `Combined` | Both. |

Any other credential kind is treated like no caller (401).

## Related attributes and options

| Item | Use |
|---|---|
| `[RequiresScope("...")]` (`Cratis.Arc.Authorization`) | Extra required scope. Repeatable; all must be held. |
| `[RequiresApproval]` | Makes a command ineligible for MCP. |
| `PatOptions.AllowedScopes` | Scopes a PAT may hold. Must include every MCP scope. |
| `PatOptions.MaximumLifetime` | Default and maximum 90 days. |
| `CapabilitiesOptions.GatedEndpoints` | Must be set. `WithMcp` adds the MCP endpoint to it. |
| `ArcOptions.EndpointGroups.Assign("capabilities", port)` | Port MCP is served on. |

## Tool annotations

| Tool | ReadOnly | Destructive | Idempotent | OpenWorld |
|---|---|---|---|---|
| Query | true | false | true | false |
| Command | false | true | false | false |

## HTTP endpoints

| Method and path | Behavior |
|---|---|
| `POST <Path>` | JSON-RPC request. One message per request. |
| `GET`, `DELETE <Path>` | 405 with `Allow: POST`, after admission. |
| `GET /.well-known/oauth-protected-resource<Path>` | Metadata JSON (`resource`, `authorization_servers`, `scopes_supported`), `Cache-Control: no-store`. 404 `not_found` under `PatOnly`. |

### Admission order and statuses

| Step | Refusal |
|---|---|
| Origin check | 403 `origin_not_allowed` (no `Origin` header is allowed) |
| Caller and profile | 401 `unauthorized`, `WWW-Authenticate`: `Bearer realm="<ApplicationName>"` (`PatOnly`) or `Bearer resource_metadata="<PublicOrigin>/.well-known/oauth-protected-resource<Path>"` |
| Method | 405 `method_not_allowed` |
| Body size | 413 `request_too_large` |
| Body shape | 400 `invalid_request` (invalid JSON, duplicate members, batch array) |
| `tools/call` scope check | 403 `insufficient_scope`, `WWW-Authenticate: Bearer error="insufficient_scope", scope="<scopes>"` |
| Authorizer | 403 `forbidden` |

## Tool call result

The result text is JSON: `{ "outcome": { "isSuccess", "isAuthorized", "isValid", "validationResults": [{ "severity", "message", "members" }] }, "response": ... }` for commands, and `data` plus `paging` for queries. `isError` is set when `isSuccess` is false.

For setup problems see [Troubleshooting](troubleshooting.md). For curated tool sources and authorizers see [Serving MCP tools](../mcp.md).
