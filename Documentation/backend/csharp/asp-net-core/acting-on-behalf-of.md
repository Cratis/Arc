---
title: Acting on behalf of a user
description: Accept on-behalf sessions forwarded by Cratis AuthProxy, authorize as the represented user, and record both identities on every event.
---

A support person, or an agent, sometimes has to sign in as a user to see what that user sees. The user grants the access; Cratis AuthProxy starts the session and forwards it to Arc. Inside such a request two identities matter:

- The **represented user** decides what the request is allowed to do. Roles, tenant and authorization are that user's.
- The **actor** is who actually acts. The audit trail has to name them.

Arc keeps both. The represented user is the request principal, and the actor is attached to it. With [Chronicle](../chronicle/index.md), every event appended in the session records the actor as its `causedBy` identity, on behalf of the represented user.

## The forwarded headers

AuthProxy forwards an on-behalf session with the usual [Microsoft identity headers](microsoft-identity.md) plus one header for the actor. This is the contract an ingress has to implement:

| Header                       | Carries              | Encoding                                                                                                                                                         |
| ---------------------------- | -------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `x-ms-client-principal`      | The represented user | Base64 (standard alphabet, padded) of the UTF-8 JSON [client principal](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information?tabs=csharp#client-principal-data) |
| `x-ms-client-principal-id`   | The represented user | The user's identifier, as plain text                                                                                                                              |
| `x-ms-client-principal-name` | The represented user | The user's name, as plain text                                                                                                                                    |
| `x-cratis-on-behalf-actor`   | The actor            | Base64 (standard alphabet, padded) of the UTF-8 JSON client principal of the actor, in the same shape as `x-ms-client-principal`                                  |

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

The header name is available as `OnBehalfOfHeaders.ActorHeader` in `Cratis.Arc.Identity`.

### Signing

The ingress must strip every inbound `x-cratis-on-behalf-actor` header, just as it strips the `x-ms-client-principal*` headers, and set its own only for an on-behalf session it started. When AuthProxy signs its forwards, the signed assertion covers the actor header together with the represented user's headers, so an actor cannot be added to, or swapped on, an otherwise valid forward.

## When Arc rejects the request

Arc only accepts an on-behalf session from a forward it trusts. Today that is a host with [`TrustForwardedIdentityHeaders`](microsoft-identity.md#trusting-the-forwarded-headers) turned on. Unlike the other forwarded headers, which an untrusting host ignores, the actor header is never ignored: a request carrying it fails authentication, so the endpoint answers `401` instead of running anonymously.

| Situation                                                                 | Result                   |
| ------------------------------------------------------------------------- | ------------------------ |
| The host does not trust forwarded identity headers                        | Authentication fails     |
| The actor header is present but the represented user's headers are not    | Authentication fails     |
| The actor header is not base64, not a client principal, or has no `userId` | Authentication fails     |
| The actor's `userId` is the represented user's `x-ms-client-principal-id` | Authentication fails     |
| A trusted forward with a valid actor                                      | Authenticated, on behalf |

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

An identity your code sets explicitly with Chronicle's `IIdentityProvider.SetCurrentIdentity` still wins over the request principal.
