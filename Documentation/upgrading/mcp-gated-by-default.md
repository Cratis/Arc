---
title: Migrate Arc MCP to the Arc 23 names and gating
description: Update C# Cratis Arc MCP hosts for the Arc 23 renames, the typed ExposeQuery overload and capability gating that no longer needs GatedEndpoints.
---

Arc 23 renames three MCP APIs and gates the MCP endpoint without requiring `CapabilitiesOptions.GatedEndpoints`. Review each change below if your host calls `WithMcp`.

## Renamed APIs

| Before | Now |
| --- | --- |
| `MapCratisArcMcp()` | `MapArcMcp()`. The old name is an obsolete forwarder and is removed in Arc 24. |
| `ExposeQuery("Namespace.ReadModel.Method", name, description)` | `ExposeQuery<ReadModel>(nameof(ReadModel.Method), name, description)`. The string overload is obsolete and is removed in Arc 24. |
| `McpToolCeiling` and its factory | `McpToolCatalog` and `McpToolCatalogFactory`. There is no forwarder; update any code that resolves or reads the type. |

"Ceiling" now only describes a caller's scope limit, such as `PatOptions.AllowedScopes`.

## MCP no longer requires GatedEndpoints

`McpRequiresGatedEndpoints` is removed. `WithMcp` with capability hosting and a verified caller installation now starts without `GatedEndpoints`:

```csharp
.WithCapabilities()
```

When `GatedEndpoints` is unset, Arc gates every endpoint on the capabilities port after routing. `/mcp` is never served without a verified caller. Only the OAuth protected resource metadata route stays anonymous, so OAuth clients can still discover how to authenticate. Other routes on that port, such as health or readiness probes, now also require one. If probes or other routes must stay reachable, either move them off the capabilities port or set `GatedEndpoints` to a selector for the endpoints to gate; `WithMcp` adds `/mcp` to it.

An explicit `GatedEndpoints` selection behaves as before.

To check the result, start the host and request `/mcp` and one non-MCP route on the capabilities port without credentials. Both should be rejected unless you set an explicit selection that leaves the second route outside the gate.
