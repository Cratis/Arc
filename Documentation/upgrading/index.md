---
title: Upgrade Cratis Arc
description: Check whether your Cratis Arc upgrade needs changes to forwarded identity, discovery access, generated identifiers or command scenarios.
---

When upgrading Cratis Arc, check the guides below for behavior your application relies on. They explain
which code or deployment settings need attention and how to check the result before deploying the upgrade.

These migration guides describe C# APIs and host behavior. For other backends,
use the [JVM parity reference](/arc/backend/kotlin/reference/parity/),
[TypeScript capability reference](/arc/backend/typescript/reference/capabilities/),
or [Go parity reference](/arc/go/parity/) to establish which behavior applies;
do not apply a .NET registration or diagnostic to another host.

| If your application relies on… | Follow this guide |
| --- | --- |
| Forwarded identity headers or anonymous access to discovery endpoints outside Development | [Secure defaults](secure-defaults.mdx): review the ingress trust boundary and choose authenticated or disabled discovery before considering anonymous access. |
| Identifiers created inside `Handle()`, `IIdentitySource` or queued identities in command scenarios | [Generated identifiers](generated-values.mdx): move generation to server-owned parameters, replace queues with named pins and check concept rules. |
| Commands that inject read models and never set `<CratisReadProtection>` | [Read protection is on by default](read-protection-default.md): keep `Off` with one property or adopt `On` through `Measure`. |
| `WithMcp`, `MapCratisArcMcp`, string `ExposeQuery`, `McpToolCeiling` or `GatedEndpoints` for MCP | [Arc MCP renames and gating](mcp-gated-by-default.md): switch to the new names and decide whether other routes on the capabilities port may be gated. |

Start with the behavior you use; you do not need to change an unrelated part of your application. For a
deployed host, check authentication and discovery using its actual environment and ingress. For generated
values, check both the handler and the scenarios that assert its result.
