---
title: Acting on behalf of a user
description: Verify on-behalf sessions signed by Cratis AuthProxy, authorize as the represented user, and record the actor and the user on every event.
---

A support person, or an agent, sometimes has to sign in as a user to see what that user sees. The user grants the access; Cratis AuthProxy starts the session and forwards it to Arc. Inside such a request two identities matter:

- The **represented user** decides what the request is allowed to do. Roles, tenant and authorization are that user's.
- The **actor** is who actually acts. The audit trail has to name them.

Once you turn on-behalf forwarding on, Arc verifies AuthProxy's signed assertion and keeps both identities. The represented user is the request principal, and the actor is attached to it. With [Chronicle](../chronicle/index.md), every event appended in the session records the actor as its `causedBy` identity, on behalf of the represented user.

## The forwarded headers

AuthProxy forwards an on-behalf session with the usual [Microsoft identity headers](microsoft-identity.md) plus two headers of its own. This is the contract an ingress has to implement:

| Header                         | Carries                         | Encoding                                                                                                                                                         |
| ------------------------------ | ------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `x-ms-client-principal`        | The represented user            | Base64 (standard alphabet, padded) of the UTF-8 JSON [client principal](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data) |
| `x-ms-client-principal-id`     | The represented user            | The user's identifier: plain text when it is printable US-ASCII, otherwise the RFC 8187 form `UTF-8''` followed by percent-encoded UTF-8. Once decoded, must equal the `userId` of `x-ms-client-principal` |
| `x-ms-client-principal-name`   | The represented user            | The user's name, as plain text                                                                                                                                    |
| `x-cratis-on-behalf-actor`     | The actor                       | Base64 (standard alphabet, padded) of the UTF-8 JSON client principal of the actor, in the same shape as `x-ms-client-principal`                                  |
| `x-cratis-on-behalf-assertion` | The signed binding of the above | A compact RS256 JWS, described in [What Arc verifies](#what-arc-verifies)                                                                                         |

The actor's client principal uses the same fields as the represented user's:

```json
{
  "identityProvider": "aad",
  "userId": "support-person-id",
  "userDetails": "support@example.com",
  "userRoles": ["support"],
  "claims": [{ "typ": "name", "val": "Support Person" }]
}
```

- `userId` is required and becomes the actor's `sub` and `NameIdentifier` claims. Arc ignores `sub` and `NameIdentifier` claims inside `claims`, for the actor exactly as for the represented user.
- `userDetails` becomes the actor's `ClaimTypes.Name`.
- `userRoles` become role claims on the actor. They never authorize the request.
- `identityProvider`, when not blank, becomes the actor's `urn:cratis:arc:identity:provider` claim.

The header names are available as `OnBehalfOfHeaders.ActorHeader` and `OnBehalfOfHeaders.AssertionHeader` in `Cratis.Arc.Identity`.

## Turning on-behalf forwarding on

On-behalf forwarding is off by default. Until you turn it on, a request carrying either on-behalf header fails authentication. Turning it on requires the AuthProxy issuer, audience and public signing keys, because Arc verifies every forward against them. The represented user still arrives in the forwarded identity headers, so [`TrustForwardedIdentityHeaders`](microsoft-identity.md#trusting-the-forwarded-headers) must be on as well:

```json
{
  "Cratis": {
    "Arc": {
      "TrustForwardedIdentityHeaders": true,
      "OnBehalfOf": {
        "Enabled": true,
        "Issuer": "https://authproxy.example.com",
        "Audience": "https://app.example.com",
        "Service": "portal",
        "ClockSkew": "00:00:30",
        "SigningKeys": [
          {
            "KeyId": "authproxy-2026",
            "PublicKeyPem": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
          }
        ]
      }
    }
  }
}
```

The same settings in code, on `ArcOptions.OnBehalfOf`:

```csharp
using Cratis.Arc.Identity;

builder.AddCratisArc(options =>
{
    options.TrustForwardedIdentityHeaders = true;
    options.OnBehalfOf = new OnBehalfOfOptions
    {
        Enabled = true,
        Issuer = "https://authproxy.example.com",
        Audience = "https://app.example.com",
        Service = "portal",
        SigningKeys = [new OnBehalfOfSigningKey { KeyId = "authproxy-2026", PublicKeyPem = authProxyPublicKeyPem }]
    };
});
```

`Issuer` and `Audience` are AuthProxy's `OnBehalf:Attestation:Issuer` and `OnBehalf:Attestation:Audience`, and each signing key pairs AuthProxy's key identifier with its public key. `Service` is the name AuthProxy knows this application by, as written in AuthProxy's `Services` configuration. It is optional: leave it empty and Arc does not check which service an assertion was issued for. Set it when AuthProxy fronts more than one service with the same signing keys, issuer and audience, so an assertion issued for a request to another service is refused here. `ClockSkew` defaults to 30 seconds.

Every signing key must be an RSA public key of at least 2048 bits in PEM form. Arc refuses a private key, so the signing secret never ends up in the application's configuration. Configure several keys to rotate: add the new public key before AuthProxy signs with it, and remove the old one once AuthProxy no longer does. When the configuration cannot be used (no issuer, audience or keys, a blank or duplicate key identifier, a private, short or unreadable key), every on-behalf forward fails, and an ASP.NET Core host that registers the Microsoft Identity Platform scheme logs an error naming the problem at startup.

## What Arc verifies

Arc accepts an on-behalf forward only when the `x-cratis-on-behalf-assertion` token passes every one of these checks:

1. It is a compact JWS whose header names `alg` `RS256` and a `kid` that is one of the configured `SigningKeys`. A token with a `crit` header is refused.
2. The RS256 signature verifies with that key.
3. `iss` equals `Issuer` and `aud` equals, or contains, `Audience`.
4. `nbf` and `exp` are present, the current time is within them allowing for `ClockSkew`, and the token lives at most five minutes (`exp` minus `nbf`, or minus `iat` when that is earlier).
5. `purpose` is `on-behalf-forward`.
6. When `Service` is configured, `svc` equals it.
7. `principal_hash` is the unpadded base64url SHA-256 of the exact `x-ms-client-principal` value, `sub` is that principal's `userId`, and `x-ms-client-principal-id`, decoded from its `UTF-8''` form when it carries one, is the same `userId`.
8. `actor_hash` is the unpadded base64url SHA-256 of the exact `x-cratis-on-behalf-actor` value, and `act.sub` is the actor's `userId`.
9. When the request carries the tenant header Arc is configured to read (`Cratis:Arc:Tenancy:HttpHeader`, `x-cratis-tenant-id` by default), `tenant` equals it.
10. When the token carries `htm`, it equals the request method. When it carries `htu`, it is an absolute path equal to the path the application received: its path base and path, without scheme, host or query. Both sides are compared after percent-decoding, except that `%2F` stays encoded, so `/a%2Fb` and `/a/b` remain different paths. A `htu` holding a full URL is refused. The host is not part of the binding because the application sees AuthProxy's internal address for itself, not the public one.
11. `jti` is present and has not been accepted before within the token's lifetime.

The actor must also be a valid client principal with a `userId` that differs from the represented user's.

### What this guarantees

A forward that passes was issued by the holder of a configured private key, for this application, within the last five minutes, for exactly these identity headers, in the tenant the request names (and, with `Service` configured, for this service), and is used once per application instance. A caller who can reach the application directly cannot add an actor to their own request, swap the actor or the represented user on a captured forward, or move a forward to another tenant.

It does not guarantee more than that:

- The replay check is in memory and per process. With several instances, a captured assertion can be replayed once to each other instance within its lifetime. The five-minute ceiling, and AuthProxy's 60-second default lifetime, bound that window.
- Without `htm` and `htu` in the token, a captured forward can be replayed to another endpoint of the same application, once per instance, within its lifetime.
- The represented user's identity headers themselves are trusted on the same terms as for every other forwarded request: through `TrustForwardedIdentityHeaders`. Signing every forwarded identity, not only on-behalf forwards, is tracked in [Cratis/Arc#2837](https://github.com/Cratis/Arc/issues/2837).

AuthProxy must still strip every inbound `x-cratis-on-behalf-*` and `x-ms-client-principal*` header and set its own only for a session it started.

## When Arc rejects the request

The actor and assertion headers are never ignored the way untrusted identity headers are: a request carrying either of them is authenticated as an on-behalf forward or not at all. Failing authentication means the request has no principal. It never runs as the represented user or as the actor, but it is not rejected outright:

- An endpoint that requires an authenticated user answers `401`. In ASP.NET Core that is an endpoint with `[Authorize]`, a policy or a fallback policy. In a `Cratis.Arc.Core` host with an authentication handler, it is every endpoint that does not allow anonymous access.
- An endpoint that allows anonymous access runs, without a principal.

| Situation                                                                                     | Result                   |
| --------------------------------------------------------------------------------------------- | ------------------------ |
| The host does not trust forwarded identity headers                                            | Authentication fails     |
| `OnBehalfOf:Enabled` is off                                                                    | Authentication fails     |
| The on-behalf configuration cannot be used                                                     | Authentication fails     |
| The actor or assertion header is present but the represented user's headers are not           | Authentication fails     |
| The actor or assertion header is repeated, or the actor header is empty                        | Authentication fails     |
| The assertion is missing or fails any [verification](#what-arc-verifies) check                 | Authentication fails     |
| The actor is not base64, not a client principal, or has no `userId`                            | Authentication fails     |
| The actor's `userId` is the represented user's                                                 | Authentication fails     |
| A verified forward with a valid actor                                                          | Authenticated, on behalf |

The first rejected forward in a process is logged as a warning with its reason; later ones are logged at debug level, so a caller sending forged headers cannot flood the logs.

Both the ASP.NET Core scheme (`AddMicrosoftIdentityPlatformIdentityAuthentication()`) and the `Cratis.Arc.Core` `MicrosoftIdentityPlatformAuthenticationHandler` apply the same rules.

## Reading the actor

Authorization needs nothing new: `[Authorize]`, `[Roles]` and policies see the represented user, because the principal is that user. The actor is the [`ClaimsIdentity.Actor`](https://learn.microsoft.com/dotnet/api/system.security.claims.claimsidentity.actor) of the principal's identity, and two extension methods in `Cratis.Arc.Identity` read it:

| Method                           | Returns                                                         |
| -------------------------------- | --------------------------------------------------------------- |
| `principal.GetActor()`           | The acting `ClaimsIdentity`, or `null` outside an on-behalf session |
| `principal.IsActedOnBehalfOf()`  | `true` when an actor is acting on behalf of the principal       |

A command can read both through `ICurrentPrincipalAccessor`, for example to record who acted in its own audit log:

```csharp
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Identity;

[Command]
public record CloseTicket(TicketId Ticket)
{
    public void Handle(ICurrentPrincipalAccessor principals, ISupportAudit audit)
    {
        var principal = principals.Current!;
        if (principal.GetActor() is { } actor)
        {
            audit.Record(Ticket, actedBy: actor.Name!, onBehalfOf: principal.Identity!.Name!);
        }
    }
}
```

`TicketId` and `ISupportAudit` stand for your own types.

## What Chronicle records

With Arc's Chronicle integration, the identity Chronicle records is a chain with the actor first and the represented user second:

```text
causedBy:      support-person-id   (the actor)
  onBehalfOf:  user-id             (the represented user)
```

Chronicle classifies a head that acts on behalf of another identity as an **agent**, so event metadata reports the initiator as `Agent` on behalf of the represented user. The identity names come from the `sub`, `name` and `preferred_username` claims of each identity, falling back to `ClaimsIdentity.Name`, as described in [Causation](../chronicle/commands/causation.md#who-caused-the-event).

This follows `ClaimsIdentity.Actor`, not the AuthProxy headers: any authentication scheme that sets an actor, such as a bearer token whose `act` claim the handler maps to `ClaimsIdentity.Actor`, is recorded the same way.

An identity your code sets explicitly with Chronicle's `IIdentityProvider.SetCurrentIdentity` still wins over the request principal.
