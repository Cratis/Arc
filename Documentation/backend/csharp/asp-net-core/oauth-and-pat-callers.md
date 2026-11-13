---
title: OAuth and PAT capability callers
description: Compose independently verified direct OAuth person grants and personal access tokens on one capability-gated resource.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# OAuth and PAT capability callers

Use `AllowOAuthBearerCallers<TVerifier>()` inside the **same** `WithPersonalAccessTokens` delegate that installs signed v2 forwarding and calls `AllowPersonalAccessTokenCallers()`. Do not install `WithVerifiedCallers` a second time. See the executable [OAuth/PAT acceptance sample](../../../../Samples/OAuthPatCallerAcceptance/README.md) for a complete external application with MCP and REST endpoints.

This is an opt-in composition, not another authentication pipeline. Without the opt-in, OAuth grants remain refused on the PAT capability path. The receiving resource still requires a trusted forwarding-hop certificate, a valid signed v2 envelope, the exact covered bearer, a persisted person mapping, current tenant membership, and endpoint capabilities. Browser sessions may issue PATs on the sample's separate issuance route; they cannot call its gated MCP or REST operations.

## Install the composition

The following excerpt is the caller configuration from the executable sample's `Program.ConfigureCallers`. Its `WithPersonalAccessTokens` delegate calls this method; `settings`, `SampleSettings`, `PersonDirectory`, and `OAuthVerifier` are sample types. The complete host also installs PAT authority/recovery/policy adapters, tenant membership, certificate trust, and capability endpoint selection.

```csharp
static void ConfigureCallers(VerifiedCallersBuilder callers, SampleSettings settings)
{
    callers.ForResource(SampleSettings.Resource, SampleSettings.Audience, "acceptance")
        .UsePersonDirectory<PersonDirectory>().AllowPersonalAccessTokenCallers()
        .UseSignedForwardedIdentityV2(forwarding => forwarding
            .WithRealmIssuer(SampleSettings.Issuer)
            .WithTenantHeader("X-Tenant-ID")
            .AddPublicKey("sample-key", settings.PublicKeyPem)
            .UseProcessLocalNonceStore());
    if (settings.AllowOAuth) callers.AllowOAuthBearerCallers<OAuthVerifier>();
}
```

`VerifiedCallersBuilder` and `IOAuthBearerVerifier` are in `Cratis.Arc.Identity.Callers`. Enable `settings.AllowOAuth` to install the verifier. The opt-in requires both signed forwarding and PAT callers in that same installation; combining it with workload callers or installing a second caller pipeline is rejected.

REST endpoints must carry `AcceptsOAuthCapability.Instance` metadata in addition to being selected by the capability gate. For MCP, select the `Combined` profile to accept PAT and OAuth callers. Profiles do not install verification: see [MCP authorization](../mcp/authorization.md) and [MCP client profiles](../mcp/connect-clients.md).

## Independent OAuth verification

Implement the public `IOAuthBearerVerifier` port. `OAuthBearerPresentation` carries credential-sensitive authorization text; do not log, serialize, or put it in diagnostic output. Verify it with the issuer's authoritative introspection service or an equivalently authoritative validation process. The sample uses a synthetic in-memory authority, not a production identity provider or durable token store.

Return `OAuthBearerVerification.Verified(new OAuthBearerVerified { ... })` only for an active direct person grant with authoritative account, audience, credential identifier, client identifier, scopes, and expiry. Return `Refused()` for invalid, expired, revoked, machine/client-credentials, delegated, or sender-constrained tokens. Missing authoritative evidence, exceptions, and cancellation fail closed. `ConfirmationKeyThumbprint` must be non-null for **any** `cnf` evidence, including an empty confirmation object (use an empty string when no thumbprint is available); this composition supports only direct unbound grants.

Arc compares independently verified issuer/subject, audience, credential identifier, and every supplied signed client/scope ceiling. A signed envelope is not OAuth verification. Signed identity text, a client identifier, or a scope header alone cannot authenticate a bearer. Absent optional signed client/scope metadata uses verified token facts; explicitly empty or malformed supplied metadata is refused.

The capability composition admits only `Person` principals whose credential kind is PAT or OAuth grant, with no actor, delegation grant, or sender-binding metadata. These checks also run after a caller has already been resolved earlier in the request. They do not change the general signed v2 protocol or its other supported credential kinds.

## No OAuth-to-PAT exchange

Enabling OAuth capability callers does **not** enable token exchange. In this composition, `IPersonalAccessTokens.Issue` refuses an `OAuthGrant` caller with `pat_issuance_refused` before policy evaluation, secret generation, or persistence. There is no builder opt-in for OAuth-to-PAT exchange. A downstream issuance route must not treat a valid OAuth capability call as permission to mint a longer-lived PAT.

The sample's separate `/issue` route supports signed browser issuance and returns a generic refusal for an OAuth grant; its MCP surface exposes only a read tool, not issuance. Browser-to-PAT issuance remains supported. Hosts that do not install this OAuth/PAT composition retain their existing PAT issuance behavior, and the general forwarded-identity v2 protocol is unchanged.

## No credential fallback

A PAT-shaped bearer selects the PAT authority and cannot fall through to OAuth, including malformed PATs and envelopes labeled OAuth. A selected OAuth verifier cannot fall back to the PAT authority. Raw OAuth bearers have no signed coverage and are refused. Existing raw PAT admission remains supported through the PAT authority; signed PAT envelopes retain their signed-context checks. Duplicate credentials, raw bearer plus a recognized session cookie, credentials in the URI, retired prefixes, bad signatures, replay, wrong audience, and unknown mapping are refused before an operation dispatches.

Capability scopes, tenant membership, transport admission, PAT recovery fencing, and current PAT policy remain independent checks. A valid token is not itself authorization to invoke an operation. Separate default and capability listeners retain their existing endpoint behavior.

## AuthProxy contract and producer limits

The receiving contract is pinned to AuthProxy **v2.62.2**, specifically `ForwardedIdentityV2ContextHeaders`: `oauth-grant` person evidence uses the trusted realm issuer, a UUID-v4 subject, identity provider, `basic` authentication context, and the covered bearer hash. See [signed forwarded identity v2](signed-forwarded-identity-v2.md) for the signature and transport requirements.

This is a context-header contract pin, not a claim that that tag produces OAuth envelopes. AuthProxy v2.62.2's built-in envelope producer supports **browser sessions only** and refuses OAuth/PAT envelope production. Its context-header list also omits `x-cratis-token-scope` and `x-cratis-token-client-id`; do not assume those optional ceilings are signed by that producer. When absent, Arc uses independently verified token scopes/client facts, never unsigned replacements. A compatible producer and a separate interoperability check are required before deploying this OAuth path through AuthProxy.

## Verification and limits

`Samples/OAuthPatCallerAcceptance.Specs` drives both real MCP HTTP tool calls and capability-annotated REST through the public host setup. Its refusal facts check both transport outcomes, source read counts, and zero operation dispatches. Cached-resolution cases exercise a startup filter that resolves callers before Arc's gate.

The harness signs synthetic envelopes independently using the documented v2 framing and uses public PAT issuance. It does not reference Arc internals or claim cross-repository AuthProxy-to-Arc end-to-end verification. A real AuthProxy interoperability run is a separate check and has not been performed here. Production deployments need a fleet-safe nonce store and durable, recovery-fenced PAT adapters; the process-local and in-memory sample implementations are for tests only.
