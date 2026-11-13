---
title: Personal access token adapters
description: Supply a trusted personal access token store, recovery authority, and current policy, register them, and run the refusal suite to prove unsafe cases fail.
---

Personal access token (PAT) adapters supply persistence, recovery evidence, and current policy to `Cratis.Arc.PersonalAccessTokens`. Use the public `IPatAuthority`, `IPatRecoveryEpoch`, and `IPatPolicy` ports when the host owns those services. Without an adapter, the module refuses issuance and verification.

## Trusted-host security contract

These ports are a **trusted-host security contract, not a sandbox**. Adapter code runs inside the host process and is fully trusted. It can read stored metadata and withhold results. The framework cannot make an adapter's claims about persistence, policy, or recovery evidence true.

Arc independently checks credential format, verifier matching, resource, audience, environment, expiry, lifetime limits, scopes, recovery epoch, and the current policy decision before accepting a credential. Persisted policy epochs are hints, not authorization. Current policy must check person, account, tenant, and organization status, membership, scope ceilings, and upstream sign-in freshness on every issuance and verification.

The public adapter API does not provide a way to mint a verified caller or construct or change `PatIssuedIntent` or `PatOwner`. Arc creates those bindings from checked caller facts; adapters can inspect them. `PatRecord` is adapter-owned metadata, not proof of authentication. Internal caller-verification and issuance-context APIs remain internal. This boundary is not protection against arbitrary code execution or reflection inside the trusted host.

## Port obligations

| Port | Required behavior |
| --- | --- |
| `IPatAuthority` | Atomically commit the verifier record and issuance intent. Enforce unique credential identifiers and verifiers. Read authoritative state with linearizable ordering against revocation and recovery advancement. Revoke atomically only for the exact checked owner. |
| `IPatRecoveryEpoch` | Read authoritative recovery evidence outside the credential store's restore domain. Return `null` or fail when evidence is missing or unavailable. Never recreate missing evidence during a read. Fence restored credentials with missing or strictly newer recovery evidence. |
| `IPatPolicy` | Reevaluate current status, membership, scopes, lifetime ceilings, and sign-in freshness. Deny on uncertainty or unavailability. Never permit with a non-positive lifetime ceiling. |

Use atomic writes with the store's required majority durability. Do not acknowledge a commit or revocation before its durability and ordering guarantees hold. Do not use positive caches for authoritative credential or recovery reads: a warmed process must not accept a revoked credential. Concurrent creation of the same verifier must have exactly one winner. Store snapshots must not alias caller-owned mutable scope or authentication-method collections.

Store only one-way verifiers, never raw tokens or reversibly encrypted token material. Verifiers are credential-sensitive even though they are not bearer tokens. Do not include tokens or verifiers in logs, exceptions, reports, or diagnostic strings. `PatRecord.ToString()` and `PatIssuedIntent.ToString()` omit the verifier; that does not make arbitrary serialization safe.

## Registration

Register one unkeyed implementation per port with standard dependency injection. This complete helper accepts your concrete adapter types:

```csharp
using Cratis.Arc.PersonalAccessTokens;
using Microsoft.Extensions.DependencyInjection;

public static class PatAdapterRegistration
{
    public static void Register<TAuthority, TRecovery, TPolicy>(IServiceCollection services)
        where TAuthority : class, IPatAuthority
        where TRecovery : class, IPatRecoveryEpoch
        where TPolicy : class, IPatPolicy
    {
        services.AddScoped<IPatAuthority, TAuthority>();
        services.AddScoped<IPatRecoveryEpoch, TRecovery>();
        services.AddScoped<IPatPolicy, TPolicy>();
    }
}
```

Install the module with `WithPersonalAccessTokens` and configure [verified callers](verified-callers.md) for the receiving resource. Register adapters before or after module installation; the startup check examines the completed service collection. A single adapter class may implement all three ports if it is registered exactly once for each port.

More than one custom registration for a port, including instance or factory registrations and differing lifetimes, refuses host startup. Keyed registrations for these ports are also refused. No custom registration is allowed and retains the unavailable, fail-closed defaults; it does not enable token issuance or verification.

The check uses `ValidateOnStart` and the generic host's `IStartupValidator`. Building or resolving a service provider without starting a host does not execute startup validation automatically. Such hosts must explicitly run startup validation before serving requests.

To add independently verified direct OAuth grants to the same PAT capability installation, see [OAuth and PAT capability callers](oauth-and-pat-callers.md). This is opt-in; do not add a second verified-caller installation. The external sample demonstrates public PAT issuance plus gated MCP and REST operations.

## Compose browser issuance and PAT callers

Use one `WithPersonalAccessTokens` call for both credential paths on the same resource. Configure signed v2 forwarding and explicitly call `AllowPersonalAccessTokenCallers()` in that call's delegate. Do not also call `WithVerifiedCallers`: a second installation is rejected.

The following **configuration fragment** assumes `arc`, public verification `settings`, and a persisted `PersonDirectory` as in the [verified caller guide](verified-callers.md). It selects `/mcp` as the gated endpoint while leaving the host's other routes unchanged:

