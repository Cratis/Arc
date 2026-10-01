---
title: IdentityProvider service
description: Read identity presentation data with IIdentityProvider, which always derives the identity from the authenticated request.
---

`IIdentityProvider` serves identity presentation data during HTTP requests. It always derives the identity from the authenticated principal of the current request and the details your `IProvideIdentityDetails` returns for it. Nothing else the client sends, such as a cookie, is read. The result is still presentation data: **it is not authorization evidence on its own**.

## Key methods

| Method | Behavior |
| --- | --- |
| `Get()` | Builds a result from the current request principal and details provider. With no request context, or no authenticated principal, returns anonymous. |
| `Get<TDetails>()` | Uses the same flow and converts the details to `TDetails`. |
| `SetCookieForHttpResponse(result)` | Writes the result as the JSON response body, with `Cache-Control: no-store, private` and `Vary: Cookie`. Despite its name, it no longer writes a cookie. It does not establish a trusted principal. |
| `ModifyDetails<TDetails>(transform)` | Calls nongeneric `Get()`, applies the transform when `Details is TDetails`, and writes the modified result to the response; otherwise does nothing. |

## Trust

The identity comes from the authenticated principal, so a client cannot change what `Get()` returns without changing its credentials. The details provider runs on every call; cache expensive lookups inside it if you need to.

Never use the result's `IsAuthenticated`, `IsAuthorized`, roles, or details in place of authorization. Read `ICurrentPrincipalAccessor.Current` from `Cratis.Arc.Authorization` and authoritative application data, and protect commands and queries with [pipeline authorization](../core/authorization.md). The result is sent to the browser, so avoid putting secrets or unnecessary personal data in the details.

## Modifying details

`ModifyDetails<TDetails>(...)` changes what the current response reports. It does not store anything: the next request derives the identity from the principal again. Persist durable preferences through an authenticated application operation and authoritative storage, and return them from your details provider.

`ModifyDetails` writes the JSON response itself; do not write another response body after calling it.

## Upgrading from the identity cookie

Earlier versions of Arc wrote the identity to an unsigned, JavaScript-readable `.cratis-identity` cookie and returned whatever that cookie decoded to before looking at the authenticated principal. A client, or a script on a sibling subdomain that could set the cookie, therefore controlled what `/.cratis/me` and `Get()` reported. Arc now neither reads nor writes the cookie.

- `Get()`, `Get<TDetails>()` and `/.cratis/me` report the authenticated principal. A request without one gets an anonymous result, and `/.cratis/me` answers 401, whatever cookie it carries.
- The details provider runs on every call instead of once per browser session.
- `ModifyDetails` no longer persists changes across requests.
- `IdentityProvider.IdentityCookieName` is obsolete.
- Frontends get the identity from `/.cratis/me`; see [frontend identity](../../../frontend/core/identity.md#upgrading-from-the-identity-cookie).
- A reverse proxy that writes its own readable `.cratis-identity` cookie should stop: nothing in Arc reads it.

## Related contracts

- [Provider flow](provider-flow.md) — how a request becomes an identity.
- [Authorization](../core/authorization.md) — trusted principal and pipeline enforcement.
- [Frontend identity](../../../frontend/react/identity.md) — presenting identity details.
