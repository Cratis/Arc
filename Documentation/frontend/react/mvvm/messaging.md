---
title: Cratis Arc MVVM messaging and scopes
description: Publish typed messages between Cratis Arc view models, choose a messenger scope for list-detail screens, and release subscriptions when a view is disposed.
---

Cratis Arc's messenger lets one view model notify another without holding a reference to it. Use it for interactions such as selecting a user in a list and displaying that user in a separate details view. The list publishes a message; the details view subscribes and updates its own state.

Messaging carries notifications, not retained screen state. Subscribers receive messages published while they are subscribed; a details view mounted after a selection will not receive that earlier selection. Keep state in a shared model or pass it through props when a late-mounted view needs the current value.

Before using the examples, configure [TSyringe](./tsyringe.md) and the [MVVM/router host](./mvvm-context.md). Both views should use `withViewModel()` so their constructor dependencies receive the messenger for the surrounding scope.

## IMessenger

Inject `IMessenger` to publish and subscribe. The implementation comes from Arc core (`@cratis/arc/messaging`); the MVVM import below re-exports the same token. Subscription matching uses the message's runtime constructor, so define a class rather than a TypeScript interface.

These examples assume your application has a `User` model in `./User`. Define `UserSelected` once in `UserSelected.ts`, then import that same class in both view models. Two separately declared classes with identical properties are different message types:

```ts
import { User } from './User';

export class UserSelected {
    constructor(readonly user: User) {}
}
```

The list view model publishes an instance when the user selects a row:

```ts
import { IMessenger } from '@cratis/arc.react.mvvm/messaging';
import { injectable } from 'tsyringe';
import { UserSelected } from './UserSelected';
import { User } from './User';

@injectable()
export class UsersListViewModel {
    // Take the IMessenger as a dependency
    constructor(private readonly _messenger: IMessenger) {}

    selectUser(user: User) {
        // Publish the message saying the user was selected
        this._messenger.publish(new UserSelected(user));
    }
}
```

Wire the list view's selection handler to `selectUser(user)`. The details view model subscribes to `UserSelected` and retains the returned RxJS `Subscription` so it can stop receiving messages on disposal:

```ts
import { IMessenger } from '@cratis/arc.react.mvvm/messaging';
import { injectable } from 'tsyringe';
import { Subscription } from 'rxjs';
import { UserSelected } from './UserSelected';
import { User } from './User';

@injectable()
export class UserDetailsViewModel {
    user?: User;
    private readonly _subscription: Subscription;

    constructor(messenger: IMessenger) {
        this._subscription = messenger.subscribe(UserSelected, (message) => {
            this.user = message.user;
        });
    }

    detached() {
        this._subscription.unsubscribe();
    }
}
```

With both views mounted in the same scope, selecting a row assigns the selected user to `UserDetailsViewModel.user`. Render that property inside the details view's `withViewModel()` boundary to observe its changes. The handler receives the published `UserSelected` instance, so it reads `message.user`, not properties of `User` directly.

The `detached()` method releases the subscription when the view model is disposed. With `<Arc development>`, ordinary unmounts skip disposal, so a removed details view can remain subscribed. Do not rely on `detached()` to release resources in that mode; see the [view model lifecycle](./using-view-model.md#view-model-lifecycle) for the exact boundary.

## Scoped messaging

The root `<Arc />` provides the application messenger. To keep a list-detail interaction local, import `MessengerScope` from `@cratis/arc.react/messaging` and wrap both views in one `<MessengerScope>`. `withViewModel()` injects the nearest scoped messenger; without a local scope, it uses the Arc root messenger. Keep the publisher and subscriber under the same wrapper unless you deliberately want messages to cross scope boundaries.

Two props control propagation:

- `bubbleToParent` defaults to `false`. Set it to `true` when messages published inside this scope should also reach its parent.
- `trickleDownToChildren` defaults to `true`. Set it to `false` on a scope whose messages should not propagate into its child scopes.

By default, a nested scope receives messages from its parent but does not publish back to it. A parent that receives a bubbling message can also send it to sibling scopes when its downward propagation is enabled. Choose these directions deliberately if more than one list-detail pair appears on a page.

Unmounting `MessengerScope` removes its messenger from the parent's child scopes. That does not replace unsubscribing the handlers your view models own. Once the scope is placed, use [the view model binding guide](./using-view-model.md) to connect the list and details renderers.
