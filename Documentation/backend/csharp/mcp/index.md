---
title: "Cratis Arc MCP: commands and queries as agent tools"
description: Cratis Arc MCP turns the commands and queries you already wrote into Model Context Protocol tools that keep Arc's validation, authorization and tenancy.
---

Cratis Arc MCP is the `Cratis.Arc.Mcp` package. It serves a [Model Context Protocol](https://modelcontextprotocol.io) endpoint from your Arc application, so an AI agent can call the commands and queries you choose as tools.

## Why use it

A hand-written MCP server is a second application. It needs its own input checks, its own authorization and its own way to find the tenant, and each one can drift from the real application.

With Arc MCP there is no second code path. A tool call runs the same command or query pipeline as a REST call:

- Command validation runs, and its results come back to the agent as structured validation results.
- Authorization runs, including `[RequiresScope]` and any `IMcpToolAuthorizer` you register.
- The caller is the verified caller the [capability gate](../capability-hosting.md) admitted, so tenancy and identity come from the same place as for REST.

## What stays under your control

- **Opt-in.** Nothing is exposed unless you declare it with `Expose`, `ExposeQuery` or `[ExposeOverMcp]`.
- **Fixed ceiling.** Tool names, descriptions and scopes are checked at startup. A tool that is invalid, has no description, requires no scope, or writes without a write scope stops the application from starting.
- **Scoped visibility.** A caller only sees, and can only call, the tools its scopes allow.
- **No approval-required commands.** A command that requires approval is never exposed.
- **Stateless.** Each request is one call. There are no MCP sessions.

## How it fits together

1. The endpoint is served only on the capabilities [endpoint group](../endpoint-groups.md) port, behind capability hosting.
2. Callers authenticate with a personal access token (PAT) by default (the `PatOnly` profile). `OAuthOnly` and `Combined` profiles accept OAuth grants verified by your installation. Arc does not issue tokens.
3. Browser-based clients and hosted connectors need extra setup; see [Connect clients](connect-clients.md).

## Where to go next

| I want to | Page |
|---|---|
| See it work in ten minutes | [Quick start](quick-start.md) |
| Decide what to expose | [Expose a command or query safely](expose-safely.md) |
| Name tools and choose scopes | [Scopes and descriptions](scopes-and-descriptions.md) |
| Connect Claude, VS Code or another client | [Connect clients](connect-clients.md) |
| Debug with a visual client | [Test with MCP Inspector](test-with-inspector.md) |
| Fix an error | [Troubleshooting](troubleshooting.md) |
| Look up an option | [Options reference](reference.md) |

[Tool sources and authorizers](advanced.md) covers curated tool sources, authorizers, exclusive tools, session authority and the Origin check in depth.
