# Identity

The React implementation of identity is built on top of what you find the [core](../core/identity.md).
It provides an encapsulation that feels more natural to a React application.

## HTTP Headers

Identity fetches include `httpHeadersCallback` headers configured through [Arc](./arc.md). Use this for Authorization or application headers, not a browser `Cookie` header. Cookies are browser-managed. Identity details are untrusted UI cache data; authorization belongs on the server.

## Identity provider context

`<Arc>` already supplies an identity provider. Use a separate provider only for deliberate configuration, such as a generated details constructor; render it beneath Arc and explicitly preserve any headers you need. Identity request settings are global, so nested providers are not independent authenticated sessions.

For the default identity shape, use the provider already included in your top-level `Arc`:

```tsx
import { Arc } from '@cratis/arc.react';

export const App = () => (
    <Arc>
        <main>Your application content</main>
    </Arc>
);
```

This context can then be used anywhere by consuming the React context directly:

```typescript
import { IdentityProviderContext } from '@cratis/arc.react/identity';

export const SomeComponent = () => {
    return (
        <IdentityProviderContext.Consumer>
            {({ identity, isLoading }) => (
                <h1>{isLoading ? 'Loading…' : identity.name}</h1>
            )}
        </IdentityProviderContext.Consumer>
    );
};
```

> The raw context shape is `{ identity, detailsConstructor, isLoading, clearIdentity }`. Details live at `value.identity.details`, not `value.details`. The default generic details type is `object`; prefer a typed `useIdentity` consumer.

### Refreshing

Sometimes you need to refresh the identity due to backend changes. On the `IIdentityContext` that represents
the context you find a method called `refresh()`. Calling this will invalidate the cookie and also just call
the backend to get the current identity details.

```typescript
import { IdentityProviderContext } from '@cratis/arc.react/identity';

export const SomeComponent = () => {
    return (
        <IdentityProviderContext.Consumer>
            {({ identity }) => (
                <>
                    <h1>{identity.name}</h1>
                    <button onClick={() => void identity.refresh()}>Refresh identity</button>
                </>
            )}
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
        <h3>User: {identity.name}</h3>
    );
};
```

The `useIdentity()` hook returns the context which holds a property called `details`. This details property is what the backend
returned to the ingress middleware.

By default, if not specified, the type of the details is `object`. You can change this by passing it a generic argument with
the exact shape of what's expected:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

type Identity = {
    firstName: string;
    lastName: string;
};

