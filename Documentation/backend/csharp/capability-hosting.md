---
title: "Cratis Arc capability hosting"
description: Require verified callers on a dedicated Cratis Arc endpoint-group port, read the admitted caller, and record capability causation with Chronicle.
---

Cratis Arc capability hosting requires a verified caller for every request on an assigned endpoint-group port by default. Place the commands and queries you want to expose in that group, then enable the caller gate.

## What it does

Requests without a verified caller receive `401` with `WWW-Authenticate: Bearer`. Malformed authorization, multiple credentials, and credentials in a URI receive `400`. A verified principal kind outside the configured allowlist receives `403`. These refusals use `application/problem+json`, not HTML, and do not run the handler. Requests on other ports do not resolve a capability caller.

The default allowlist admits `Person` and `DelegatedActor`. Configure `AllowedPrincipalKinds` explicitly if the application also admits service principals.

## Turn it on

Configure the server listener, the [endpoint-group assignment](endpoint-groups.md), and a [verified caller installation](asp-net-core/verified-callers.md). The following **configuration fragment** belongs in the runnable acceptance sample's `Program.Build`, where `settings` supplies public verification material and the sample's `PersonDirectory` provides persisted account mappings:

```csharp
using Cratis.Arc;
using VerifiedCallerAcceptance;

builder.AddCratisArc(
    options => options.EndpointGroups.Assign("capabilities", settings.CapabilityPort),
    configureBuilder: arc => arc.WithCapabilities().WithVerifiedCallers(callers => callers
        .ForResource("https://api.example.test", "example-api", "sample")
        .UsePersonDirectory<PersonDirectory>()
        .UseSignedForwardedIdentityV2(v2 => v2.WithRealmIssuer("https://identity.example.test")
            .AddPublicKey(settings.KeyId, settings.PublicKeyPem)
            .UseProcessLocalNonceStore())));
```

The fragment omits listener setup: before accepting signed callers, configure HTTPS on the capability port and require a trusted forwarding peer's client certificate as shown in [Install one receiving resource](asp-net-core/verified-callers.md#install-one-receiving-resource). An HTTP-only listener cannot admit signed v2 callers; assigning a port does not establish transport trust.

Tag capability commands, read models, or query methods with `[EndpointGroup("capabilities")]`. Group membership is the explicit exposure choice; commands and queries outside that group retain their existing routing. Enabling capabilities without assigning the group's port fails startup with `CapabilityEndpointGroupHasNoPort`. Startup also fails with `CapabilityHostingHasNoCredentialInstallation` when no credential verifier and receiving installation are registered.

For a different group name, set `EndpointGroup` through the `WithCapabilities` configuration delegate and assign a port to that same group. The [verified caller guide](asp-net-core/verified-callers.md) explains custom signed tenant headers and fleet-wide nonce stores. Process-local replay protection is only a single-process sample choice.

## Select gated endpoints on a shared listener

Leave `GatedEndpoints` unset to gate the whole capability port, including health probes. To share that listener with existing routes, explicitly select the **gated** endpoints:

```csharp
arc.WithCapabilities(options => options.GatedEndpoints = new CapabilityEndpointSelector()
    .ForRoutePattern("/tools/{name}"));
```

This configuration fragment uses `Cratis.Arc.Capabilities`. Patterns match the registered endpoint's `RoutePattern.RawText` exactly with ordinal comparison: include route parameters, preserve case and slashes, and do not use a request path, prefix, wildcard, or regular expression. ASP.NET Core routing decides which endpoint a request reaches; the gate never compares the request path to these patterns. Controller route text commonly has no leading slash (`"tools/{name}"`), while a minimal endpoint may register `"/tools/{name}"`. Those are distinct selector rules even if they accept the same request path. Review both registrations as aliases or select their metadata instead.

For endpoints with aliases, prefer server-attached metadata. Define a marker type such as `ToolEndpointMetadata`, attach an instance with `.WithMetadata(new ToolEndpointMetadata())` to **every** alias's endpoint builder, and configure:

```csharp
arc.WithCapabilities(options => options.GatedEndpoints = new CapabilityEndpointSelector()
    .ForEndpointMetadata<ToolEndpointMetadata>());
```

Metadata matches assignable instances, including derived types. Pattern and metadata rules may be combined; they form a union, and selecting the same endpoint with both kinds is allowed. Pattern selection alone does not cover a different registered alias to the same handler. Unselected routes present at startup are outside the gate, so review every alias and prefer metadata for shared handlers. This option selects gated endpoints, not exemptions.

Startup rejects an explicitly empty selector or **any individual rule** matching no registered route endpoints with `CapabilityEndpointSelectorMatchesNothing`. Every route-pattern rule and every metadata-type rule must match at least one endpoint, even when another rule matches successfully. Blank patterns are rejected during configuration. Duplicate or case-colliding patterns, duplicate metadata types, and assignable metadata types are rejected with `CapabilityEndpointSelectorIsAmbiguous`. These checks do not replace the port and credential-installation requirements.

Only a routed endpoint instance positively known to be outside the selection bypasses capability verification on that port. The bypass does not install a capability caller or verified principal. Missing or routing-generated non-route endpoints remain gated. A registered, unselected `MapFallback` is a known outside route: it can capture otherwise unmatched paths and let those requests through without capability verification. The same applies when an endpoint-less branch or static-file request first matches a known outside fallback. Select the fallback too if it must be gated; an eventual 404 status does not classify admission.

