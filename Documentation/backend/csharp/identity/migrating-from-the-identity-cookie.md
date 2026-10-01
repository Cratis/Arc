---
title: Migrating from the identity cookie
description: What changed when Arc stopped trusting and writing the .cratis-identity cookie, what keeps working during the transition, and what applications must change.
---

Earlier versions of Arc wrote the identity to a `.cratis-identity` cookie: unsigned base64 JSON with `HttpOnly = false`. On the server, `IIdentityProvider.Get()`, `Get<TDetails>()` and `/.cratis/me` returned whatever that cookie decoded to before looking at the authenticated principal. In the browser, `IdentityProvider.getCurrent()` read it before asking the server. Anyone who could set the cookie, such as a browser user or a script on a sibling subdomain, therefore decided which identity the application reported. Any script on the page could also read the identity, roles and details from it.

Arc now derives the identity only from the authenticated request, and the frontend gets it from `/.cratis/me`.

## What changed

| | Before | Now |
| --- | --- | --- |
| `Get()`, `Get<TDetails>()`, `/.cratis/me` | Returned the cookie when one was sent, otherwise the principal | Always the authenticated principal; a sent cookie is ignored. Without a principal, `/.cratis/me` answers 401. |
| `/.cratis/me` response | JSON body plus a readable `.cratis-identity` cookie | JSON body only. A `.cratis-identity` cookie on the request is expired. |
| `IProvideIdentityDetails` | Skipped while the browser held the cookie | Runs on every `/.cratis/me` call |
| `ModifyDetails` | Wrote the modified identity to the cookie | Changes the current response only |
| `IdentityProvider.getCurrent()` in `@cratis/arc` | Read the cookie, and called `/.cratis/me` only without one | Calls `/.cratis/me` once and keeps the answer in memory. Reads the cookie only when `/.cratis/me` answers 404, and warns when it does. |
| `IdentityProvider.clearIdentityCookie()` | Expired the cookie | Deprecated; same as the new `clearCache()` |

## What keeps working during the transition

The NuGet and npm packages do not have to be upgraded together:

- **New backend, earlier frontend.** The earlier frontend reads the cookie first and calls `/.cratis/me` when there is none, which is now always the case for new sessions because the new backend does not write it. A cookie left from before the upgrade lasts until the browser session ends or the frontend next calls `/.cratis/me`, for example through `refresh()`. That response expires it.
- **Earlier backend, new frontend.** The new frontend asks `/.cratis/me`, which the earlier backend serves as before. An explicit `refresh()` expires the readable legacy cookie before requesting the identity, so the earlier backend recomputes it from the current credentials rather than returning stale identity, roles or details.
- **Identity from a proxy in front of the application.** Cratis AuthProxy writes its own readable `.cratis-identity` cookie. When the browser's `/.cratis/me` request reaches the application, the identity comes from the forwarded principal. When nothing answers `/.cratis/me` (404), the new frontend falls back to the proxy's cookie and logs a warning in the browser console. After an explicit refresh answers 404, it restores the fallback cookie at the root path so the identity survives a page reload even if the proxy does not reissue it. `clearCache()` expires that cookie on logout. That fallback exists for the transition and will be removed in a future major version.
- A 401 or 403 from `/.cratis/me` is never overruled by a cookie. The identity is reported as not set.

## What applications must change

1. **Expose `/.cratis/me` to the frontend.** If the browser console shows the warning about reading the identity from the `.cratis-identity` cookie, the frontend cannot reach `/.cratis/me`. Arc maps this endpoint by default, with `DefaultIdentityDetailsProvider` when you have no custom provider. Check proxy routing and the frontend's `apiBasePath` and origin so the request reaches your Arc application, and make sure the proxy forwards the authenticated principal. Register a custom `IProvideIdentityDetails` only when you need application-specific details.
2. **Stop reading the cookie yourself.** Code that reads `.cratis-identity` from `document.cookie`, or from the request on the server, must use `IdentityProvider.getCurrent()` or `useIdentity()` in the browser, and `IIdentityProvider.Get()` or `ICurrentPrincipalAccessor` on the server.
3. **Replace `IdentityProvider.clearIdentityCookie()` with `IdentityProvider.clearCache()`.**
4. **Replace `IdentityProvider.IdentityCookieName`** (.NET) and `IdentityProvider.CookieName` (TypeScript). Both are obsolete. A build that treats warnings as errors fails on the .NET constant until the reference is removed.
5. **Make the details provider cheap enough to run on every `/.cratis/me` call.** Cache expensive lookups inside it if needed.
6. **Proxies that write `.cratis-identity` should stop.** Nothing in Arc trusts it, and once the frontend reaches `/.cratis/me`, nothing reads it.

## Changes that cannot be made compatible

- **Reading the cookie directly.** Code that read `.cratis-identity` itself, rather than through Arc, gets nothing once the backend is upgraded, because the backend no longer writes the cookie. Writing it again would bring back the exposure this change removes. Move that code to `/.cratis/me`.
- **`ModifyDetails` across requests.** The modified details used to be stored in the client-controlled cookie and returned on the next request. Arc no longer trusts that cookie, so the change cannot be carried to the next request. Store such preferences through an authenticated operation and return them from your details provider.
- **Identity before the first request.** The cookie let the first render show the identity without a round trip. The identity now arrives with the `/.cratis/me` response. Render a loading state until it does. In React, `useIdentity()` reports `isLoading`.

## Related

- [IdentityProvider service](identity-provider-service.md)
- [Provider flow](provider-flow.md)
- [Frontend identity](../../../frontend/core/identity.md)
