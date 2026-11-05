---
title: Cratis Arc MVVM identity access
description: Retrieve the current user's identity in a Cratis Arc view model, check whether it is set, and handle cached identity and typed application details.
---

Inject `IIdentityProvider` when a Cratis Arc view model needs the current user's name, roles, or application-specific details. Its asynchronous `getCurrent()` method returns an identity; check `isSet` before using that result as a signed-in user. Client-side identity checks do not replace server-side authorization.

Set up [TSyringe metadata](./tsyringe.md) and the [MVVM context](./mvvm-context.md) first. The context registers the [core identity provider](../../core/identity.md), so the view model only needs to declare its constructor dependency:

```typescript
import { injectable } from 'tsyringe';
import { IIdentityProvider } from '@cratis/arc/identity';

type IdentityDetails = {
    department: string,
    age: number
};

@injectable()
export class MyViewModel {
    constructor(private readonly _identityProvider: IIdentityProvider) {
    }

    async sayHello() {
        const identity = await this._identityProvider.getCurrent<IdentityDetails>();
        console.log(`Hello '${identity.name}' from ´${identity.details.department}`);
    }
}
```

`IIdentityProvider` is an abstract class, so it exists at runtime as a dependency token. Calling `sayHello()` retrieves the identity and reads the backend's `department` value. This example assumes a signed-in user and a matching details payload; in your screen's loading flow, handle an unset identity and request failures before displaying user-specific data.

## Deserialize application-specific details

The `<IdentityDetails>` type parameter above describes the expected JSON shape to TypeScript. It has no effect at runtime: `identity.details` remains the raw payload returned by the provider.

If details contain a value such as `Guid` that needs to become a class instance, pass a class constructor as an argument to `getCurrent()`. Add an `@field` decorator to each property you want populated:

> [!IMPORTANT]
> Use a runtime constructor for deserialization, not only a generic type argument:
>
> ```typescript
> import { injectable } from 'tsyringe';
> import { IIdentityProvider } from '@cratis/arc/identity';
> import { Guid, field } from '@cratis/fundamentals';
>
> class IdentityDetails {
>     @field(Guid)
>     userId!: Guid;
> }
>
> @injectable()
> export class MyViewModel {
>     constructor(private readonly _identityProvider: IIdentityProvider) {
>     }
>
>     async sayHello() {
>         // The constructor argument is what deserializes - not just a <IdentityDetails> type parameter.
>         const identity = await this._identityProvider.getCurrent(IdentityDetails);
>         console.log(identity.details.userId.toString());
>     }
> }
> ```

A class with no `@field` decorators leaves the raw payload unchanged rather than discarding its properties. The backend determines which details are present; see [backend identity configuration](../../../backend/csharp/identity/index.md) to define that payload.

## Cache and refresh boundaries

The provider fetches identity from the authenticated `/.cratis/me` endpoint and keeps a successful result in memory for the page. Creating another view model does not force a new request. If that endpoint returns 404, it falls back to the readable `.cratis-identity` cookie used by earlier backends; that compatibility path is scheduled for removal.

Use the returned identity's `refresh()` method when the server must resolve the identity again. Await the result and replace any identity held by the view model: refreshing does not mutate every previously returned object. Clear cached identity as part of your application's logout flow.

For loading state and identity updates shared by React components, use the [React identity integration](../identity.md). It also explains how to refresh and clear identity without keeping a separate copy in each view model.
