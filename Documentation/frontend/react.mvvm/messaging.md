# Messaging

At times you want to allow multiple features on a page to know about changes that happens in one component.
A common scenario is a list + detail scenario where you typically have a list and when you click an item you
get the details of it somewhere else on the page.

With React you would typically create a context and wrap the components needing the same information in
a React context. The problem with this is that you are having to create these different contexts, which creates
a set of boiler plate code that has little value and you need to put it into the correct place in the
rendering hierarchy. This creates a logical coupling in your application and is error prone when changing
structure.

With the MVVM model this doesn't really work either, as the contexts aren't available to the view model.
Instead, we can leverage a publish/subscribe mechanism where components through their view model can
publish messages that can be subscribed to by other parts of your system that lives on the page.

This creates for a more decoupled approach and making your code and structure easier to change.

## IMessenger

In Cratis Arc the system that deals with this is called `IMessenger`.
It has a method called `publish()` for publishing messages, and one for subscribing called `subscribe()`.
Both of these methods work on types and require a runtime type like a `class` to be the message type.

Below is an example of the view models for two components, one listing items and the other holding details.

The first thing we want is a message saying user is selected (assuming a type of `User`):

```ts
export class UserSelected {
    constructor(readonly user: User) {
    }
}
```

Then for the list component we would typically have something like the following:

```ts
import { IMessenger } from '@cratis/arc.react.mvvm/messaging';

@injectable()
export class UsersListViewModel {

    // Take the IMessenger as a dependency
    constructor(private readonly _messenger: IMessenger) {
    }

    selectUser(user: User) {
        // Publish the message saying the user was selected
        this._messenger.publish(new UserSelected(user));
    }
}
```

Whenever a selection is done, the view would call the `selectUser()` method, which
would then publish the message.

For the details component, the view model would then need to subscribe to this:

```ts
import { IMessenger } from '@cratis/arc.react.mvvm/messaging';

@injectable()
export class UserDetailsViewModel {

    // Take the IMessenger as a dependency
    constructor(messenger: IMessenger) {
        messenger.subscribe(UserSelected, (user) => this.user = user)
    }

    // State that would be used in the view
    user?: User;
}
```

With this, the details view model will subscribe to the `UserSelected` message and set the state of the
view model accordingly.