```csharp
using Cratis.Arc;
using Cratis.Arc.Capabilities;
using Cratis.Arc.PersonalAccessTokens;

arc.WithCapabilities(options => options.GatedEndpoints = new CapabilityEndpointSelector()
        .ForRoutePattern("/mcp"))
    .WithPersonalAccessTokens(new PatOptions("stage-example-api") { AllowedScopes = ["content:write"] }, callers => callers
        .ForResource("https://api.example.test", "example-api", "sample")
        .UsePersonDirectory<PersonDirectory>()
        .UseSignedForwardedIdentityV2(v2 => v2
            .WithRealmIssuer("https://identity.example.test")
            .WithTenantHeader("x-example-tenant")
            .AddPublicKey(settings.KeyId, settings.PublicKeyPem)
            .UseProcessLocalNonceStore())
        .AllowPersonalAccessTokenCallers());
```

Keep the forwarding-hop client-certificate configuration from that guide, and register the three authoritative adapters described above. Process-local nonce storage is only a single-process development choice. The selected route pattern must match an actual registered endpoint; Arc does not provide an issuance HTTP endpoint here. To serve MCP tools on that route, use the `Cratis.Arc.Mcp` package; see [MCP](../mcp/index.md). `WithMcp` adds the MCP endpoint to the selected gated endpoints, so `GatedEndpoints` must be set.

On an unselected browser route, call `IPersonalAccessTokens.Issue` after authenticating the signed browser session. Issuance uses the verified persisted person, exact account key, tenant, receiving resource, environment, and upstream assurance; request fields or a claims principal cannot replace those facts. This composition supports **browser-session issuance only**. It installs no OAuth verifier, and a PAT cannot issue another PAT.

Selected capability endpoints require a single raw PAT bearer. Signed browser envelopes, session cookies, another authentication scheme, and identity headers do not admit a capability caller. Outside selected capability endpoints, a PAT carried inside a signed envelope is still bound to a fresh verification by the PAT authority. It remains a PAT caller and cannot issue another PAT, even if an envelope claims a browser or OAuth credential kind. Selected capability endpoints refuse every envelope and require a raw PAT bearer. PAT-plus-cookie, duplicate credentials, retired namespaces, and URI credentials are refused rather than falling back to a browser identity. PAT verification still reads current authoritative credential, recovery, and policy state for every request. Product membership and authorization remain required.

A PAT-only installation can omit forwarding configuration and `AllowPersonalAccessTokenCallers()` as before; its capability endpoints are also PAT-only. A signed-only `WithVerifiedCallers` installation without the opt-in retains its browser capability behavior. Missing signed-forwarding configuration with the opt-in, incomplete bindings, duplicate installation, or a PAT-capable host without a PAT verifier fail closed.

## Run the refusal suite

The framework-independent kit ships in the same PAT assembly under `Cratis.Arc.PersonalAccessTokens.Contract`; it does not require a separate testing package or a particular test framework. Every adapter must pass it against its real persistence and external recovery services, not only against an in-memory substitute.

Never run `PatAdapterContract.Run` against any production store: it issues real records and executes restore and outage hooks.

Call this complete helper from a test. Supply a factory returning a fresh, isolated `PatAdapterInstance` wired to your three ports:

```csharp
using Cratis.Arc.PersonalAccessTokens.Contract;

public static class PatAdapterChecks
{
    public static async Task Verify(
        Func<PatAdapterInstance> createIsolatedInstance,
        CancellationToken cancellationToken = default)
    {
        var harness = new PatAdapterHarness { CreateInstance = createIsolatedInstance };
        var report = await PatAdapterContract.Run(harness, cancellationToken);
        if (!report.Passed)
        {
            throw new InvalidOperationException(report.ToString());
        }
    }
}
```

Each instance must supply all six controls: `AdvanceRecoveryEpoch`, `Restore`, `SetOutage`, `RemoveRecoveryEvidence`, `DenyEverything`, and `Permit`. `Restore` must exercise a real restore, including an in-place restore, while preserving the external recovery boundary and fencing old credentials. `SetOutage` enables or clears outages of all three authorities. `Permit` changes the current lifetime ceiling. Missing controls fail rather than skip. Use the optional `Dispose` callback for asynchronous cleanup; the runner disposes each instance.

Prepare synthetic enabled person, account, tenant, organization, membership, scope, and sign-in policy for these fixed test facts:

| Fact | Value |
| --- | --- |
| Resource key | `stage-contract` |
| Resource / environment | `pat-contract-resource` / `contract` |
| Audience / issuer / identity provider | `https://pat-contract.example` |
| Person | `pat-contract-person` |
| Account subject | `00000000-0000-4000-8000-000000000001` |
| Tenant / organization | `pat-contract-tenant` / `pat-contract-organisation` |
| Scope / assurance / method | `read` / `basic` / `sso` |

Fixtures use current timestamps, a one-hour credential lifetime, and sign-in five minutes earlier. Initial recovery evidence must exist, and initial policy must permit those facts for issuance and verification. Never point the harness at live credentials or a shared production store.

The 22 named checks cover duplicates, malformed and noncanonical-case verifier lookups, 100 sequential revoke-then-read ordering checks with warmed reads, owner binding, concurrent creation, outages, mutable input isolation, cancellation, recovery fencing, current policy, and real module issuance and verification. Canonical verifier hex is uppercase; lowercase hex is noncanonical. The kit holds adapters to canonical uppercase lookup keys, while the framework verifier compares decoded digest bytes rather than hex casing. Each case has a one-minute limit, and a timeout fails. Caller cancellation propagates. Reports omit adapter exception text and credential material. A passing report is behavioral evidence, not certification or permission to trust an adapter without review.

## Audit boundary

The public persistence ports do not expose the audit outbox. `IPatAuditOutbox` and audit payload construction remain internal. See [personal access token audit](../identity/personal-access-token-audit.md) for the audit boundary; do not add external friend-assembly access to implement it.
