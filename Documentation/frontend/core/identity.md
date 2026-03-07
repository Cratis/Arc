# Identity

The frontend identity is based on information it gets from a cookie called `.cratis-identity`. The purpose of this is to be able to
provide identity information to the client at the first render. This allows for a better developer and user experience, as there is no need
to call the backend for details about the user.

While in development mode on your local machine, if this cookie does not exist it will call the `.cratis/me` endpoint from the frontend
itself. This makes it possible to work without having to simulate the entire production environment locally.

> Important note: Since local development is not configured with the identity provider, but you still need a way to test that both the backend and the frontend
> deals with the identity in the correct way. This can be achieved by creating the correct token and injecting it as request headers using
> a browser extension. Read more [about generating principals](../../general/generating-principal.md).

This information found in the cookie is a base64 encoded string containing the JSON structure that is expected.

## Identity provider

Identity is a read only feature in the frontend. It can't be manipulated, as it is owned by the backend or the ingress.
All access to identity goes through what is called `IdentityProvider`.

The `IdentityProvider` provides functionality for getting the current identity.

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

const identity = await IdentityProvider.getCurrent();

console.log(`Hello '${identity.name}'`);
```

> Note that the `getCurrent()` method is an asynchronous operation that returns a promise.
> The reason for this is that if the cookie is not found, it will call the `.cratis/me` endpoint to try to get the identity.

## Details

Part of the identity can hold details that are beyond what the identity provider provides. These details are application specific and something that your
application or ingress should be responsible for filling out. Details can be considered optional, as that might not be a requirement for your application.

The `getCurrent()` method takes a generic parameter that allows you to specify the type of the details object.

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

type IdentityDetails = {
    department: string,
    age: number
};

const identity = await IdentityProvider.getCurrent<IdentityDetails>();

console.log(`Hello '${identity.name}' from ´${identity.details.department}`);
```

## IIdentity

The return type coming from `getCurrent()` looks like the following:

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | string | The unique identifier from the identity provider |
| name | string | The user name |
| roles | string[] | Array of roles the identity is in |
| details | any / type | Any additional identity details with type given, defaults to `any` |
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
Rather than having your user log out and back in again, you can issue a refresh. Since the cookie is there and not governed by the frontend, it
needs to call the backend or ingress to perform the refresh. The refresh calls the `.cratis/me` endpoint which returns the identity and details.
This endpoint should also be responsible for updating the cookie so that any call to `getCurrent()` on the `IdentityProvider` gives you the correct
identity and details.

To refresh the identity you can call the `refresh()` method on the identity object itself.

```typescript
import { IdentityProvider } from '@cratis/arc/identity';

let identity = await IdentityProvider.getCurrent();
identity = await identity.refresh();
```

The identity object is designed to be immutable, leading to the `refresh()` method having to return a new instance.
This means that the original `identity` instance won't be updated and you would have to replace it if you have it as a variable.
