---
title: Cratis Arc React messaging between components
description: Coordinate React components with typed Cratis Arc messages, automatic subscription cleanup and scopes that control where messages travel.
---

Cratis Arc React messaging lets components coordinate through typed messages without passing callbacks through every intermediate component. For example, a user list can publish a selection that a details panel receives. These are in-memory UI messages, not persisted events or requests to the backend.

`<Arc />` supplies the root messenger through React context. Use `useOnMessage` and `useSendMessage` for component subscriptions and publishing, or `useMessenger()` when you need the messenger directly:

```tsx
import { useMessenger } from '@cratis/arc.react/messaging';

export const Details = () => {
    const messenger = useMessenger();
    // use messenger.subscribe(...) or messenger.publish(...)
    return null;
};
```

## Subscribing — `useOnMessage`

Call `useOnMessage` in the component that should react to a message. It subscribes through the nearest messenger and cleans up when the component unmounts. You do not need `useCallback` around the handler; the hook invokes the latest callback reference.

```tsx
import { useOnMessage } from '@cratis/arc.react/messaging';

class UserSelected {
    constructor(readonly userId: string) {
    }
}

export const UserDetails = () => {
    useOnMessage(UserSelected, message => {
        console.log('Selected user:', message.userId);
    });
    return null;
};
```

### Filtering

Pass a filter when the panel should handle only some messages of that type. It receives the typed message and can change without creating a new subscription:

```tsx
useOnMessage(
    UserSelected,
    message => console.log('Selected tracked user:', message.userId),
    message => message.userId === '42'
);
```

## Publishing — `useSendMessage`

In the list, call `useSendMessage()` and publish a `UserSelected` instance when the user makes a choice. The returned function stays stable while the nearest messenger stays the same:

```tsx
import { useSendMessage } from '@cratis/arc.react/messaging';

class UserSelected {
    constructor(readonly userId: string) {
    }
}

export const UserList = () => {
    const send = useSendMessage();
    return (
        <button onClick={() => send(new UserSelected('42'))}>
            Select user
        </button>
    );
};
```

## Scoped messenger hierarchy

Use `<MessengerScope>` when a group of components should share a local messenger. `FeatureA` and `FeatureB` below can exchange messages without sending them up to the root by default:

```tsx
import { MessengerScope } from '@cratis/arc.react/messaging';

export const Page = () => (
    <MessengerScope>
        <FeatureA />
        <FeatureB />
    </MessengerScope>
);
```

The hooks use the nearest `MessengerScope`, or the Arc root messenger if no local scope exists. This determines who can receive a published message.

By default, messages stay out of parent scopes but travel down to child scopes recursively. Set `bubbleToParent` on a scope to let its messages reach its parent. Set `trickleDownToChildren={false}` to stop that scope from forwarding messages to its children.

The subscriber and publisher must use the same message class constructor. The examples repeat `UserSelected` to show each side; in your application, define it once and import it in both components.

For subscriptions outside React components, see [core messaging](../core/messaging.md). If you coordinate through view models, continue with [MVVM messaging](./mvvm/messaging.md).
