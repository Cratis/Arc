---
title: Identity
description: How the frontend gets the current identity from the /.cratis/me endpoint, keeps it in memory, and exposes it through the core identity API.
---

The frontend gets the current identity from the `/.cratis/me` endpoint. The backend derives that identity from the authenticated request, so what the frontend sees is what the server knows about the caller, not anything the browser stored. Configure authentication on the host; identity lookup is not itself a login mechanism.

> Important note: Since local development is not configured with the identity provider, but you still need a way to test that both the backend and the frontend
> deals with the identity in the correct way. This can be achieved by creating the correct token and injecting it as request headers using
> a browser extension. Read more [about generating principals](../../backend/csharp/development/generating-principal.md).

## Identity provider

Identity is intended for read-only UI consumption through `IdentityProvider`. The identity is held in the page's memory, where anyone with the browser's developer tools can change it, so it is not proof of anything. Every protected backend operation must authorize the actual credential independently.

The `IdentityProvider` provides functionality for getting the current identity.

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

const identity = await IdentityProvider.getCurrent();

console.log(`Hello '${identity.name}'`);
```

> Note that the `getCurrent()` method is an asynchronous operation that returns a promise.
> The first call asks `/.cratis/me` for the identity. The answer is kept in memory for the page, so later calls return it without asking the server again.

When the server does not resolve an identity, for example because the caller is not signed in, `getCurrent()` returns an identity with `isSet` set to `false`, and that answer is not kept: the next call asks the server again.

### Signing out

`IdentityProvider.clearCache()` forgets the identity kept in memory, so the next `getCurrent()` asks the server again. It does not revoke sessions or remove authentication tokens or cookies. Real sign-out must complete through your authentication system before reconnecting anonymously.

`IdentityProvider.clearIdentityCookie()` is deprecated and now does the same as `clearCache()`.

## Details

Part of the identity can hold details that are beyond what the identity provider provides. These details are application specific and something that your
application or ingress should be responsible for filling out. Details can be considered optional, as that might not be a requirement for your application.

The `getCurrent()` method takes a generic parameter that describes the expected details object. It does not guarantee the backend supplies one. Guard absent details or choose an explicit display fallback:

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
> The `<IdentityDetails>` **type parameter** only tells TypeScript what shape to expect at compile time - it has no effect at runtime, because a type is erased before the code ever runs. `identity.details` above is still the raw JSON object the server sent.
>
> Deserialization into a real class - constructing `Guid`, `DateOnly`, or other complex types with their methods and behavior instead of plain JSON - only happens when you additionally pass a class **constructor** as a runtime argument to `getCurrent()`:
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
> The class also needs an `@field` decorator on every property you want populated - a class with none of them cannot be deserialized into, and the raw payload is passed through unchanged instead of being silently blanked.

## IIdentity

The return type coming from `getCurrent()` looks like the following:

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | string | The unique identifier from the identity provider |
| name | string | The user name |
| roles | string[] | Array of roles the identity is in |
| details | `TDetails` | Application-specific details, default generic type `object`; data may be absent at runtime |
| isInRole | (role: string) => boolean | Method to check if the identity is in a specific role |

## Role checking

The identity includes information about the roles assigned to the user. You can check if a user is in a specific role using the `isInRole()` method:

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

In some scenarios you might need to refresh the identity. Typically if the user has been granted more access or details has been updated.
Rather than having your user log out and back in again, you can issue a refresh. Refresh always calls `/.cratis/me` for the identity and details, and replaces what is kept in memory, so later calls to `getCurrent()` return the refreshed identity. It does not renew or revoke authentication credentials.

To refresh the identity you can call the `refresh()` method on the identity object itself.

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

let identity = await IdentityProvider.getCurrent();
identity = await identity.refresh();
```

The identity object is designed to be immutable, leading to the `refresh()` method having to return a new instance.
This means that the original `identity` instance won't be updated and you would have to replace it if you have it as a variable.

## Upgrading from the identity cookie

Earlier versions of Arc wrote the identity to a JavaScript-readable `.cratis-identity` cookie, and `IdentityProvider` read it before asking `/.cratis/me`. Arc no longer writes or reads that cookie, because anyone able to set it - a script on the page or on a sibling subdomain - decided which identity the frontend saw.

- Code that read `.cratis-identity` from `document.cookie` must call `IdentityProvider.getCurrent()`, or `useIdentity()` in React, instead.
- Replace `IdentityProvider.clearIdentityCookie()` with `IdentityProvider.clearCache()`.
- Identity is no longer available synchronously on the first render: render a loading state until `getCurrent()` resolves. In React, `useIdentity()` reports `isLoading` for this.
- If a reverse proxy in front of your application still writes a readable `.cratis-identity` cookie, Arc ignores it. Stop the proxy from writing it.

See [upgrading the backend](../../backend/csharp/identity/identity-provider-service.md#upgrading-from-the-identity-cookie) for the server side.
