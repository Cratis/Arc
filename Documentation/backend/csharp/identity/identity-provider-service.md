---
title: Cratis Arc IIdentityProvider usage
description: Use Cratis Arc IIdentityProvider to read identity presentation data or modify details in the current response, without changing authentication.
---

Use `IIdentityProvider` when backend code needs the identity presentation data that Cratis Arc returns to the frontend. For an authenticated request, it reads the principal, the server-side representation of the caller, and invokes `IProvideIdentityDetails` to add application-specific details. The result describes the caller for display; use the principal and authorization policies to decide what the caller may do.

## Key methods

| Method | Behavior |
| --- | --- |
| `Get()` | Builds a result from the current request principal and details provider. With no request context, or no authenticated principal, returns anonymous. |
| `Get<TDetails>()` | Uses the same flow and converts the details to `TDetails`. |
| `SetCookieForHttpResponse(result)` | Writes the result as the JSON response body, sets `Cache-Control: no-store, private`, and adds `Cookie` to `Vary`. The name is retained for compatibility: it expires a `.cratis-identity` cookie if the request carries one, but does not write a new cookie or establish a trusted principal. |
| `ModifyDetails<TDetails>(transform)` | Calls nongeneric `Get()`, applies the transform when `Details is TDetails`, and writes the modified result to the response; otherwise does nothing. |

## Trust

`Get()` ignores the `.cratis-identity` cookie. Setting that cookie cannot change the resolved identity or roles. For an authenticated caller, the details provider runs on every call, so changes to the application data it reads can affect the result even when the credentials stay the same. Cache expensive lookups inside the provider if needed, accounting for how long a cached application-entry decision may remain valid.

Do not use the result's `IsAuthenticated`, `IsAuthorized`, roles, or details in place of authorization. Read `ICurrentPrincipalAccessor.Current` from `Cratis.Arc.Authorization` and authoritative application data, and protect commands and queries with [pipeline authorization](../core/authorization.md). Because Arc sends the result to the browser, leave secrets and unnecessary personal data out of the details.

## Modifying details

`ModifyDetails<TDetails>(...)` changes what the current response reports when the resolved details match `TDetails`. For example, it can return a different display name, but it does not save that preference. To keep the name across requests, store it through an authenticated application operation and have your details provider read it back on later calls.

When the types match, `ModifyDetails` writes the JSON response itself. Do not write another response body after it has done so.

## Upgrading from the identity cookie

Earlier versions returned whatever the unsigned `.cratis-identity` cookie decoded to before looking at the authenticated principal, and wrote that cookie readable by any script. Arc no longer trusts or writes it. See [migrating from the identity cookie](migrating-from-the-identity-cookie.md) for what keeps working during the transition and what to change.

## Related contracts

- [Migrating from the identity cookie](migrating-from-the-identity-cookie.md)
- [Provider flow](provider-flow.md) — how a request becomes an identity.
- [Authorization](../core/authorization.md) — trusted principal and pipeline enforcement.
- [Frontend identity](../../../frontend/react/identity.md) — presenting identity details.
