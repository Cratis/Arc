---
title: Cratis Arc frontend messaging lifecycle
description: Cratis Arc Messenger coordinates frontend UI through typed in-memory messages. Subscribe before publishing, control scope, and clean up subscriptions.
---

Cratis Arc's `Messenger` lets one part of a frontend notify another through typed, in-memory messages. Use it when a customer selection should update a details panel without making the selection code hold a reference to that panel. Both parts use the same messenger, or connected scopes in a messenger hierarchy.

## IMessenger

`IMessenger` defines publishing, subscribing, and propagation settings. `Messenger` implements that contract and returns an RxJS subscription from `subscribe(MessageType, handler)`. The subscription connects a handler to future messages until you call `unsubscribe()`.

Define a class for each message type and publish an instance of that class. Matching uses the exact constructor passed to `subscribe`, not a string name or an object's properties. A plain object with the same `customerId` property will not reach the `CustomerSelected` handler below.

```typescript
import { Messenger } from '@cratis/arc/messaging';

class CustomerSelected {
    constructor(readonly customerId: string) {}
}

const messenger = new Messenger();
const subscription = messenger.subscribe(CustomerSelected, message => {
    console.log('Load customer details:', message.customerId);
});

messenger.publish(new CustomerSelected('customer-42'));
subscription.unsubscribe();
messenger.dispose();
```

The example registers the details handler before publishing the selection, so the handler logs `customer-42`. It then releases the subscription and disposes the messenger. In an application, keep the subscription for as long as the panel needs updates and unsubscribe when the panel closes.

Messages are delivered to current subscribers only. A panel that subscribes after the selection does not receive it; keep current selection state separately if the panel needs an initial value. Publishing also does not send a request to a backend or persist a domain event. Use a [command](./commands/index.md) for a backend state change.

## Scoped hierarchy

Construct a child with `new Messenger(parent)` when a feature needs a local group of subscribers. The constructor registers the child with its parent. A published message reaches local subscribers first, then bubbles upward if enabled, and finally travels down to children if enabled.

| Setting | Default | Effect |
| --- | --- | --- |
| `bubbleToParent` | `false` | Allows a locally published message to travel upward. Each ancestor's setting controls further bubbling. |
| `trickleDownToChildren` | `true` | Allows delivery down the child tree. Each receiving scope controls propagation to its own children. |

### Propagation and cleanup

A child receives messages from its parent even when the child's own `trickleDownToChildren` is `false`; that setting stops delivery to the child's descendants. If the child also enables `bubbleToParent`, its locally published messages can reach sibling scopes through a parent that trickles. The sending child is excluded from that return path, so it does not receive the same message back from its parent.

When a feature closes, unsubscribe its handlers and call `dispose()` on its messenger to detach it from the parent. These are separate steps: `dispose()` removes the parent-child registration but does not unsubscribe local handlers or complete the local message stream.

## React usage

In React components, use `useOnMessage` to keep a subscription for the component's lifetime and clean it up on unmount. `useMessenger` returns the nearest messenger from context for publishing. Continue with [React Messaging](../react/messaging.md) to place scopes around a feature and connect its components.

## See also

- [Core TypeScript](index.md)
- [Arc provider configuration](../react/arc.md)
