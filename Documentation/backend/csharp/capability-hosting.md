---
title: "Cratis Arc capability hosting"
description: Require verified callers on a dedicated Cratis Arc endpoint-group port, read the admitted caller, and record capability causation with Chronicle.
---

Cratis Arc capability hosting requires a verified caller for every request on an assigned endpoint-group port. Place the commands and queries you want to expose in that group, then enable the caller gate.

## What it does

Requests without a verified caller receive `401` with `WWW-Authenticate: Bearer`. Malformed authorization, multiple credentials, and credentials in a URI receive `400`. A verified principal kind outside the configured allowlist receives `403`. These refusals use `application/problem+json`, not HTML, and do not run the handler. Requests on other ports do not resolve a capability caller.

The default allowlist admits `Person` and `DelegatedActor`. Configure `AllowedPrincipalKinds` explicitly if the application also admits service principals.

## Turn it on

Configure both the server listener and the [endpoint-group assignment](endpoint-groups.md). This host setup enables the default `capabilities` group:

```csharp
using Cratis.Arc;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:5000", "http://*:5001");
builder.AddCratisArc(
    options => options.EndpointGroups.Assign("capabilities", 5001),
    configureBuilder: arc => arc.WithCapabilities());

var app = builder.Build();
app.UseCratisArc();
app.Run();
```

Tag capability commands, read models, or query methods with `[EndpointGroup("capabilities")]`. Group membership is the explicit exposure choice; commands and queries outside that group retain their existing routing. Enabling capabilities without assigning the group's port fails startup with `CapabilityEndpointGroupHasNoPort`.

For a different group name, set `EndpointGroup` through the `WithCapabilities` configuration delegate and assign a port to that same group.

## Read the caller

Inject `Cratis.Arc.Capabilities.ICapabilityCallers` into capability code. Its `Current` property returns the admitted `CapabilityCaller`, including the endpoint group, protocol `rest`, principal kind, person, current delegated actor, client metadata, credential identifier, checked grant identifier, tenant, and scope ceiling. Outside a capability call it is null.

A declared client is metadata, not an authenticated actor. `Client.IsAuthenticated` distinguishes authenticated client metadata from declared metadata. The scopes are a verified ceiling, not an authorization decision.

## What events record

With Arc's Chronicle integration, a command executed while a capability caller is present adds a `Capability` entry to event causation. It contains these properties:

| Property | Value |
| --- | --- |
| `endpointGroup` | Receiving endpoint group |
| `protocol` | `rest` |
| `principalKind` | `person`, `delegated-actor`, `organization-automation`, `cluster-workload`, or `runner` |
| `person` | Product person reference |
| `actorIssuer` | Current delegated actor's issuer |
| `actorSubject` | Current delegated actor's subject |
| `clientId` | Client identifier |
| `clientAuthentication` | `declared` or `authenticated` |
| `credentialId` | Non-secret credential identifier |
| `grantId` | Checked delegation grant identifier |
| `tenant` | Verified tenant identifier |

Absent values are omitted, including an unset tenant. Tokens, secrets, scopes, and the internal credential kind are not recorded by this entry. Commands outside a capability call add no capability causation.

## Limits today

- A credential verifier must be installed, such as a PAT package or forwarded-identity verifier. The inert defaults refuse every capability call.
- Product authorization still applies: a capability is not authority. The verified caller is not mapped to `HttpContext.User`; commands using `[Authorize]` still evaluate that principal.
- Keep the group port private. Never expose it publicly or forward public traffic to it.
- Default-group endpoints reachable on the capability port also require a caller. There are no anonymous paths on that port, including health probes.
- MCP serving and protected-resource metadata follow separately; this API hosts REST capabilities only.
