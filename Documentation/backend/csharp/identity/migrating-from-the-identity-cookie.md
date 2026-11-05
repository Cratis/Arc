---
title: Cratis Arc identity cookie migration
description: Migrate Cratis Arc identity reads from the .cratis-identity cookie to /.cratis/me, update cache clearing, and handle mixed frontend and backend versions.
---

To migrate Cratis Arc identity handling, replace direct `.cratis-identity` cookie reads with Arc's identity APIs and make `/.cratis/me` reachable from the frontend. The backend now derives identity from the authenticated request, while the frontend fetches the presentation data from that endpoint and keeps it in memory.

Earlier versions wrote unsigned base64 JSON to `.cratis-identity` with `HttpOnly = false`, making it readable by scripts. The server's `IIdentityProvider.Get()`, `Get<TDetails>()` and `/.cratis/me` returned the decoded cookie before checking the authenticated principal, and the browser's `IdentityProvider.getCurrent()` read it before asking the server. A caller who could set that cookie could therefore choose the identity the application reported. The migration removes that cookie as a backend identity source; it does not make frontend presentation data a substitute for authorization.

## What changed

| | Before | Now |
| --- | --- | --- |
| `Get()`, `Get<TDetails>()`, `/.cratis/me` | Returned the cookie when one was sent, otherwise the principal | Always the authenticated principal; a sent cookie is ignored. Without a principal, `/.cratis/me` answers 401. |
| `/.cratis/me` response | JSON body plus a readable `.cratis-identity` cookie | JSON body only. A `.cratis-identity` cookie on the request is expired. |
| `IProvideIdentityDetails` | Skipped while the browser held the cookie | Runs on every authenticated `/.cratis/me` call |
| `ModifyDetails` | Wrote the modified identity to the cookie | Changes the current response only |
| `IdentityProvider.getCurrent()` in `@cratis/arc` | Read the cookie, and called `/.cratis/me` only without one | Calls `/.cratis/me` and keeps a resolved identity in memory until refresh or cache clearing. An unresolved identity is not cached. Reads the cookie only when `/.cratis/me` answers 404, and warns when it does. |
| `IdentityProvider.clearIdentityCookie()` | Expired the cookie | Deprecated; same as the new `clearCache()` |

## What keeps working during the transition

The NuGet and npm packages do not have to be upgraded together. Which part you upgrade first affects how an existing cookie is handled.

With a new backend and an earlier frontend, the frontend still reads the cookie first. New sessions have no Arc-written cookie, so it asks `/.cratis/me`. An existing cookie can remain until the browser session ends or the frontend next calls `/.cratis/me`, for example through `refresh()`. That response expires it. If an earlier frontend still displays stale identity after the upgrade, make it request the endpoint instead of waiting for the session to end.

With an earlier backend and a new frontend, the frontend asks `/.cratis/me`. An explicit `refresh()` expires the readable legacy cookie before making that request, so the earlier backend recomputes identity from the current credentials instead of returning the stale cookie.

Cratis AuthProxy writes its own readable `.cratis-identity` cookie by default. If you use it, route the browser's `/.cratis/me` request to the application and configure trusted authentication for the forwarded principal. When that request returns 404, the new frontend falls back to the cookie and logs a browser-console warning. After an explicit refresh returns 404, it restores the fallback cookie at the root path so it survives a page reload even if the proxy does not reissue it. `clearCache()` expires that cookie on logout. This compatibility fallback will be removed in a future major version.

A 401 or 403 from `/.cratis/me` is never overruled by a cookie. The frontend reports the identity as not set.

## What applications must change

1. Make `/.cratis/me` reachable from the frontend. A console warning about reading `.cratis-identity` means the request returned 404. Arc maps the endpoint by default and uses `DefaultIdentityDetailsProvider` when you have no custom provider. Check proxy routing, `apiBasePath`, and the frontend origin so the request reaches your Arc application with its authenticated principal. Add a custom `IProvideIdentityDetails` only when you need application-specific details or an entry decision.
2. Replace direct cookie reads. In the browser, use `IdentityProvider.getCurrent()` or `useIdentity()`. On the server, use `IIdentityProvider.Get()` for presentation data, or `ICurrentPrincipalAccessor` when you need the trusted principal for authorization.
3. Replace `IdentityProvider.clearIdentityCookie()` with `IdentityProvider.clearCache()` when clearing frontend identity, including at logout.
4. Remove references to `IdentityProvider.IdentityCookieName` (.NET) and `IdentityProvider.CookieName` (TypeScript). Both are obsolete. A build that treats warnings as errors fails on the .NET constant until the reference is removed; code should no longer need the cookie name to read identity.
5. Keep the details provider suitable for every authenticated `/.cratis/me` call. Cache expensive lookups inside it if needed, accounting for how long cached membership decisions may remain valid.
6. Stop proxies from writing `.cratis-identity` once every frontend sharing the session reads identity from `/.cratis/me`. The Arc backend ignores the cookie, but an earlier frontend or a new frontend receiving 404 may still read it during the transition.

Check the browser's network response after migration. An authenticated, authorized identity request should return JSON from `/.cratis/me` without a cookie-fallback warning. A 401 means the request has no authenticated principal; a 403 means the details provider rejected application entry. Neither should fall back to a cookie.

## Changes that cannot be made compatible

Direct cookie readers stop receiving updates from the upgraded backend because it no longer writes `.cratis-identity`. Move them to `/.cratis/me` or the Arc identity APIs. Writing the cookie again would restore the script-readable data exposure this change removes.

`ModifyDetails` changes only the current response. Previously, the client-controlled cookie carried modified details into later requests. Store durable preferences through an authenticated operation and return them from your details provider instead.

The first render must wait for an identity request rather than reading a cookie synchronously. Render a loading state until the response arrives. In React, `useIdentity()` reports `isLoading`; the [frontend identity guide](../../../frontend/core/identity.md) explains how to consume the result.

## Related

- [IdentityProvider service](identity-provider-service.md)
- [Provider flow](provider-flow.md)
- [Frontend identity](../../../frontend/core/identity.md)
