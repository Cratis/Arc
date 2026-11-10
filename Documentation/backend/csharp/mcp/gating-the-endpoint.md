---
title: "Gate the Arc MCP endpoint behind a verified caller"
description: Serve the Cratis Arc MCP endpoint only to verified callers: the capability gate, the gated endpoint selection, what Arc 23 sets by default and how to opt out.
---

The MCP endpoint must never answer an anonymous request. Arc enforces that with the [capability gate](../capability-hosting.md): endpoints selected for gating are served only on the capabilities port and only after a verified caller is admitted.

## What is required

`WithMcp` needs all of these, in any registration order. The check runs when the host starts:

- `WithCapabilities(...)` for capability hosting.
- A port for the capabilities endpoint group: `options.EndpointGroups.Assign("capabilities", port)`.
- A verified caller installation: `WithPersonalAccessTokens(...)` or `WithVerifiedCallers(...)`.

```csharp
.WithCapabilities()
```

## Gated by default

You do not set `CapabilitiesOptions.GatedEndpoints` for MCP. When it is unset, Arc gates every endpoint on the capabilities port after routing, so `/mcp` is never served without a verified caller. Other routes on that port, such as health probes, then need a verified caller too; see the [upgrade notes](../../../upgrading/mcp-gated-by-default.md).

An explicit `GatedEndpoints` selection is respected unchanged. `WithMcp` adds the MCP endpoint to it, so you do not list `/mcp` yourself, and the rest of the port stays outside the gate.

## What the gate does to a request

| Situation | Result |
|---|---|
| Request on the default port | Not served. |
| Request on the capabilities port with no accepted credential | 401 `unauthorized` with a `WWW-Authenticate` challenge. |
| Credential of a kind the profile does not accept | 401 `unauthorized`. |
| Verified caller | Admitted. Scope and authorizer checks follow; see [Authorization](authorization.md). |

Assigning the port does not bind a listener. Bind both the default listener and one on the capabilities port, and make that port reachable only through your trusted ingress; see [Turn it on](advanced.md#turn-it-on).

## Map the endpoint

```csharp
var app = builder.Build();
app.UseCratisArc();
app.MapArcMcp();
```

`MapCratisArcMcp` still works as an obsolete forwarder to `MapArcMcp` and is removed in Arc 24.

## Related

- [Capability hosting](../capability-hosting.md)
- [Endpoint groups](../endpoint-groups.md)
- [Troubleshooting](troubleshooting.md#startup-errors)
