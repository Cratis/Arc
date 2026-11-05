---
title: Authenticate requests in Cratis Arc.Core
description: Establish a trusted request principal in Cratis Arc.Core with authentication handlers, and configure the trust boundary for forwarded identity headers.
---

In a lightweight `Cratis.Arc.Core` service, authentication handlers establish who sent an HTTP request by producing a `ClaimsPrincipal`, the .NET object that holds the caller's identity and claims. Configure this before protecting commands or queries with roles and policies. ASP.NET Core applications use their host's authentication instead; follow [Microsoft Identity integration](../asp-net-core/microsoft-identity.md) for that setup.

## Authentication flow

Arc discovers `IAuthenticationHandler` implementations through `IInstancesOf<IAuthenticationHandler>` and tries them sequentially:

1. `AuthenticationResult.Anonymous` means this handler does not apply; try the next one.
2. `AuthenticationResult.Succeeded(principal)` stops the sequence and supplies the request principal.
3. `AuthenticationResult.Failed(reason)` stops the sequence without authenticating.

Do not depend on a particular discovery order or assume individual dependency-injection registrations define that order. If mechanisms overlap, make their applicability unambiguous. An earlier success means later handlers cannot veto it.

```mermaid
flowchart LR
    Request --> Handlers[Applicable authentication handler]
    Handlers --> Principal[Validated principal]
    Principal --> Pipeline[Arc authorization and validation]
    Pipeline --> Handler[Command or query logic]
```

## Implementing an authentication handler

The fragment below connects bearer credentials to Arc; it is not a complete JSON Web Token (JWT) implementation. `ITokenValidator` is an application-owned contract, so you must implement and register it before using the handler. Use your identity library to verify the token's signature, trusted issuer, audience, lifetime, and applicable revocation requirements. Decoding a token alone does not validate it.

```csharp
using System.Security.Claims;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;

public interface ITokenValidator
{
    Task<ClaimsPrincipal?> Validate(string token);
}

public class BearerTokenAuthenticationHandler(ITokenValidator validator) : IAuthenticationHandler
{
    public async Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        if (!context.Headers.TryGetValue("Authorization", out var header) ||
            !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticationResult.Anonymous;
        }

        var token = header["Bearer ".Length..].Trim();
        if (token.Length == 0)
        {
            return AuthenticationResult.Failed("Invalid credentials");
        }

        try
        {
            var principal = await validator.Validate(token);
            return principal?.Identity?.IsAuthenticated == true
                ? AuthenticationResult.Succeeded(principal)
                : AuthenticationResult.Failed("Invalid credentials");
        }
        catch (Exception)
        {
            return AuthenticationResult.Failed("Authentication unavailable");
        }
    }
}
```

Missing bearer credentials let another mechanism try. Empty, invalid, or unverifiable bearer credentials never become a successful principal. Log operational failures server-side without recording tokens or disclosing validation internals to clients. API keys and passwords likewise need a real credential store and validator; never compare against sample hardcoded secrets or put credentials into claims.

## Microsoft Identity Platform (Azure)

Core includes `Cratis.Arc.Identity.MicrosoftIdentityPlatformAuthenticationHandler`, discovered alongside other authentication handlers. It can read the Azure-style forwarded identity headers below, so you do not need to write another parser for them:

| Header | Purpose |
| --- | --- |
| `x-ms-client-principal-id` | User ID, decoded when it carries the RFC 8187 `UTF-8''` form |
| `x-ms-client-principal-name` | Display name |
| `x-ms-client-principal` | Base64 JSON principal containing roles and claims |

In the unsigned-header mode described here, the handler requires all three headers and rejects an invalid principal representation. It replaces forwarded subject/identifier claims and takes the reserved `MicrosoftIdentityPlatformClaims.IdentityProvider` claim from the payload's `identityProvider` field. That gives the claim one source within the payload; it does not prove who sent the headers.

:::caution[Forwarded headers need a trusted sender]
These headers alone are not signed credentials. Deploy this mechanism only behind trusted ingress: a gateway or proxy that authenticates callers, strips caller-supplied identity headers, writes its own values, and prevents direct access to the backend. A custom `X-User-ID` or `X-User-Role` header needs the same protections. Adding a bearer validator does not secure a separately accepted forwarded-header mechanism.
:::

Unless signed forwarding is enabled separately, the handler ignores the forwarded headers and returns `AuthenticationResult.Anonymous` until the host opts in with `Cratis:Arc:TrustForwardedIdentityHeaders` (`ArcOptions.TrustForwardedIdentityHeaders`). The setting defaults to `false` and applies to every request, including protected [introspection catalogs](../introspection/index.md). Turn it on only behind the trusted ingress described above:

