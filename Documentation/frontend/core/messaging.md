---
title: Messaging
description: Publish and subscribe to typed frontend messages with Messenger, including subscription cleanup and parent-child scope propagation.
---

`Messenger` connects frontend publishers and subscribers through typed, in-memory messages. Use it for UI coordination, such as letting a details panel react to a selection, without coupling the publisher to the panel.

## IMessenger

- `IMessenger` defines `publish(...)`, `subscribe(...)`, and propagation settings.
- `Messenger` implements that contract with RxJS subscriptions.
- `subscribe(MessageType, handler)` returns a subscription; call `unsubscribe()` when its owner no longer needs messages.

Messages are matched by their constructor, not by a string name or a matching object shape. Publish an instance of the same class passed to `subscribe`; a plain object with identical properties does not have that constructor.

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

Register subscribers before publishing. This is a live message channel, not a replaying store: a later subscriber does not receive earlier selections. It also does not send messages to a backend or persist them as domain events.

## Scoped hierarchy

Construct a child with `new Messenger(parent)` when a feature needs its own scope. A message is delivered locally, then follows these settings:

| Setting | Default | Effect |
| --- | --- | --- |
| `bubbleToParent` | `false` | Allows a locally published message to travel upward. Each ancestor's setting controls further bubbling. |
| `trickleDownToChildren` | `true` | Allows delivery down the child tree. Each receiving scope controls propagation to its own children. |

### Propagation and cleanup

When a child bubbles a message to a parent that trickles, sibling scopes can receive it too; the sending child is excluded from that return path. Use propagation deliberately rather than assuming a child scope isolates every message.

`dispose()` detaches a messenger from its parent. It does not replace subscription cleanup: keep and unsubscribe subscriptions when their handlers are no longer needed.

## React usage

React applications use the nearest messenger from context. `useOnMessage` handles subscription cleanup on unmount; `useMessenger` exposes the messenger for publishing. See [React Messaging](../react/messaging.md) for component examples and scope configuration.

## See also

- [Core TypeScript](index.md)
- [Arc provider configuration](../react/arc.md)
