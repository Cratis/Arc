# Identity

The React implementation of identity is built on top of what you find the [core](../core/identity.md).
It provides an encapsulation that feels more natural to a React application.

## HTTP Headers

Identity requests automatically include any HTTP headers provided by the `httpHeadersCallback` configured in the [Arc](./arc.md). This is particularly useful for including authentication cookies or other headers needed for identity verification and retrieval.

## Identity provider context

To use the identity system you need to provide the identity context for your application.

At the top level of your application, typically in your `App.tsx` file you would add the provider by doing the following:

```typescript
import { IdentityProvider } from '@cratis/arc.react/identity';

export const App = () => {
    return (
        <IdentityProvider>
            {/* ... your app content ... */}
        </IdentityProvider>
    );
};
```

This context can then be used anywhere by consuming the React context directly:

```typescript
import { IdentityProviderContext } from '@cratis/arc.react/identity';

export const SomeComponent = () => {
    return (
        <IdentityProviderContext.Consumer>
            {({ details }) => {
                const actualDetails = details as Identity;
                return (
                    <h1>{actualDetails.firstName} {actualDetails.firstName}</h1>
                );
            }}
        </IdentityProviderContext.Consumer>
    );
};
```

> Note: As you can see, the `details` type will be of type `any` in the context. This means that if your type is
> a specific type, you'll need to cast it to that type before using it.

### Refreshing

Sometimes you need to refresh the identity due to backend changes. On the `IIdentityContext` that represents
the context you find a method called `refresh()`. Calling this will invalidate the cookie and also just call
the backend to get the current identity details.

```typescript
import { IdentityProviderContext } from '@cratis/arc.react/identity';

export const SomeComponent = () => {
    return (
        <IdentityProviderContext.Consumer>
            {(identity) => {
                const actualDetails = identity.details as Identity;
                return (
                    <h1>{actualDetails.firstName} {actualDetails.firstName}</h1>

                    {/* Refresh button */}
                    <button onClick={() => identity.refresh()}>Refresh identity</button>
                );
            }}
        </IdentityProviderContext.Consumer>
    );
};
```

## useIdentity() hook

Anywhere within your application you can then access the identity by adding using the `useIdentity()` hook:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

export const Home = () => {
    const identity = useIdentity();

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```

The `useIdentity()` hook returns the context which holds a property called `details`. This details property is what the backend
returned to the ingress middleware.

By default, if not specified, the type of the details is `any`. You can change this by passing it a generic argument with
the exact shape of what's expected:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

type Identity = {
    firstName: string;
    lastName: string;
};

export const Home = () => {
    const identity = useIdentity<Identity>();

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```

### Type-safe identity with complex types

If your identity details contain complex types like `Guid` from `@cratis/fundamentals`, you can enable type-safe deserialization by providing a constructor. This ensures that complex types are properly instantiated with their methods and behavior, not just plain JSON objects.

First, define your identity details class:

```typescript
import { Guid } from '@cratis/fundamentals';

class UserIdentityDetails {
    userId: Guid = Guid.empty;
    firstName: string = '';
    lastName: string = '';
}
```

Then, configure the `IdentityProvider` with the details type:

```typescript
import { IdentityProvider } from '@cratis/arc.react/identity';

export const App = () => {
    return (
        <IdentityProvider detailsType={UserIdentityDetails}>
            {/* ... your app content ... */}
        </IdentityProvider>
    );
};
```

Finally, use the `useIdentity()` hook with the constructor:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';

class UserIdentityDetails {
    userId: Guid = Guid.empty;
    firstName: string = '';
    lastName: string = '';
}

export const Home = () => {
    const identity = useIdentity(UserIdentityDetails);

    // Now identity.details.userId is a proper Guid instance with all its methods
    return (
        <h3>User ID: {identity.details.userId.toString()}</h3>
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```

This approach uses `JsonSerializer.deserializeFromInstance()` under the hood to recursively deserialize complex types, ensuring that types like `Guid`, `DateTime`, and other custom types are properly instantiated rather than being plain JSON objects.

### Refreshing with hook

Since the `useIdentity()` returns an instance of the `IIdentityContext`. So for refreshing with a hook, its easily
accessible:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

type Identity = {
    firstName: string;
    lastName: string;
};

export const Home = () => {
    const identity = useIdentity<Identity>();

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>

        {/* Refresh button */}
        <button onClick={() => identity.refresh()}>Refresh identity</button>
    );
};
```

## Default value

You can also provide a default value for the `details` property in the identity context.
If you don't provide one, it will default to an empty object, `{}`.
This is especially useful when working in local development and the cookie has not been provided

The default value can be provided as an argument to the `useIdentity()` hook:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

type Identity = {
    firstName: string;
    lastName: string;
};

export const Home = () => {
    const identity = useIdentity<Identity>({
        firstName: '[N/A]',
        lastName: '[N/A]'
    });

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```

When using the type-safe overload with a constructor, the default value is provided as the second parameter:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';
import { Guid } from '@cratis/fundamentals';

class UserIdentityDetails {
    userId: Guid = Guid.empty;
    firstName: string = '';
    lastName: string = '';
}

export const Home = () => {
    const defaultDetails: UserIdentityDetails = {
        userId: Guid.empty,
        firstName: '[N/A]',
        lastName: '[N/A]'
    };
    
    const identity = useIdentity(UserIdentityDetails, defaultDetails);

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```

