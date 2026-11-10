---
title: Install verified callers in Cratis Arc
description: Configure signed v2 browser callers and read verified request facts while keeping membership and operation authorization in the product.
---

Cratis Arc can install a signed v2 **browser-session** caller for a receiving capability resource. The verified person becomes the principal used by Arc authorization, `ICurrentPrincipalAccessor`, and Chronicle identity/causation when Chronicle is installed. Arc accepts signed v2 callers only through an admitted forwarding hop: the immediate TLS peer must present a client certificate from a trusted CA with an allowed peer identity. The signature and hop admission do not grant product access.

## Install one receiving resource

The runnable acceptance sample in the repository's `Samples/VerifiedCallerAcceptance` folder shows a complete host and product-owned authorization. This **configuration fragment** belongs in its `Program.Build`, where `builder`, `settings`, and the sample's `PersonDirectory` are available:

```csharp
using System.Net;
using Cratis.Arc;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using VerifiedCallerAcceptance;

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Listen(IPAddress.Loopback, settings.CapabilityPort, listen => listen.UseHttps(settings.ServerCertificate, https =>
    {
        https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
        https.AllowAnyClientCertificate();
    }));
    kestrel.Listen(IPAddress.Loopback, settings.DefaultPort);
});
builder.AddCratisArc(
    options =>
    {
        options.EndpointGroups.Assign("capabilities", settings.CapabilityPort);
        options.Tenancy.HttpHeader = "x-example-tenant";
        options.ForwardedIdentity.ClientCertificate.Required = true;
        options.ForwardedIdentity.ClientCertificate.TrustedCertificateAuthoritiesPath = settings.TrustedCertificateAuthoritiesPath;
        options.ForwardedIdentity.ClientCertificate.AllowedDnsNames.Add(settings.AllowedPeerDnsName);
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

`settings.ServerCertificate` is the backend's TLS certificate with its private key; the trusted CA bundle and allowed DNS name identify the forwarding proxy's client certificate, not the browser. The sample uses `AllowCertificate` and delegates peer validation to Arc through `AllowAnyClientCertificate()`. This lets the acceptance specs prove Arc returns HTTP 401 without dispatch when a valid signed envelope arrives without a client certificate. Arc refuses signed v2 callers without a trusted client certificate regardless of the transport mode, including when `ClientCertificate.Required` is false. Production hosts may additionally require the certificate at TLS with `ClientCertificateMode.RequireCertificate`, rejecting missing certificates before HTTP.

Call `app.UseCratisArc()` after building the host, as in the sample. The capability port must be reachable only through the trusted ingress. An endpoint-group assignment alone does not establish that boundary.

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

## Read the current caller

Backend handlers and HTTP-hosted tools can consume `IVerifiedCallerAccessor` from `Cratis.Arc.Identity.Callers`. It reads framework-verified facts for the current request; it does not turn the current `ClaimsPrincipal`, arbitrary headers, query values, or a tool payload into verified identity.

The signed v2 installation above supplies the supported browser-session source. Publishing the accessor adds no new credential source: existing host bearer validation and v1 forwarded identity do not guarantee the required credential identity and assurance. A bearer-shaped presentation without a matching trusted source is refused with `NoMatchingSource`.

This handler illustrates the consumer boundary, not MCP server registration or token verification. The host supplies the accessor, and `authorize` is the product's own membership and operation policy:

```csharp
using Cratis.Arc.Identity.Callers;