export const Home = () => {
    const identity = useIdentity<Identity>();
    if (identity.isLoading) return <p>Loading…</p>;
    if (!identity.isSet || !identity.details) return <p>No identity details.</p>;

    return (
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```

The generic describes the expected payload; it does not make optional backend details present. Guard details even after loading finishes, or supply the [deliberate defaults](#default-details-value) shown below.

## Has the identity arrived yet?

The identity is fetched from the backend, which means there is a moment - short, but real - where your
application is rendering and nobody knows who the user is yet. `isSet` cannot tell you about that
moment: it reads `false` both _before_ the first request has answered and _after_ it answered that
nobody is signed in.

That is what `isLoading` is for. It is on the object `useIdentity()` returns and it separates the two:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

export const Greeting = () => {
    const identity = useIdentity();

    if (identity.isLoading) return <Spinner />;
    if (!identity.isSet) return <SignInPrompt />;

    return <h3>Welcome back, {identity.name}</h3>;
};
```

`isLoading` covers every resolution, not only the first one. Calling `refresh()` - which is what you do
after a sign-in or after the backend grants a role - puts it back up until the round-trip answers, so
anything that gates on who the user is keeps waiting instead of briefly deciding on the old identity.

Note that `isLoading` lives on `IIdentityContext`, not on the framework-agnostic `IIdentity` in the
core package. It is a React lifecycle concern and it stays on the React side.

## Role checking

The identity context includes information about the roles assigned to the user. You can check if a
user is in a specific role using the `isInRole()` method, or read the roles array directly:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

export const UserProfile = () => {
    const identity = useIdentity();

    return (
        <div>
            <h3>User: {identity.name}</h3>
            <p>Roles: {identity.roles.join(', ')}</p>
            {identity.isInRole('Admin') && <AdminBadge />}
        </div>
    );
};
```

That is the right shape for decorating a screen. It is the wrong shape for guarding one:

```typescript
// Don't do this - it decides on an identity that may not have arrived.
if (!identity.isInRole('Admin')) {
    return <div>Access denied. Admin role required.</div>;
}
```

Before the identity request answers, an administrator holds no roles yet - so this renders "Access
denied," then swaps to the panel a moment later. Every signed-in user sees the rejection flash by on
every load. There are three outcomes here, not two, and a guard has to say something about all of
them.

## Guarding a screen with RequireRole

`RequireRole` is that guard. It renders one of three slots, and which one is never in doubt:

```typescript
import { RequireRole } from '@cratis/arc.react/identity';

export const Admin = () => (
    <RequireRole
        roles={['Administrator', 'Auditor']}
        whileLoading={<Spinner />}
        forbidden={<AccessDenied />}>
        <AdminPanel />
    </RequireRole>
);
```

| Prop           | Type                             | What it does                                                                                       |
| -------------- | -------------------------------- | -------------------------------------------------------------------------------------------------- |
| `roles`        | `string[]`                       | Roles that grant access - the identity needs any one of them                                       |
| `allow`        | `(details, identity) => boolean` | Predicate deciding access from the identity's details                                              |
| `children`     | `ReactNode`                      | Rendered when the caller is authenticated and allowed                                              |
| `whileLoading` | `ReactNode`                      | Rendered while the identity is still being resolved. Defaults to nothing                           |
| `forbidden`    | `ReactNode`                      | Rendered for an anonymous caller, or an authenticated one that is not allowed. Defaults to nothing |

At least one of `roles` and `allow` has to be there; the type system says so, and the component denies
at runtime if `undefined` gets past it anyway. Supply both and both must pass.

`forbidden` is a slot rather than a `redirectTo` on purpose. Arc.React does not depend on a router, so
instead of picking one for you it lets you hand it whatever your application already uses:

```typescript
<RequireRole roles={['Administrator']} forbidden={<Navigate to="/" replace />}>
    <AdminPanel />
</RequireRole>
```

### Deciding on details

Roles are what the backend put in the token. When access depends on something your own domain knows,
pass a predicate over the identity's details:

```typescript
import { RequireRole } from '@cratis/arc.react/identity';

type OrganizationDetails = {
    department: string;
};

export const Ledger = () => (
    <RequireRole<OrganizationDetails>
        allow={details => details?.department === 'Finance'}
        forbidden={<AccessDenied />}>
        <LedgerContent />
    </RequireRole>
);
```

The details come first because that is what an application's own access rules are written against; the
whole identity arrives as a second argument when a rule also needs `isInRole`.

The details are typed as possibly absent, and the `?.` above is not decoration. An identity is set as
soon as the backend answers, whether or not the application registered anything to fill details in -
so a predicate written as though they are always there throws on exactly the deployments that have
none.

### Everything that is not a yes is a no

A gate that cannot reach a decision does not open. `RequireRole` renders `forbidden` for all of these,
and warns on the console for the ones that are configuration mistakes:

- The caller is anonymous.
- No role matched, or the predicate answered `false`.
- `roles` is an empty array. That is not "no rule" - it is a rule no caller can satisfy.
- `roles` is not an array at all (a `null` out of JSON configuration, say).
- Neither `roles` nor `allow` was supplied. A key renamed in a feature-flag object arrives as
  `undefined`, compiles, lints, and would otherwise open the panel to everyone signed in. If
  authentication alone really is the rule, say so: `allow={() => true}`.
- The predicate threw.
- The identity carries no details but an `allow` predicate was supplied. There is nothing to decide on,
  and the two natural ways to phrase a predicate disagree about absence - one throws, the other reads
  it as innocence and admits - so the gate answers instead of letting the phrasing decide.

### RequireRole hides UI, it does not protect data

> [!CAUTION]
> `RequireRole` is a usability feature, not a security boundary. The identity it reads comes from a
> cookie that is deliberately not `HttpOnly` - the frontend has to be able to read it - which means the
> browser, and anyone driving it, can edit that cookie and render these children at will.
>
> Use the gate to keep people out of screens that would only frustrate them. Never use it as the thing
> that keeps them out of the data. Every query and command behind the gate has to carry its own
> `[Authorize]` / `[Roles]` on the server, where the decision is made from the real credential and
> cannot be edited.

## Type-safe identity with complex types

If your identity details contain complex types like `Guid` from `@cratis/fundamentals`, you can enable type-safe deserialization by providing a constructor. This ensures that complex types are properly instantiated with their methods and behavior, not just plain JSON objects.

A constructor alone is not enough: Fundamentals deserialization reads declared field metadata, not the types of property initializers. Use the generated/decorated details model rather than an unannotated class.

Nest the configured provider beneath `Arc`. In this header-authenticated composition, your authentication integration supplies `httpHeadersCallback`; pass the same retained callback to both providers so the details provider does not replace it with empty headers:

```tsx
import { Arc } from '@cratis/arc.react';
import { IdentityProvider } from '@cratis/arc.react/identity';
import { UserIdentityDetails } from './generated/UserIdentityDetails';

export const App = ({
    httpHeadersCallback,
}: {
    httpHeadersCallback: () => HeadersInit;
}) => (
    <Arc httpHeadersCallback={httpHeadersCallback}>
        <IdentityProvider
            detailsType={UserIdentityDetails}
            httpHeadersCallback={httpHeadersCallback}
        >
            <main>Your application content</main>
        </IdentityProvider>
    </Arc>
);
```

Finally, use the `useIdentity()` hook with the constructor and an explicit details generic. The generic avoids inferring the constructor itself as the default-details value:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';
import { UserIdentityDetails } from './generated/UserIdentityDetails';

export const Home = () => {
    const identity = useIdentity<UserIdentityDetails>(UserIdentityDetails);
    if (identity.isLoading) return <p>Loading…</p>;
    if (!identity.isSet || !identity.details?.userId) return <p>No identity details.</p>;

    // Now identity.details.userId is a proper Guid instance with all its methods
    return (
        <>
            <h3>User ID: {identity.details.userId.toString()}</h3>
            <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
        </>
    );
};
```

The provider passes `detailsType` to `JsonSerializer.deserializeFromInstance()`. Passing a constructor to `useIdentity` alone does not deserialize an already-loaded plain object. The example requires the provider configuration and generated field metadata.

## Refreshing with hook

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
        <>
            <h3>User: {identity.details?.firstName} {identity.details?.lastName}</h3>
            <button onClick={() => void identity.refresh()}>Refresh identity</button>
        </>
    );
};
```

## Default value

You can also provide a default value for the `details` property in the identity context.

### Clearing identity

When a user logs out, you can clear the client-side identity state and remove the identity cookie using `clearIdentity()`. This is available on the object returned by `useIdentity()`:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

export const ClearIdentityCacheButton = () => {
    const identity = useIdentity();

    const handleLogout = () => {
        identity.clearIdentity();
    };

    return <button onClick={handleLogout}>Clear identity cache</button>;
};
```

Calling `clearIdentity()` does two things:

1. Removes the `.cratis-identity` cookie.
2. Resets the identity context to its initial unset state (`isSet` becomes `false`, `details` is reset).

> Clearing this cache is not logout: authentication cookies, tokens, and server sessions remain unchanged. Complete real sign-out through your authentication system first, then clear identity and [reconnect queries](./arc.md#reconnecting-queries). Reconnection uses current browser credentials and does not purge cached query data.

## Default details value

The provider starts with an empty details object, `{}`, while identity is unset. A resolved identity can still carry `null` or missing details; there is no unconditional empty-object fallback. A loading check alone does not make details safe to dereference.

Supply a typed default for display placeholders. It is used while identity is unset and when resolved details are absent; it does not fill individual missing fields or establish authentication.

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

import { UserIdentityDetails } from './generated/UserIdentityDetails';

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
