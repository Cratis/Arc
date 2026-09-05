# Messaging

Arc core provides a typed publish/subscribe messenger through `@cratis/arc/messaging`.

## IMessenger

`IMessenger` publishes and subscribes by runtime type:

```ts
import { IMessenger } from '@cratis/arc/messaging';

class UserSelected {
    constructor(readonly userId: string) {
    }
}

messenger.subscribe(UserSelected, message => {
    console.log(message.userId);
});

messenger.publish(new UserSelected('42'));
```

## Scoped hierarchy

`Messenger` can be created with a parent messenger:

```ts
import { Messenger } from '@cratis/arc/messaging';

const root = new Messenger();
const child = new Messenger(root);
```

Default behavior:

- Messages published in a child stay in that branch (no bubbling to parent).
- Messages published in a parent trickle down to all descendants recursively.

### Override properties

Each messenger has two properties:

- `bubbleToParent` (default `false`) — bubble published messages to parent.
- `trickleDownToChildren` (default `true`) — forward messages to child messengers.
