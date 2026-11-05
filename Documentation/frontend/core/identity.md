---
title: Cratis Arc frontend identity lifecycle
description: Cratis Arc reads frontend identity from /.cratis/me and caches it in memory. Learn when to refresh or clear it and keep authorization on the server.
---

Cratis Arc's `IdentityProvider` reads the current user's name, roles, and application-specific details from `/.cratis/me` for display in the frontend. It keeps a resolved identity in memory, so use it to personalize a screen and refresh it after sign-in or a change to the user's details.

The backend derives identity from the authenticated request. Configure authentication on the host; looking up identity does not sign the user in. The frontend reads a legacy cookie only if the endpoint answers 404; see [upgrading from the identity cookie](#upgrading-from-the-identity-cookie).

To exercise users and roles before connecting your identity provider, follow [generating principals for local development](../../backend/csharp/development/generating-principal.md). That procedure uses unsigned identity headers on an isolated development host, not production login tokens.

## Identity provider

Use `IdentityProvider.getCurrent()` when you need the current identity outside React. In a React component, use [the identity hook](../react/identity.md) to track loading and changes through context.

Identity is read-only presentation data. Someone with browser developer tools can change the in-memory value, so every protected backend operation must authorize the caller independently.

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

const identity = await IdentityProvider.getCurrent();

console.log(`Hello '${identity.name}'`);
```

The first call starts the request; concurrent calls share that pending request. Await the promise before displaying the user's name. Once the request resolves to an identity, later calls use the cached response without another request.

A non-success response other than the 404 cookie fallback, including 401 or 403, produces an identity with `isSet` set to `false`. That result is not cached, so the next call asks again. A network failure or invalid JSON rejects the promise and also leaves the next call free to retry. Handle those failures in your UI instead of treating every failed lookup as an anonymous user.

For header-based authentication, configure `IdentityProvider.setHttpHeadersCallback()` to supply the request headers. `setOrigin()` and `setApiBasePath()` select the backend location; changing either clears the cached response. These settings and the cache are shared across `IdentityProvider` instances, not separate sessions per instance.

### Signing out

Complete sign-out through your authentication system, then call `IdentityProvider.clearCache()` to forget the cached response and expire a legacy `.cratis-identity` cookie. The next `getCurrent()` asks the server again with the credentials then available.

Clearing the cache does not revoke sessions, remove authentication tokens or cookies, or update identity objects your code already holds. Stop displaying those objects when the user signs out.

`IdentityProvider.clearIdentityCookie()` is deprecated and now does the same as `clearCache()`.

## Details

Use identity details for application-specific display data such as a department. The backend supplies that data through [identity enrichment](../../backend/csharp/identity/index.md); declaring a frontend type does not create the data or guarantee it is present.

A generic type parameter describes the JSON shape you expect. Guard absent details or choose a display fallback:

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

type IdentityDetails = {
    department: string,
    age: number
};

const identity = await IdentityProvider.getCurrent<IdentityDetails>();

const department = identity.details?.department ?? 'Unknown department';
console.log(`Hello '${identity.name}' from '${department}'`);
```

> [!IMPORTANT]
> The `<IdentityDetails>` type parameter is erased at runtime, so `identity.details` above remains the server's raw JSON. Pass a class constructor to `getCurrent()` when you need deserialization to construct complex values, such as a `Guid` with a `toString()` method. The following example assumes the backend supplies details containing `userId`:
>
> ```typescript
> import { IdentityProvider } from '@cratis/arc/identity';
> import { Guid, field } from '@cratis/fundamentals';
>
> class IdentityDetails {
>     @field(Guid)
>     userId!: Guid;
> }
>
> // The constructor argument is what deserializes - not just the <IdentityDetails> type parameter.
> const identity = await IdentityProvider.getCurrent(IdentityDetails);
>
> console.log(identity.details.userId.toString());
> ```
>
> Add an `@field` decorator to every property you want populated. If the class has no decorated properties, Arc warns and leaves the raw payload unchanged.

## IIdentity

`getCurrent()` returns an `IIdentity<TDetails>` with these members:

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | string | The unique identifier from the identity provider |
| name | string | The user name |
| roles | string[] | Array of roles the identity is in |
| details | `TDetails` | Application-specific details, default generic type `object`; data may be absent at runtime |
| isSet | boolean | Whether the lookup resolved an identity |
| isInRole | (role: string) => boolean | Method to check if the identity is in a specific role |
| refresh | `() => Promise<IIdentity<TDetails>>` | Requests identity again and returns a new object |

## Role checking

Use `isInRole()` to choose which UI to show for a role. It checks the roles in the current identity response; the backend must still authorize any operation the UI offers.

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

const identity = await IdentityProvider.getCurrent();

if (identity.isInRole('Admin')) {
    console.log('User is an admin');
}
```

You can also access the roles array directly:

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

const identity = await IdentityProvider.getCurrent();

console.log(`User roles: ${identity.roles.join(', ')}`);
```

## Refresh

Call `refresh()` after sign-in or when the backend changes the user's roles or details. It requests `/.cratis/me` again and replaces the cached response, so later `getCurrent()` calls use the new result. It does not renew or revoke authentication credentials.

The identity object exposes `refresh()` directly:

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

let identity = await IdentityProvider.getCurrent();
identity = await identity.refresh();
```

Refresh returns a new identity object rather than changing the old one. Assign the result as shown above, and update any UI state that holds the previous object. A rejected refresh discards the cached response; handle the error and stop presenting the old object as current.

## Upgrading from the identity cookie

Earlier versions of Arc wrote the identity to a JavaScript-readable `.cratis-identity` cookie, and `IdentityProvider` read it before asking `/.cratis/me`. Now `IdentityProvider` asks `/.cratis/me` first. It reads the cookie only when that endpoint answers 404, for example behind a proxy that does not route it to the application yet. When it does, it logs a warning in the browser console. That fallback will be removed in a future major version.

- Code that read `.cratis-identity` from `document.cookie` must call `IdentityProvider.getCurrent()`, or `useIdentity()` in React, instead.
- Replace `IdentityProvider.clearIdentityCookie()` with `IdentityProvider.clearCache()`.
- Identity is no longer available before the first request completes: render a loading state until `getCurrent()` resolves. In React, `useIdentity()` reports `isLoading` for this.

See [migrating from the identity cookie](../../backend/csharp/identity/migrating-from-the-identity-cookie.md) for the full picture, including mixed frontend and backend versions.
