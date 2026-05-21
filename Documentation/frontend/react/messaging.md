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

`useMessenger()` resolves the nearest scope first, then falls back to the Arc root messenger.

By default:

- Child publishes do **not** bubble to parent.
- Parent publishes trickle down to children recursively.

You can override this on a scope with:

- `bubbleToParent`
- `trickleDownToChildren`
