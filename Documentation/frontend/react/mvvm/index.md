---
title: Cratis Arc MVVM for React
description: Move complex React screen behavior into Cratis Arc view models, configure dependency injection, and manage state and subscriptions across a view's lifetime.
---

Cratis Arc's Model-View-ViewModel (MVVM) integration moves React screen behavior into a class while the component renders its state. Use it when coordinating commands, deriving display values, or responding to other features makes the JSX difficult to follow. Small screens can stay with [React hooks](../../react/index.md); MVVM is optional.

A component wrapped with `withViewModel()` gets a view model whose state is made observable through MobX. Reads in the wrapped render determine which changes update the view. The instance survives re-renders, so it can hold the screen's state without rebuilding its dependencies on each render. You can exercise that behavior separately from the component.

## Adopt MVVM on one screen

Keep your existing Arc host and start with one component:

1. Configure [TSyringe and decorator metadata](./tsyringe.md) so constructor dependencies can be resolved at runtime.
2. Add the [MVVM context](./mvvm-context.md) and ensure the component renders inside React Router. MVVM does not supply a router.
3. [Bind a component to a view model](./using-view-model.md), then move its behavior into the class. Keep observable reads inside the wrapped render, or pass plain values to child components.

The view model's lifetime belongs to the mounted view, not to the whole application. Release subscriptions in `detached()` when it is disposed. With `<Arc development>`, ordinary unmounts skip that disposal; read the [lifecycle contract](./using-view-model.md#view-model-lifecycle) before moving resource-owning behavior into a view model.

## Add the integrations your screen needs

| Topic | What it covers |
| ------- | ----------- |
| [Using a view model](./using-view-model.md) | Bind a component to a view model and drive it from there. |
| [MVVM context](./mvvm-context.md) | Place the providers and understand application-wide configuration. |
| [TSyringe](./tsyringe.md) | Dependency injection for view models with TSyringe. |
| [Browser](./browser.md) | Save browser preferences and understand navigation callback limits. |
| [Dialogs](./dialogs.md) | Driving dialogs from a view model. |
| [Identity](./identity.md) | Accessing identity and user context in MVVM. |
| [Messaging](./messaging.md) | Notify another feature and keep messages within the intended scope. |

If you are new to the frontend integration, start with the [React hooks](../../react/index.md) to connect a command or query before deciding where its screen behavior should live.
