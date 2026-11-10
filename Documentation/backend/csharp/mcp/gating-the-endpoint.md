---
title: "Gate the Arc MCP endpoint behind a verified caller"
description: Serve the Cratis Arc MCP endpoint only to verified callers: the capability gate, the gated endpoint selection, what Arc 23 sets by default and how to opt out.
---

The MCP endpoint must never answer an anonymous request. Arc enforces that with the [capability gate](../capability-hosting.md): endpoints selected for gating are served only on the capabilities port and only after a verified caller is admitted.

## What is required today

`WithMcp` needs all of these, in any registration order. The check runs when the host starts:

- `WithCapabilities(...)` for capability hosting.
- A port for the capabilities endpoint group: `options.EndpointGroups.Assign("capabilities", port)`.
- `CapabilitiesOptions.GatedEndpoints` set to a `CapabilityEndpointSelector`.
- A verified caller installation: `WithPersonalAccessTokens(...)` or `WithVerifiedCallers(...)`.

```csharp
.WithCapabilities(o => o.GatedEndpoints = new CapabilityEndpointSelector())
```

`WithMcp` adds the MCP endpoint to whatever selection you set, so you do not list `/mcp` yourself. If you already gate other endpoints, keep your selection: MCP is added to it unchanged. Without `GatedEndpoints`, the host refuses to start with the `McpRequiresGatedEndpoints` message, which names every requirement.

## Arc 23: gated by default

<!-- TODO(L110): Arc 23 (Cratis/Arc#3777) is not implemented in the branch: McpStartupValidation still throws McpRequiresGatedEndpoints when GatedEndpoints is null. When it lands, state here that WithMcp() with capability hosting and a verified caller installation but no GatedEndpoints starts and serves /mcp gated by a verified caller, that an explicit GatedEndpoints selection is respected unchanged, and link the upgrade notes. Then drop the GatedEndpoints line from the quick start sample and the "Must be set" row in reference.md. -->

For Arc 23 the intent is that the endpoint is gated without any `GatedEndpoints` setting. An explicit `GatedEndpoints` selection keeps full control, and the endpoint is never served without a verified caller. Until your Arc version includes that change, set `GatedEndpoints` as shown above.

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
app.MapCratisArcMcp();
```

<!-- TODO(L110): Arc 23 (Cratis/Arc#3775) renames MapCratisArcMcp to a name not yet decided (for example MapArcMcp), possibly with an obsolete forwarder for one major. Update this page and every sample (quick-start, advanced, reference, connect-clients) with the final name and add the rename to the upgrade notes. -->

## Related

- [Capability hosting](../capability-hosting.md)
- [Endpoint groups](../endpoint-groups.md)
- [Troubleshooting](troubleshooting.md#startup-errors)
