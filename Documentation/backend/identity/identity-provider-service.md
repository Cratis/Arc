---
title: IdentityProvider service
description: Read identity presentation data with IIdentityProvider while respecting cookie caching, trust, and mutation limits.
---

`IIdentityProvider` serves identity presentation data during HTTP requests. It can reuse a browser cookie to avoid calling the details provider again, but that optimization has an important boundary: **cookie data is not authentication or authorization evidence**.

## Key methods

| Method | Current behavior |
| --- | --- |
| `Get()` | Reads a nonempty identity cookie first; otherwise builds a result from the current request principal and details provider. With no request context, returns anonymous. |
| `Get<TDetails>()` | Uses the same cookie-first flow and deserializes/converts details to `TDetails`. |
| `SetCookieForHttpResponse(result)` | Writes both the identity cookie and JSON response body. It does not establish a trusted principal. |
| `ModifyDetails<TDetails>(transform)` | Calls nongeneric `Get()` and applies the transform only when `Details is TDetails`; otherwise does nothing. |

## Cookie trust and caching

`.cratis-identity` contains **unsigned base64 JSON**, not an encrypted or signed authentication ticket. It has `HttpOnly = false`, `SameSite = Lax`, `Path = /`, and `Secure` only when the request is HTTPS. JavaScript and the client can read or replace it.

`Get()` accepts a cookie result before consulting the authenticated principal or provider. A malformed nonempty cookie returns an anonymous result rather than refreshing from the provider. A valid cached cookie can retain old identity flags/details after provider-side changes. Typed deserialization does not make the content trustworthy.

Never use its `IsAuthenticated`, `IsAuthorized`, roles, tenant selection, or details to authorize backend actions. Read `ICurrentPrincipalAccessor.Current` from `Cratis.Arc.Authorization` and authoritative application data instead. Treat cookie-backed selections as untrusted preferences and validate them independently. Avoid putting secrets or unnecessary personal data in this JavaScript-readable payload.

## Mutation limitation

`ModifyDetails<UserDetails>(...)` is not currently a reliable cookie-backed preferences update API. On a subsequent request, nongeneric `Get()` normally deserializes object-valued details as `JsonElement`, not `UserDetails`. The method's type test fails, the callback is not invoked, and no update is written. It may work when a fresh provider result already contains the requested CLR type, which can hide the second-request failure.

The writer also writes JSON to the response; do not assume it only sets a cookie and then independently write another response body. Persist durable preferences through an authenticated application operation and authoritative storage. Test the complete cookie-bearing request lifecycle before relying on mutation.

## Verification and related contracts

Test a forged cookie, a cached cookie after a permission change, an invalid cookie, and a second cookie-bearing mutation request.

- [Provider flow](provider-flow.md) — fresh enrichment versus cached results.
- [Authorization](../core/authorization.md) — trusted principal and pipeline enforcement.
- [Frontend identity](../../frontend/react/identity.md) — presenting identity details.
