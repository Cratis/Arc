---
title: "Troubleshoot Cratis Arc MCP errors"
description: Find the cause and fix for Cratis Arc MCP startup errors, HTTP statuses and tool errors, including Origin, PAT scope and capability hosting problems.
---

Look up the error text or HTTP status, then follow the fix.

## Startup errors

Startup validation runs in this order. Fix the first error you see.

| Message or exception | Cause | Fix |
|---|---|---|
| `MCP serving requires capability hosting. Call WithCapabilities() before WithMcp().` | `WithMcp` without capability hosting. | Add `WithCapabilities(...)` before `WithMcp(...)`. |
| `Capability endpoint group '<group>' has no port. Assign one with ArcOptions.EndpointGroups.Assign("<group>", <port>).` | The capabilities endpoint group has no port. | `options.EndpointGroups.Assign("capabilities", port)` in the `AddCratisArc` options. |
| `MCP serving requires selected gated endpoints. Set CapabilitiesOptions.GatedEndpoints when calling WithCapabilities().` | `GatedEndpoints` is null. The message names the cause, not the fix you may expect: it is about capability hosting, not MCP options. | `WithCapabilities(o => o.GatedEndpoints = new CapabilityEndpointSelector())`. `WithMcp` adds the MCP endpoint to the selection. |
| `McpOptions.<option> is missing or invalid for the <profile> profile.` | An option is invalid. `<option>` names it. | See the rules in the [options reference](reference.md). For `OAuthOnly` and `Combined`, set `PublicOrigin`, `Resource` (= `PublicOrigin` + `Path`) and `AuthorizationServers`. |
| `MCP tool '<name>' is invalid. Names must match ^[a-z][a-z0-9_]{0,63}$.` | Bad tool name. | Use lowercase letters, digits and underscores, starting with a letter. |
| `MCP tool '<name>' is declared more than once.` | Duplicate name. | Rename one. |
| `MCP tool '<name>' has no description.` | Missing description. | Add `Description` or the builder argument. |
| `MCP tool '<name>' exposes '<op>', which is not a command or query known to Arc.` | A wrong query name or an operation without a handler. | For `ExposeQuery`, use `<ReadModel full type name>.<Method>`. Make sure the command has a handler. |
| `... which requires approval. Approval-required commands are never exposed over MCP.` | The command has `[RequiresApproval]`. | Do not expose it. |
| `MCP tool '<name>' requires no scope. Configure McpOptions.ApplicationName, ReadScope and WriteScope.` | No application name and no explicit scope. | Set `ApplicationName`, or `ReadScope` and `WriteScope`. |
| `... may change state, so every scope set it declares must require McpOptions.WriteScope or one of McpOptions.WriteScopes.` | A write tool, usually a curated source, requires only read scopes. | Require `WriteScope` or add the scope to `WriteScopes`. |
| `MCP tool '<name>' is used both by a tool registered on the MCP server and by an exposed tool.` | Name clash with a server-registered tool. | Rename one. |
| Startup fails for a command you did not expose | Arc validates authorization for every discovered command and query. | Fix the missing or invalid authorization policy it names. |

## HTTP errors

| Status and body | Cause | Fix |
|---|---|---|
| 403 `origin_not_allowed` | The request has an `Origin` header that is not `PublicOrigin` or in `AllowedOrigins`. Typical for a browser-based client. | Add the exact origin to `AllowedOrigins`; see [Test with MCP Inspector](test-with-inspector.md). |
| 401 `unauthorized` | No caller, or a caller of a kind the profile does not accept. | Send `Authorization: Bearer <PAT>`. A PAT-only endpoint rejects OAuth grants. Check the token has not expired or been revoked. |
| 401 for a PAT that looks right | The token holds a scope that is not in `PatOptions.AllowedScopes`. | Add every MCP scope to `AllowedScopes`, then issue a new token. |
| 403 `insufficient_scope` | The tool needs a scope the caller does not hold. The `WWW-Authenticate` header lists the scopes. | Issue a token with those scopes. |
| 403 `forbidden` | A registered `IMcpToolAuthorizer` denied the call. | Check the authorizer's rule. |
| 404 `not_found` on `/.well-known/oauth-protected-resource/mcp` | The profile is `PatOnly`, which serves no metadata. | Use `OAuthOnly` or `Combined` if a client needs it. |
| 405 `method_not_allowed` (`Allow: POST`) | A GET or DELETE on the MCP path. | Clients must POST. |
| 413 `request_too_large` | The body is over `MaxRequestBodySize` (1 MiB by default). | Send less, or raise the option. |
| 400 `invalid_request` | Invalid JSON, duplicate members, or a JSON-RPC batch array. | Send one JSON-RPC message per request. |
| Not found or refused on the default port | MCP is served only on the capabilities port. | Use the capabilities port in the URL, and bind a listener on it. |

## Tool errors

A tool failure returns a result with `isError` set, not an HTTP error.

| Text | Cause | Fix |
|---|---|---|
| `The arguments are not valid for this tool.` | An argument is missing, has the wrong type, or is not in the schema. | Match the tool's `inputSchema`. Undeclared arguments are refused. |
| `The caller lacks a scope this tool requires.` | The scope check failed at the tool. | See `insufficient_scope`. |
| `The caller is not allowed to call this tool.` | An authorizer or the operation's authorization refused. | Check authorization on the command or query. |
| `The tool failed.` | The operation threw. | Check the server log. |
| `The tool is not available on this host.` | A streaming query, or a replaced command or query pipeline. | Expose a non-streaming query. Use Arc's own pipelines. |
| Result with `isValid: false` | Command validation failed. | Read `validationResults` and fix the input. |

## The tool list is empty or missing a tool

`tools/list` shows only tools the caller's scopes and the authorizers allow. An empty list means no caller was identified or the token holds no MCP scopes. A missing write tool means the token lacks the write scope.
