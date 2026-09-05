# Messaging

`<Arc />` sets up a root messenger that is available through React context.

Use `useMessenger()` to get the nearest messenger:

```tsx
import { useMessenger } from '@cratis/arc.react/messaging';

export const Details = () => {
    const messenger = useMessenger();
    // use messenger.subscribe(...) or messenger.publish(...)
    return null;
};
```

## Subscribing — `useOnMessage`

`useOnMessage` subscribes to messages of a given type from the nearest messenger for
the lifetime of the component. The subscription is cleaned up automatically on unmount,
and the callback does not need to be wrapped in `useCallback` — the latest reference is
always invoked.

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

An optional filter narrows on message content without re-subscribing. The filter receives
the actual typed message and tells the hook whether the consumer is interested:

```tsx
useOnMessage(
    UserSelected,
    message => console.log('Selected admin:', message.userId),
    message => message.role === 'admin'
);
```

## Publishing — `useSendMessage`

`useSendMessage` returns a stable function that publishes to the nearest messenger.
Use it when a component needs to send messages without holding a reference to the
messenger itself:

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

Use `<MessengerScope>` to create a nested messenger:

```tsx
import { MessengerScope } from '@cratis/arc.react/messaging';

export const Page = () => (
    <MessengerScope>
        <FeatureA />
        <FeatureB />
    </MessengerScope>
);
```

`useMessenger()`, `useOnMessage`, and `useSendMessage` all resolve the nearest scope
first, then fall back to the Arc root messenger.

By default:

- Child publishes do **not** bubble to parent.
- Parent publishes trickle down to children recursively.

You can override this on a scope with:

- `bubbleToParent`
- `trickleDownToChildren`
