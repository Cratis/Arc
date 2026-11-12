---
title: Cratis Arc React identity and role-based UI
description: Read the current user in Cratis Arc React, wait for identity to load and show role-based UI while keeping authorization on the server.
---

Cratis Arc's `useIdentity()` hook gives React components the current user's name, roles and application-specific details. Use it to personalize a screen or decide which UI to show, and wait for identity to load before treating the user as anonymous. These browser-side checks do not replace server authorization.

The hooks use Arc's [core identity model](../core/identity.md). Components such as `Spinner`, `SignInPrompt` and `AccessDenied` in the examples represent your application's UI.

## HTTP Headers

The `/.cratis/me` endpoint derives identity from the authenticated request. Identity fetches include headers from the `httpHeadersCallback` configured on [Arc](./arc.md), so use that callback for an `Authorization` header or application headers. Do not set a browser `Cookie` header there; the browser manages cookies.

## Identity provider context

Start with the identity provider already mounted by `<Arc>`. To deserialize typed details, configure `detailsType` on `<Arc>` as shown [below](#type-safe-identity-with-complex-types). If you need a separate `IdentityProvider`, render it beneath Arc and explicitly preserve the headers you need. Identity request settings are global; nested providers do not create independent authenticated sessions.

For the default identity shape, use the provider already included in your top-level `Arc`:

```tsx
import { Arc } from '@cratis/arc.react';

export const App = () => (
    <Arc>
        <main>Your application content</main>
    </Arc>
);
```

Components under the provider can consume the context directly, though most components can use the shorter [`useIdentity()` hook](#useidentity-hook):

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

Call `refresh()` after a sign-in or a backend change that affects the user's identity. It requests the current identity from the backend and replaces the value kept in memory:

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

Call `useIdentity()` in a component under `<Arc>` to read the current identity:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

export const Home = () => {
    const identity = useIdentity();

    return (
        <h3>User: {identity.name}</h3>
    );
};
```

The returned `IIdentityContext` includes `details`, the application-specific payload supplied by the backend. Its default TypeScript type is `object`. Pass a type argument to describe the payload your application expects:

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

While the identity request is pending, the application does not yet know whether the user is signed in. `isSet` is `false` both before the request answers and after it resolves to an anonymous user. Check `isLoading` first so the screen can distinguish waiting from signed out:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

export const Greeting = () => {
    const identity = useIdentity();

    if (identity.isLoading) return <Spinner />;
    if (!identity.isSet) return <SignInPrompt />;

    return <h3>Welcome back, {identity.name}</h3>;
};
```

`refresh()` sets `isLoading` back to `true` until the new request finishes. Keep the loading branch during refresh so the UI does not decide access using stale identity data.

`isLoading` belongs to React's `IIdentityContext`, not the framework-independent `IIdentity` in the core package.

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

A direct role check works for an optional badge. To decide whether to render an entire screen, it also needs a loading check. This check alone is not enough:

```typescript
// Don't do this - it decides on an identity that may not have arrived.
if (!identity.isInRole('Admin')) {
    return <div>Access denied. Admin role required.</div>;
}
```

Before the request answers, the client has no roles to match. Even an administrator can briefly see “Access denied” before the panel appears. Handle loading separately from allowed and forbidden states, or use `RequireRole` to do that for you.

## Guarding a screen with RequireRole

`RequireRole` selects the loading, forbidden or allowed content for a configured access rule. Here, either `Administrator` or `Auditor` is sufficient:

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

Supply `roles`, `allow`, or both. If both are present, both must pass. The type requires an access rule, and the component also denies at runtime if the rule is missing.

Arc React does not depend on a router. To redirect a forbidden user, pass your router's navigation component as the `forbidden` content:

```typescript
<RequireRole roles={['Administrator']} forbidden={<Navigate to="/" replace />}>
    <AdminPanel />
</RequireRole>
```

### Deciding on details

When a screen depends on application-specific data rather than roles alone, pass an `allow` predicate over the identity's details:

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

The predicate receives details first and the whole identity second, so a rule can also call `isInRole`. Details are typed as possibly absent: a resolved identity does not imply that the backend supplied application details. Keep that distinction when writing predicates and displaying user information.

### Everything that is not a yes is a no

`RequireRole` renders `forbidden` when it cannot allow access. If the panel stays hidden, check for these conditions:

- The caller is anonymous.
- No supplied role matches, or the predicate returns anything other than `true`.
- `roles` is an empty array or is not an array.
- Neither `roles` nor `allow` was supplied.
- The predicate throws.
- An `allow` predicate is supplied but identity details are absent.

Missing rules, a non-array `roles` value, a thrown predicate and absent details for a predicate produce console warnings. For an authentication-only rule, use `allow={() => true}`; the predicate path still requires details to be present.

### RequireRole hides UI, it does not protect data

> [!CAUTION]
> `RequireRole` is a usability feature, not a security boundary. The identity it reads comes from
> `/.cratis/me` and is held in the page's memory, which means anyone with the browser's developer tools
> can change it and render these children at will.
>
> Use the gate to avoid showing screens the user cannot use. Protect every query and command behind
> it with server-side authorization, such as `[Authorize]` / `[Roles]`, where the decision uses the
> authenticated request rather than browser state.

## Hiding what is not available

Roles decide what a user may do. Whether a screen exists at all for the user's tenant, such as a commercial package, is [availability](availability.md). Combine the two by nesting `RequireAvailable` around `RequireRole`:

```tsx
import { RequireRole } from '@cratis/arc.react/identity';
import { RequireAvailable } from '@cratis/arc.react/availability';
import { QualityScores } from './QualityScores';

export const DataQuality = () => (
    <RequireAvailable part={QualityScores} whileLoading={<Spinner />}>
        <RequireRole roles={['Administrator']} forbidden={<AccessDenied />}>
            <QualityScoresTable />
        </RequireRole>
    </RequireAvailable>
);
```

The part is hidden and shown again as the server reports changes, without a reload.

## Type-safe identity with complex types

If details contain a type such as Fundamentals' `Guid`, configure `<Arc detailsType={...}>` to deserialize the payload into class instances. A TypeScript type argument alone describes the shape; it does not turn JSON into objects with methods.

First, define your identity details class with an `@field` decorator on every property that should be deserialized:

```typescript
import { Guid, field } from '@cratis/fundamentals';

class UserIdentityDetails {
    @field(Guid)
    userId!: Guid;

    @field(String)
    firstName!: string;

    @field(String)
    lastName!: string;
}
```

> [!IMPORTANT]
> Deserialization copies only members declared with `@field`. If the class has no decorated members, Arc warns and returns the raw payload rather than creating an empty instance. Decorate every property you expect to deserialize.

Pass that constructor to `<Arc>`. Its internal `IdentityProvider` deserializes available details once, making the instance available to consumers below it:

```typescript
import { Arc } from '@cratis/arc.react';

export const App = () => {
    return (
        <Arc detailsType={UserIdentityDetails}>
            {/* ... your app content ... */}
        </Arc>
    );
};
```

If you wire `IdentityProvider` directly instead of using `<Arc>` - see [Identity provider context](#identity-provider-context) - it accepts the same `detailsType` prop.

The provider's runtime configuration does not change the hook's TypeScript return type. Pass the details type where you read it. The examples below focus on property access; add loading and absent-detail branches before rendering these values in your application:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

export const Home = () => {
    const identity = useIdentity<UserIdentityDetails>();
    // identity.details.userId is a proper Guid instance with all its methods
    return (
        <>
            <h3>User ID: {identity.details.userId.toString()}</h3>
            <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
        </>
    );
};
```

A shared component can pass the constructor to `useIdentity()` when it cannot rely on the host's `detailsType` configuration:

```typescript
import { useIdentity } from '@cratis/arc.react/identity';

export const Home = () => {
    const identity = useIdentity(UserIdentityDetails);

    return (
        <h3>User ID: {identity.details.userId.toString()}</h3>
        <h3>User: {identity.details.firstName} {identity.details.lastName}</h3>
    );
};
```

You can configure both the provider and the hook. If the payload is already an instance of the requested type, the hook returns it unchanged. Otherwise, `JsonSerializer.deserializeFromInstance()` reconstructs the decorated members, including supported complex types such as `Guid` and `DateOnly`.

## Refreshing with hook

The hook returns the same `refresh()` method as the raw context. Call it when you need to fetch updated identity data:

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

A display may need placeholder details while identity is unset or details are absent. See [Default details value](#default-details-value) for that pattern. Defaults do not establish authentication; clear stale identity separately after sign-out.

### Clearing identity

When a user logs out, you can clear the client-side identity state using `clearIdentity()`. This is available on the object returned by `useIdentity()`:

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

1. Forgets the identity kept in memory by `IdentityProvider`, so the next request for it asks `/.cratis/me` again.
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
import { Guid, field } from '@cratis/fundamentals';

class UserIdentityDetails {
    @field(Guid)
    userId!: Guid;

    @field(String)
    firstName!: string;

    @field(String)
    lastName!: string;
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

> [!NOTE]
> Defaults are used as-is, without deserialization. Supply the final typed value, constructing any complex members yourself, such as the `Guid` in the constructor example above.

For server-side identity setup, continue with [core identity](../core/identity.md). If sign-in or sign-out also changes what an open query can read, follow [reconnecting queries](./arc.md#reconnecting-queries).