The endpoint snapshot is frozen at startup. A new or replacement instance is unknown and gated if the gate sees it or it is assigned later, including a replacement for an outside route. However, the early router can continue matching a previously frozen outside instance or fallback even after a runtime data-source change. Adding an endpoint does not guarantee every request for its path becomes gated. Restart to classify the new graph consistently. Other ports retain their existing behavior.

Outside admission is also enforced across later endpoint assignments. Status-code or exception re-execution and rewriters that clear and re-run routing cannot use an outside route to reach a selected or unknown endpoint: it is replaced with a 401 refusal endpoint, and that refusal cannot be cleared by subsequent endpoint assignments. Clearing the endpoint to run routing again is allowed, but only another exact, positively outside instance may execute. A downstream router may construct new instances even for unchanged route text; those are unknown and fail closed. Do not replace the guarded `IEndpointFeature` or invoke endpoint delegates directly around admission.

Selective hosting runs routing immediately before the gate. Configure server-owned `UsePathBase` or URL rewriting **before** Arc's startup filter, for example in an earlier `IStartupFilter`. A path base configured later in the application's pipeline is not visible to early routing; unclassified requests remain gated. Forwarded-prefix, forwarded-host, and original-URL headers do not select outside endpoints. Later path rewriting or re-execution is subject to the endpoint-assignment guard described above, not a second authentication attempt. For Startup-class hosts with capability services registered, the filter populates its early router from the completed host route graph, including the host's own `UseRouting`/`UseEndpoints`; it refuses startup if the configured rules cannot be resolved rather than silently skipping selection. CORS preflight on selected or unclassified endpoints is gated too; later CORS or authorization middleware cannot exempt it. Only routing moves earlier, not the host's authentication or authorization policy.

## PAT-capable installations

A `WithPersonalAccessTokens` installation restricts selected capability endpoints to a single raw PAT bearer before resolving a caller. This also applies when signed browser-session verification is composed through `AllowPersonalAccessTokenCallers()`. A valid signed browser session, cookie, identity header, or existing authenticated principal cannot admit a capability caller on those endpoints. Preflight refusals return `401` without invoking the endpoint or publishing a caller. Unselected routes and other listeners are unchanged.

The [PAT adapter guide](asp-net-core/personal-access-token-adapters.md#compose-browser-issuance-and-pat-callers) shows one installation for browser-only issuance and PAT verification. It requires live authoritative storage, recovery, and policy adapters. A PAT cannot issue another PAT. Selected capability endpoints refuse forwarded envelopes; outside those endpoints, a forwarded PAT remains authority-verified and cannot issue another PAT. OAuth issuance is not installed by this composition. MCP serving is a separate package; see [MCP](mcp/index.md). Signed-only capability installations retain their existing browser-session behavior.

## Read the caller

Inject `Cratis.Arc.Capabilities.ICapabilityCallers` into capability code. Its `Current` property returns the admitted `CapabilityCaller`, including the endpoint group, protocol `rest`, principal kind, person, current delegated actor, client metadata, credential identifier, checked grant identifier, tenant, and scope ceiling. Outside a capability call it is null.

A declared client is metadata, not an authenticated actor. `Client.IsAuthenticated` distinguishes authenticated client metadata from declared metadata. The scopes are a verified ceiling, not an authorization decision.

## Session authority contract

Session identifiers MUST NOT confer authority. Every request MUST re-authenticate its own credential, check current credential and grant fences (including recovery state), and re-check live tenant membership and operation authorization. A session opened before revocation, rotation, or a membership change MUST NOT preserve earlier authority. Refuse the request if a required status authority is unavailable; never use a session snapshot as fallback.

The required revocation bounds depend on credential validation:

| Credential validation | Required bound |
| --- | --- |
| Live browser-session, PAT, or grant status | An acknowledged revocation or invalidating rotation takes effect on next use, including on another replica; no session grace period |
| Offline access-token validation | Previously issued authority is bounded by token lifetime plus accepted clock skew, at most 630 seconds; an online fence check may shorten this bound |

The 630-second bound applies only to authority established by offline token validation. Mandatory online credential and grant fences remain next-use checks; the offline bound is not a grace period for them. Offline token validity does not waive live membership or other required authoritative checks. These are contract requirements, not a claim that every adapter or credential profile is implemented. The released MCP behavior is described in [Session authority](mcp.md#session-authority-arc-mcp-authorization-v1): its stateless transport issues no server session ID.

Conforming stateful integrations MUST test open → revoke → call and open → invalidating rotation → call, both on the opening replica and on another replica, and refuse within the applicable bound. They MUST also test membership removal and required-authority outage after opening. Arc's stateless MCP checks do not establish conformance for a valid server-issued session ID.

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

- The public installers support signed v2 browser-session callers and resource-local PATs, including browser-only PAT issuance on the same resource. OAuth-grant, service-account, and on-behalf installation remain separate work.
- Product authorization still applies: a capability is not authority. The verified caller is the principal used by Arc authorization, the current-principal accessor, and Chronicle identity/causation. Roles, policies, and live tenant membership must authorize that subject.
- `[RequiresScope]` narrows access against the verified credential's scope ceiling. Missing scopes mean an empty ceiling. Product cookies, roles, or a same-subject scheme cannot widen it, and scopes cannot replace product authorization.
- A signed tenant is a candidate, not proof of membership. A conflicting authenticated subject is refused, not merged.
- Keep the group port private. Never expose it publicly or forward public traffic to it.
- By default, default-group endpoints reachable on the capability port also require a caller. Anonymous routes on that port require an explicit gated-endpoint selection as described above.
- This API hosts REST capabilities only. MCP serving and protected-resource metadata come from the `Cratis.Arc.Mcp` package; see [MCP](mcp/index.md).
