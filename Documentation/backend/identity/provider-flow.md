---
title: Provider flow
description: Understand when Arc enriches the current principal and when it reuses client-controlled identity details.
---

Authentication establishes a principal; identity enrichment supplies application-specific information for the frontend. Keeping those steps separate prevents a UI convenience from becoming an accidental permission boundary.

## Endpoint mapping

Normal `UseCratisArc()` activation maps `/.cratis/me` when `IProvideIdentityDetails` is registered, unless a replacement endpoint with the same endpoint name already exists. You do not need a second mapping call when using the standard host setup.

The endpoint has anonymous metadata so it can handle identity results itself. It calls `IIdentityProvider.Get()`, returns 401 for a result marked unauthenticated, 403 for a result marked unauthorized, and otherwise writes JSON plus the `.cratis-identity` cookie. These result flags can come from the cookie-first path; this endpoint is not an independent validation of a browser's cached identity.

## Identity details provider

Arc discovers `IProvideIdentityDetails` implementations. On a fresh request without a nonempty identity cookie, it checks the request principal, constructs `IdentityProviderContext`, and invokes the provider. The ID comes from the principal's `sub` claim (or `"unknown"` when absent); the name comes from the principal, with the forwarded name header as a fallback.

This **illustrative type fragment** supplies display data only, allowing every already-authenticated user through this enrichment step. It is not an application membership policy:

```csharp
using Cratis.Arc.Identity;

public class IdentityDetailsProvider : IProvideIdentityDetails
{
    public Task<IdentityDetails> Provide(IdentityProviderContext context) =>
        Task.FromResult(new IdentityDetails(true, new { DisplayName = context.Name }));
}
```

For an application-entry decision, your provider must consult authoritative membership data. That decision still does not protect every command/query: enforce those permissions in their pipelines.

## Cached identity and frontend integration

```mermaid
flowchart TD
    Request --> Cookie{Nonempty identity cookie?}
    Cookie -- Yes --> Cached[Deserialize client-controlled result]
    Cookie -- No --> Principal[Check authenticated request principal]
    Principal --> Provider[Compose details using provider]
    Cached --> Result[Identity response]
    Provider --> Result
    Result --> Browser[Frontend presentation]
```

Frontend support reads `.cratis-identity` directly. It is unsigned, JavaScript-readable base64 JSON; a cached result bypasses provider recomputation. It does not add trusted claims or authorize pipeline execution. Read [cookie trust and mutation limits](identity-provider-service.md) before using `IIdentityProvider` in backend code.

For client consumption, see [React identity integration](../../frontend/react/identity.md).
