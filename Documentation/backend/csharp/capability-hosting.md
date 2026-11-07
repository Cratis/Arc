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

builder.WebHost.UseUrls($"http://127.0.0.1:{settings.DefaultPort}", $"http://127.0.0.1:{settings.CapabilityPort}");
builder.AddCratisArc(
    options => options.EndpointGroups.Assign("capabilities", settings.CapabilityPort),
    configureBuilder: arc => arc.WithCapabilities().WithVerifiedCallers(callers => callers
        .ForResource("https://api.example.test", "example-api", "sample")
        .UsePersonDirectory<PersonDirectory>()
        .UseSignedForwardedIdentityV2(v2 => v2.WithRealmIssuer("https://identity.example.test")
            .AddPublicKey(settings.KeyId, settings.PublicKeyPem)
            .UseProcessLocalNonceStore())));
```

Tag capability commands, read models, or query methods with `[EndpointGroup("capabilities")]`. Group membership is the explicit exposure choice; commands and queries outside that group retain their existing routing. Enabling capabilities without assigning the group's port fails startup with `CapabilityEndpointGroupHasNoPort`. Startup also fails with `CapabilityHostingHasNoCredentialInstallation` when no credential verifier and receiving installation are registered.

For a different group name, set `EndpointGroup` through the `WithCapabilities` configuration delegate and assign a port to that same group. The [verified caller guide](asp-net-core/verified-callers.md) explains custom signed tenant headers and fleet-wide nonce stores. Process-local replay protection is only a single-process sample choice.

## Select gated endpoints on a shared listener

Leave `GatedEndpoints` unset to gate the whole capability port, including health probes. To share that listener with existing routes, explicitly select the **gated** endpoints:

```csharp
arc.WithCapabilities(options => options.GatedEndpoints = new CapabilityEndpointSelector()
    .ForRoutePattern("/tools/{name}"));
```

This configuration fragment uses `Cratis.Arc.Capabilities`. Patterns match the registered endpoint's `RoutePattern.RawText` exactly with ordinal comparison: include route parameters, preserve case and slashes, and do not use a request path, prefix, wildcard, or regular expression. ASP.NET Core routing decides which endpoint a request reaches; the gate never compares the request path to these patterns.

For endpoints with aliases, prefer server-attached metadata. Define a marker type such as `ToolEndpointMetadata`, attach an instance with `.WithMetadata(new ToolEndpointMetadata())` to **every** alias's endpoint builder, and configure:

```csharp
arc.WithCapabilities(options => options.GatedEndpoints = new CapabilityEndpointSelector()
    .ForEndpointMetadata<ToolEndpointMetadata>());
```

Metadata matches assignable instances, including derived types. Pattern and metadata rules may be combined; they form a union, and selecting the same endpoint with both kinds is allowed. Pattern selection alone does not cover a different registered alias to the same handler. Unselected routes present at startup are outside the gate, so review every alias and prefer metadata for shared handlers. This option selects gated endpoints, not exemptions.

Startup rejects an explicitly empty selector or one selecting no registered route endpoints with `CapabilityEndpointSelectorMatchesNothing`. Blank patterns are rejected during configuration. Duplicate or case-colliding patterns, duplicate metadata types, and assignable metadata types are rejected with `CapabilityEndpointSelectorIsAmbiguous`. These checks do not replace the port and credential-installation requirements.

Only a routed endpoint positively known to be outside the selection bypasses capability verification on that port. The bypass does not install a capability caller or verified principal. Missing endpoints, 404/405 routing results, endpoint-less branches, static files, and endpoint instances added or replaced after startup remain gated. The endpoint snapshot is frozen at startup; changes require a restart to classify new outside endpoints. Other ports retain their existing behavior.

Selective hosting runs routing immediately before the gate. Configure server-owned `UsePathBase` or URL rewriting **before** Arc's startup filter, for example in an earlier `IStartupFilter`. A path base configured later in the application's pipeline is not visible to early routing; unclassified requests remain gated. Forwarded-prefix, forwarded-host, and original-URL headers do not select outside endpoints. Never rewrite paths, change the endpoint, or re-run routing after this gate on the capability listener, including status-code re-execution: doing so would invalidate the admission decision. CORS preflight on selected or unclassified endpoints is gated too; later CORS or authorization middleware cannot exempt it. Only routing moves earlier, not the host's authentication or authorization policy.

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

- The public installer supports signed v2 browser-session callers; PAT, OAuth-grant, service-account, bearer-verifier, and on-behalf installation are separate work.
- Product authorization still applies: a capability is not authority. The verified caller is the principal used by Arc authorization, the current-principal accessor, and Chronicle identity/causation. Roles, policies, and live tenant membership must authorize that subject.
- `[RequiresScope]` narrows access against the verified credential's scope ceiling. Missing scopes mean an empty ceiling. Product cookies, roles, or a same-subject scheme cannot widen it, and scopes cannot replace product authorization.
- A signed tenant is a candidate, not proof of membership. A conflicting authenticated subject is refused, not merged.
- Keep the group port private. Never expose it publicly or forward public traffic to it.
- By default, default-group endpoints reachable on the capability port also require a caller. Anonymous routes on that port require an explicit gated-endpoint selection as described above.
- MCP serving and protected-resource metadata follow separately; this API hosts REST capabilities only.
