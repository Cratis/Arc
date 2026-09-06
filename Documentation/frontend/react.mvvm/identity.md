# Identity

The MVVM implementation of identity is built on top of what you find the [core](../core/identity.md).
To access identity in an MVVM solution with a view model, the `IdentityProvider` is hooked up to the [container](./tsyringe.md)
through its interface `IIdentityProvider`.

> Note: This depends on the [MVVM Context](./mvvm-context.md) being used.

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

The code takes a dependency to the abstract class called `IIdentityProvider` representing the interface for the identity provider.
With this the code simply calls the `getCurrent()` method to get the identity.

> [!IMPORTANT]
> The `<IdentityDetails>` above is a **type parameter** - it only shapes what TypeScript expects `identity.details` to look like at compile time and has no effect at runtime. `identity.details` is still the raw JSON object the identity provider returned.
>
> If your details contain complex types like `Guid` that need to be properly instantiated - not left as plain JSON - pass a class **constructor** as a runtime argument to `getCurrent()` instead of (or in addition to) a type parameter, with an `@field` decorator on every property you want deserialized:
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
>
> A class with no `@field` decorators cannot be deserialized into - the raw payload is passed through unchanged instead of being silently blanked.
