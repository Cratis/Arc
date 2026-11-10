---
title: "Arc MCP authorization: verified callers, tokens and scopes"
description: How Cratis Arc MCP decides who is calling and what that caller may do: verified callers, personal access tokens, forwarded identity, scopes and authorizers.
---

An MCP call is authorized in layers. Each layer can only narrow what the previous one allowed, and a refusal at any layer ends the call.

| Layer | Question | Refusal |
|---|---|---|
| Origin check | Is the browser origin allowed? | 403 `origin_not_allowed` |
| Verified caller | Did the capability gate admit a verified caller, and does the profile accept its credential kind? | 401 `unauthorized` |
| Scopes | Does the caller hold every scope the tool requires? | 403 `insufficient_scope` |
| `IMcpToolAuthorizer` | Does every registered authorizer allow this tool for this caller? | 403 `forbidden` |
| The operation | Does the command or query pass its own authorization and validation? | Tool error, `isAuthorized: false` or `isValid: false` |

The full admission order is in the [options reference](reference.md#admission-order-and-statuses).

## The caller is verified before MCP runs

Arc MCP never reads `HttpContext.User` or a header to decide who is calling. The [capability gate](../capability-hosting.md) verifies the credential and stores a verified caller for the request. MCP reads only that. If the gate admitted no caller, no MCP method runs and the response is 401.

The verified caller carries the person, the tenant, the scopes of the credential and the credential kind. The command or query that a tool runs sees the same identity and tenant it would see for a REST call.

## Credential kinds

| Kind | How it arrives | Accepted by profile |
|---|---|---|
| Personal access token (PAT) | `Authorization: Bearer <token>`, verified against your `IPatAuthority`. | `PatOnly` (default), `Combined` |
| OAuth grant | `Authorization: Bearer <token>` from your authorization server, verified by your installation. | `OAuthOnly`, `Combined` |
| Browser session (signed forwarded identity v2) | A signed envelope forwarded by a trusted proxy. | None. MCP refuses it with 401. |

A signed forwarded identity is how a signed-in person reaches the application through the proxy. Under the [PAT installation](../asp-net-core/personal-access-token-adapters.md#compose-browser-issuance-and-pat-callers) that session is what lets the person issue a PAT; the agent then calls MCP with the PAT, not with the session. Selected capability endpoints, MCP among them, refuse forwarded envelopes. A PAT that was forwarded through the proxy is still verified against your PAT authority.

Arc does not issue PATs through an endpoint of its own and does not issue OAuth tokens. See [Connect clients](connect-clients.md) for the profiles.

## What a PAT can do

A PAT is bound to one person and carries a fixed set of scopes. It can hold only scopes on `PatOptions.AllowedScopes`, never a scope with an `admin` or `approve` segment, and it lives at most 90 days (`PatOptions.MaximumLifetime`). The agent acts with the person's identity, restricted to those scopes.

| Scope held | The agent can |
|---|---|
| None of the tool's scopes | Not see the tool in `tools/list`, and get 403 `insufficient_scope` if it calls it. |
| `<ApplicationName>:read` | List and call query tools. |
| `<ApplicationName>:write` (or another write scope) | Call command tools that require it. A write scope does not grant read. |
| The above plus a tool's `[RequiresScope]` extras | Call that tool. Every required scope must be held. |

Holding the scope is necessary, not sufficient. The command's own validation, its authorization policy and any `IMcpToolAuthorizer` still run, so a person's PAT can never do more than the person could do through the application.

There is no MCP session: the transport is stateless and each request is verified again, so a revoked token stops working on its next call.

## Add your own rules

- Add required scopes with `[RequiresScope("...")]`; see [Scopes and descriptions](scopes-and-descriptions.md).
- Implement `IMcpToolAuthorizer` for rules a scope cannot express, such as "only members of this organisation". See [Tool sources and authorizers](advanced.md#tool-authorizers).
- Commands that require approval are never exposed.

## Related

- [Verified callers](../asp-net-core/verified-callers.md)
- [Personal access token adapters](../asp-net-core/personal-access-token-adapters.md)
- [Audit log](audit-log.md)