public sealed class ToolHandler(IVerifiedCallerAccessor callers)
{
    public async Task<VerifiedCaller?> Handle(Func<VerifiedCaller, bool> authorize)
    {
        var resolution = await callers.GetCurrent();
        return resolution.Refusal is null && resolution.Caller is { } caller && authorize(caller) ? caller : null;
    }
}
```

A refusal never falls back to user claims or a client-declared person. A successful resolution still passes through `authorize`: verified tenant binding is not tenant membership, and verified scopes and permissions are ceilings, not an operation decision. Return your tool's normal denial response when the handler returns null. Identity display data returned through `/.cratis/me` is a [separate concern](../identity/index.md), not an alternative verified caller source.

`GetCurrent()` resolves lazily and caches both success and refusal for the unchanged request. Changes to consumed request inputs poison reuse with `RequestChanged`; another request resolves independently. Outside a request it returns no caller and `NoCredential`. Do not retain a caller as authority for a later request.

### Caller fields

`CallerResolution.Caller` is a `VerifiedCaller` on success; `Refusal` classifies failure without exposing credential contents. Both records have framework-only constructors and initialization. Application code can read them but cannot construct or alter trusted caller or resolution fields. The verifier seam and the principal-stamping machinery remain internal.

| Fields | Meaning |
| --- | --- |
| `ContractVersion` | The locally assigned verified-caller contract version. |
| `PrincipalKind`, `CredentialKind`, `CredentialId` | The verified principal and credential categories and a non-secret credential identifier. |
| `AccountKey`, `Person`, `Assurance` | Person-specific verified account, person mapping, and assurance; absent for service callers. |
| `ServicePrincipal` | The verified service identity, separate from a person. |
| `Actor`, `GrantId` | A delegated actor and its checked grant, separate from the delegating person. |
| `Client` | Client metadata. A PAT label stays `Declared` and never grants actor authority. |
| `Resource`, `Tenant`, `Organisation`, `Environment`, `PolicyEpoch` | Checked receiving and policy bindings, not independent product authorization. |
| `Scopes`, `Permissions` | Immutable snapshots of verified scope and permission ceilings. |
| `SenderBinding` | A successfully checked sender binding, or null; never an unchecked proof. |

The return model distinguishes person and delegated callers from organization automation, cluster workloads, and runners; those model categories do not enable the adapters excluded below. A delegated actor does not replace the person. Service callers must not acquire person or delegation fields. Use the typed fields instead of reconstructing identity from claims.

## Run the acceptance proof

From the repository root:

```sh
dotnet test Samples/VerifiedCallerAcceptance.Specs/VerifiedCallerAcceptance.Specs.csproj -c Release
```

The specs use only public Arc APIs, a real HTTPS capability listener, synthetic signing material from the shared v2 fixture, and freshly generated test CA, server, and forwarding-peer certificates. Clients pin the test server certificate; no machine trust-store changes are needed. They compare the test signer's framing to the shared vector bytes, then exercise command/query dispatch, scopes, roles, membership, replay, key/audience mismatch, missing people, unsigned forwarding, and listener isolation. The command returns `ICurrentPrincipalAccessor`'s subject, the same identity source used for causation; this sample does not append Chronicle events.

## Boundaries

`WithVerifiedCallers` supports the signed v2 browser-session flow. To compose browser-session PAT issuance with raw PAT verification on the same resource, use one `WithPersonalAccessTokens` call with signed forwarding and `AllowPersonalAccessTokenCallers()` as shown in [personal access token adapters](personal-access-token-adapters.md#compose-browser-issuance-and-pat-callers). In that mode, selected capability endpoints accept only raw PAT bearers; unselected browser routes retain their behavior. Do not install both helpers separately.

The signed-only installer does not install PAT, OAuth-grant, service-account, bearer-verifier, or on-behalf adapters. Those flows cannot be enabled by changing a header or credential kind. MCP serving, `--as` clients, protected-resource metadata, and OAuth registration are separate work.

Client-declared names, scope strings, grant references, or body arguments cannot supply missing verified facts or upgrade actor authority. The accessor does not implement PAT authentication, OAuth introspection, DPoP verification, workload verification, or an MCP server.
The public, released `IVerifiedCallerAccessor` can be injected into a command, query, or tool through scoped DI. Call `GetCurrent()` for the current request; a refusal conveys no authority. Verification is not operation authorization.

The construction boundary checks row 18's missing verified grant, not full conformance. Row 26 (request grant-b versus verified grant-a) remains deferred to the future grant-lookup verifier. Header-only HttpListener requests retain the fail-closed `UnreadableCredentialHeaders` outcome and select no verifier.


The locked identity vectors are only partially exercised at existing Arc boundaries. Construction checks consume already verified facts, including proof outcomes; they do not prove cryptographic verification, proof replay detection, or full vector conformance. Credential counting for a session cookie plus a bearer (zero-based row 28 of the locked principals-and-workload artifact) is covered: a recognized session cookie together with an `Authorization` value or a v1 envelope is refused as multiple credentials before any caller is built, while a session cookie with a v2 envelope counts as one credential. Recognized session cookie names are a fixed internal default (`cratis-identity-session`, `.AspNetCore.Cookies`) plus the cookie names of the cookie authentication schemes registered in the ASP.NET host. Each name also matches its numbered chunks (`<name>C<n>`), case-insensitively, across `;`- and `,`-separated pairs; other cookies are ignored. Full MCP integration and the remaining credential-verification work are not complete.

For proxy-side framing, ingress admission, rotation, and replay retention requirements, see [signed forwarded identity v2](signed-forwarded-identity-v2.md). For capability listener exposure, see [capability hosting](../capability-hosting.md).