```json
{
  "Cratis": {
    "Arc": {
      "TrustForwardedIdentityHeaders": true
    }
  }
}
```

The first request that carries the headers while they are not trusted logs a warning that names this setting.

In this unsigned-header mode, a request carrying `x-cratis-on-behalf-actor` or `x-cratis-on-behalf-assertion` also needs `Cratis:Arc:OnBehalfOf:Enabled` and a verified signed assertion; otherwise authentication fails. A verified forward authenticates as the represented user, with the actor attached as `ClaimsIdentity.Actor`. See [Acting on behalf of a user](../asp-net-core/acting-on-behalf-of.md).

`ArcOptions.ForwardedIdentity.Enabled` selects a separate signed-forwarding path before the unsigned-header handling above. In that mode, the handler verifies the forwarding signature rather than relying on `TrustForwardedIdentityHeaders`; enabling trust alone is not a signed-forwarding setup.

Arc cannot detect a custom handler that reads forwarded headers. If yours does, check the same option through `IOptions<ArcOptions>` and return `AuthenticationResult.Anonymous` when `TrustForwardedIdentityHeaders` is `false`.

In the ASP.NET Core package, the corresponding registration is `builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication()`. It is not the Core registration API. See [Microsoft Identity Platform](../asp-net-core/microsoft-identity.md) for that host's setup and local-development principals.

## Endpoint enforcement and limits

The lightweight authentication middleware places a successful principal on `IHttpRequestContext.User`. With handlers present, an endpoint that does not allow anonymous access returns HTTP 401 if authentication fails. If its metadata specifies `Roles`, an authenticated caller without any listed role receives 403. An endpoint with `AllowAnonymous = true` proceeds even when credentials fail and bypasses the role check.

With no handlers, the middleware proceeds without authenticating unless the endpoint metadata sets `RequireAuthentication = true`. For that endpoint it throws `AuthenticationRequiredWithoutHandlers` (`Cratis.Arc.Http`) instead. Without that property, metadata alone does not protect against a missing authentication setup.

Anonymous access means credentials are not required, not that an identity is unwanted. Handlers therefore run on `AllowAnonymous` endpoints by default. Arc's current-caller identity and observable query demultiplexer endpoints can use the principal of a signed-in caller even though they allow anonymous requests.

Discovery endpoints have a different default: they are anonymous only in Development, or when `ArcOptions.Introspection.RequireAuthentication` is explicitly `false`. Elsewhere they require authentication and remain unmapped with a warning if the host cannot authenticate.

If your handler should do no work on anonymous endpoints, implement `AppliesToAnonymousEndpoints => false`. Arc skips the handler entirely there, including its rejection logging, and continues to the next handler. The interface defaults to `true`; opting out never skips a protected endpoint or a request without endpoint metadata.

In an `IAuthenticationHandler` implementation:

```csharp
public bool AppliesToAnonymousEndpoints => false;
```

Keep the default of `true` when a handler should still establish a principal on an anonymous endpoint. If it only needs to behave differently there, check `context.AllowsAnonymous()` inside `HandleAuthentication` instead. The middleware sets the matched endpoint's metadata before calling authentication; `AllowsAnonymous()` (from `HttpRequestContextEndpointExtensions`) is `true` only when that metadata declares `AllowAnonymous = true`.

This opt-out belongs to the Arc.Core listener's `IAuthentication` pipeline. ASP.NET Core hosting uses ASP.NET Core authentication schemes and endpoint authorization; configure anonymous access through that host's authentication and authorization setup instead.

Arc command/query authorization is a separate pipeline check. Read the current principal through `ICurrentPrincipalAccessor` from `Cratis.Arc.Authorization`, not the identity details sent to the frontend. See [Authorization](authorization.md) for roles, result status, and direct-call boundaries.

## Testing authentication handlers

Before exposing the service, test every accepted authentication mechanism with missing, malformed, invalid, and valid credentials. For tokens, include expired tokens and tokens from the wrong issuer or for the wrong audience. For forwarded identity, include forged headers and attempts to reach the backend without passing through ingress.

Exercise endpoints with and without anonymous metadata, and verify that protected endpoints cannot proceed when authentication handlers are missing. A test that accepts only a valid token does not show that invalid requests are denied.

## Next steps

- [Authorization](authorization.md) — apply authentication and role requirements.
- [Identity](../identity/index.md) — supply frontend identity details without treating them as credentials.
- [Endpoint mapping](endpoint-mapping.md) — understand manual endpoint responsibilities.
