---
title: Install verified callers in Cratis Arc
description: Configure signed v2 browser callers with an explicit resource binding, person directory, public verification keys, and replay store for capability hosting.
---

Cratis Arc can install a signed v2 **browser-session** caller for a receiving capability resource. The verified person becomes the principal used by Arc authorization, `ICurrentPrincipalAccessor`, and Chronicle identity/causation when Chronicle is installed. A signature authenticates the caller; it does not grant product access.

## Install one receiving resource

The [runnable acceptance sample](https://github.com/Cratis/Arc/blob/main/Samples/VerifiedCallerAcceptance/README.md) shows a complete host and product-owned authorization. This **configuration fragment** belongs in its `Program.Build`, where `builder`, `settings`, and the sample's `PersonDirectory` are available:

```csharp
using Cratis.Arc;
using VerifiedCallerAcceptance;

builder.AddCratisArc(
    options =>
    {
        options.EndpointGroups.Assign("capabilities", settings.CapabilityPort);
        options.Tenancy.HttpHeader = "x-example-tenant";
    },
    arc => arc.WithoutControllers().WithCapabilities().WithVerifiedCallers(callers => callers
        .ForResource("https://api.example.test", "example-api", "sample")
        .UsePersonDirectory<PersonDirectory>()
        .UseSignedForwardedIdentityV2(v2 => v2
            .WithRealmIssuer("https://identity.example.test")
            .WithTenantHeader("x-example-tenant")
            .AddPublicKey(settings.KeyId, settings.PublicKeyPem)
            .UseProcessLocalNonceStore())));
```

Configure the corresponding server listeners, then call `app.UseCratisArc()`, as in the sample. The capability port must be reachable only through the trusted ingress. An endpoint-group assignment alone does not establish that boundary.

`WithVerifiedCallers` installs exactly one resource/audience/environment binding without normalizing it. It is separate from `WithCapabilities`, which enables the request gate. Missing binding values, a missing person directory, missing realm issuer or keys, and a second installation throw `InvalidVerifiedCallerInstallation` during registration, before requests are served. Capability hosting without an installation continues to fail startup.

`AddPublicKey` accepts public-only P-256 PEM material. Private keys, other curves, and malformed PEM throw `InvalidForwardedIdentityPublicKey`. Keep signing keys at the proxy, not the receiving backend. Key identifiers are exact and unique; a duplicate identifier is a configuration error.

## Supply a persisted-person directory

Implement `Cratis.Arc.Identity.Callers.ICallerPersonDirectory`. Its `Find(CallerAccountKey, CancellationToken)` returns `CallerPersonFound.Found(CallerPersonId)` only for an existing persisted account mapping; otherwise return `CallerPersonFound.NotFound()`. The result type is in `Cratis.Arc.Identity.Callers`.

The account key consists of the exact trusted realm issuer and canonical lowercase UUID subject. `idp` describes the upstream connection; it is not the account-key namespace. Arc registers the selected directory in the request scope, so it can depend on scoped persistence services. Do not provision people or infer authorization from forwarded headers. The sample directory contains synthetic persisted mappings solely to keep the proof self-contained.

## Select replay protection explicitly

No nonce store is selected by default. A missing choice throws `ForwardedIdentityNonceStoreNotConfigured`. `UseProcessLocalNonceStore()` is an explicit **single-process sample/development** choice, not fleet replay protection.

For multiple receiving replicas, use `UseNonceStore<TStore>()`, where the product's singleton, concurrency-safe store implements `IForwardedIdentityNonceStore`. Its existing asynchronous `Reserve(aud, kid, nonce, timestamp, now, cancellationToken)` contract returns `Reserved`, `Replayed`, or `Unavailable`. Reservations must be atomic and shared across replicas, survive the required replay window, and fail closed on outages or capacity limits. Do not replace an unavailable fleet store with a local fallback.

## Keep authorization in the product

A valid caller is evaluated as its verified subject. Product roles, policies, and live tenant membership must still authorize that subject. Claims transformation can enrich roles, but every authenticated identity it adds must preserve that subject; a different or missing subject is not an enrichment of the same person.

`[RequiresScope("content:write")]` narrows an operation to callers whose **verified credential scope ceiling** contains that scope. Scope comparison is exact. An absent ceiling is empty, not unrestricted. Claims, roles, another authentication scheme, or a same-subject product cookie cannot widen it. Conversely, a scope cannot substitute for product authorization: an editor-only operation still refuses a non-editor with `content:write`.

A request already authenticated as a different subject is refused rather than merged with the verified person. A signed tenant header is only a candidate: Arc checks live `ITenantMembership` against the verified principal before publishing it or invoking the operation. The sample covers both missing roles and tenant non-membership and checks that neither invokes the command.

## Run the acceptance proof

From the repository root:

```sh
dotnet test Samples/VerifiedCallerAcceptance.Specs/VerifiedCallerAcceptance.Specs.csproj -c Release
```

The specs use only public APIs, a real HTTP host, and synthetic signing material from the shared v2 fixture. They compare the test signer's framing to the shared vector bytes, then exercise command/query dispatch, scopes, roles, membership, replay, key/audience mismatch, missing people, unsigned forwarding, and listener isolation. The command returns `ICurrentPrincipalAccessor`'s subject, the same identity source used for causation; this sample does not append Chronicle events.

## Boundaries

This installer supports the signed v2 browser-session flow. It does not install PAT, OAuth-grant, service-account, bearer-verifier, or on-behalf adapters. Those flows cannot be enabled by changing a header or credential kind. MCP serving, `--as` clients, protected-resource metadata, and OAuth registration are separate work.

For proxy-side framing, ingress admission, rotation, and replay retention requirements, see [signed forwarded identity v2](signed-forwarded-identity-v2.md). For capability listener exposure, see [capability hosting](../capability-hosting.md).
