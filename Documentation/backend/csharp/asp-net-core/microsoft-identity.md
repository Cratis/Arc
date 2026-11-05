---
title: Microsoft identity headers in Cratis Arc
description: Authenticate forwarded Microsoft client principals in an ASP.NET Core Arc host, with explicit ingress trust and separate endpoint authorization.
---

Cratis Arc can reconstruct an authenticated principal from the Microsoft client-principal headers supplied by a trusted ingress. Use this integration when Azure App Service or Container Apps authentication (EasyAuth), or Cratis AuthProxy, authenticates callers before forwarding requests to your ASP.NET Core application.

The ingress is the service in front of your application that receives those requests. The setup below trusts its unsigned identity headers, so it must strip caller-supplied identity headers, write its own replacements, and prevent direct backend access. Arc's handler reads these headers:

| Header                     | Description                                                                                                                                                                                                                 |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| x-ms-client-principal      | The unsigned principal payload holding the details, base64 encoded [Microsoft Client Principal Data definition](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data) |
| x-ms-client-principal-id   | The unique identifier from the identity provider for the identity. A value in the RFC 8187 form `UTF-8''` followed by percent-encoded UTF-8, which AuthProxy uses for an identifier that is not printable US-ASCII, is decoded |
| x-ms-client-principal-name | The name of the identity, typically resolved from claims within the token                                                                                                                                                   |

For local development without an identity provider, [generate a principal fixture](../development/generating-principal.md) and inject it as request headers, for example with a browser extension. The `x-ms-client-principal` value must be the base64-encoded [Microsoft Client Principal Data definition](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data).

> [!WARNING]
> Use these headers only behind ingress that authenticates callers, strips caller-supplied identity headers, writes trusted replacements, and prevents direct backend access. Base64 is not a signature. Generated principals are local test fixtures, not credentials to accept on an internet-facing service. The identity Arc returns from enrichment is likewise not an authentication ticket.

## Authentication / Authorization

Register the header authentication handler with `AddMicrosoftIdentityPlatformIdentityAuthentication()`. It creates the request principal used by ASP.NET Core and Arc authorization; it does not decide which endpoints require that principal.

This registration fragment is for an ASP.NET Core application using `Cratis.Arc`, not `ArcApplication`. Enable header trust only after establishing the ingress boundary described above:

```csharp
using Cratis.Arc;
using Cratis.Arc.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.AddCratisArc(options => options.TrustForwardedIdentityHeaders = true);
builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();
builder.Services.AddAuthorization();
```

The extension calls `AddAuthentication()` with the default scheme name `MicrosoftIdentityPlatform` and registers its handler. Pass a string argument to the extension if you need a different scheme name.

### Trusting the forwarded headers

Without signed forwarding enabled, the handler ignores these unsigned headers until the host opts in with `TrustForwardedIdentityHeaders`. It returns no authentication result, so the request stays anonymous unless another scheme authenticates it. Set the option in code, as above, or in configuration:

```json
{
  "Cratis": {
    "Arc": {
      "TrustForwardedIdentityHeaders": true
    }
  }
}
```

This setting applies to every endpoint, including protected [introspection catalogs](../introspection/index.md). A host that calls `AddMicrosoftIdentityPlatformIdentityAuthentication()` (including through `AddCratis()`) while the headers are not trusted logs a warning naming the setting on every startup. The built-in header handlers also warn on the first request carrying untrusted headers.

If your ingress must sign forwarded identities, configure [signed forwarded identity](signed-forwarded-identity.md) as well. That mode also requires `TrustForwardedIdentityHeaders`, but refuses unsigned forwards once enabled. It needs a compatible signer; current unmodified AuthProxy does not produce that envelope. Check the signing guide's producer requirements before enabling it.

After building the application, add authentication and authorization middleware before activating Arc:

```csharp
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseCratisArc();
app.Run();
```

This establishes the host authentication mechanism; apply [authorization requirements](authorization.md) to private operations. Identity details do not automatically protect every endpoint.

## Knowing which identity provider signed the caller in

The `identityProvider` field in `x-ms-client-principal` identifies the provider reported by the ingress, such as Entra ID or GitHub. Arc copies that value into a claim on the reconstructed principal. Request authorization and `/.cratis/me` identity resolution can read it:

```csharp
using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Http;

public class IdentityProviderReader(IHttpContextAccessor httpContextAccessor)
{
    public string? Current =>
        httpContextAccessor.HttpContext?.User.FindFirst(MicrosoftIdentityPlatformClaims.IdentityProvider)?.Value;
}
```

| Aspect      | Detail                                                                                                   |
| ----------- | -------------------------------------------------------------------------------------------------------- |
| Claim type  | `urn:cratis:arc:identity:provider` — use the `MicrosoftIdentityPlatformClaims.IdentityProvider` constant |
| Value       | The exact `identityProvider` value the ingress forwarded, for example `aad` or `github`                  |
| When absent | The forwarded principal carried no `identityProvider` field, or the field held only blank characters     |

Arc reserves this claim type. Before writing its value, Arc removes claims of that type from the forwarded payload, ignoring casing. This prevents a forwarded claim from displacing the value copied from `identityProvider`.

> [!WARNING]
> Removing duplicate claims establishes where the value came from, not whether it is authentic. In the unsigned setup above, a caller with direct backend access can forge the entire payload, including `identityProvider`. Trust the claim only when your ingress is the only source of the header. Reject or overwrite inbound identity headers there and prevent callers from bypassing it.

Read the claim with `FindFirst` or `FindAll`, as in the example. These lookups compare claim types the same way Arc does when removing forwarded copies. Do not normalize claim types yourself: applying `ToUpperInvariant()` or `Trim()` while enumerating `User.Claims` widens the match to types Arc did not remove. A forged type using Unicode case folding (`urn:cratiſ:arc:identity:provider`, U+017F) or trailing whitespace can then match. Since forwarded claims precede Arc's own claim, `FirstOrDefault()` over that widened set can return the forgery.

> [!IMPORTANT]
> Arc copies `identityProvider` verbatim, without trimming or interpreting it. A canonical AuthProxy deployment forwards its canonical provider key, also published as `urn:cratis:identity:provider-key`. Other ingresses may supply a scheme name or a display name that changes when renamed. Use Arc's claim for diagnostics and provider-aware behavior, but do not assume its value is a durable key. When you need one, use the ingress's designated claim, such as `urn:cratis:identity:provider-key` in AuthProxy. Arc copies that ingress-authored claim through untouched.

## Acting on behalf of a user

When Cratis AuthProxy forwards a session in which a support person or an agent acts on behalf of a user, it adds an `x-cratis-on-behalf-actor` header and a signed `x-cratis-on-behalf-assertion`. The principal stays the represented user, and the actor is attached to it. Arc rejects both headers unless on-behalf forwarding is turned on and the assertion verifies. See [Acting on behalf of a user](acting-on-behalf-of.md) for the configuration, the verification rules and how to read the actor.

## Identity Details

Once authentication is configured, use [identity details](../identity/index.md) to supply application-specific information to the UI. Those details are presentation data, not an authentication ticket or additional authorization claims. Protect operations through [authorization requirements](authorization.md) on the authenticated principal.
