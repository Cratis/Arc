---
title: "MVVM with React"
---

For most screens, the [React hooks](../react/index.md) keep state in the component and that's exactly
right. But as a screen's logic grows — orchestrating several commands, deriving display state, reacting
to navigation — that logic starts to crowd the markup. The **Model-View-ViewModel** approach moves it
into a dedicated view model class: the component renders, the view model decides. The result is logic
you can test and reuse independently of the JSX.

Reach for MVVM when a screen has substantial behavior; stay with plain hooks when it doesn't.

| Topic | What it covers |
| ------- | ----------- |
| [Using a view model](./using-view-model.md) | Bind a component to a view model and drive it from there. |
| [MVVM context](./mvvm-context.md) | How the MVVM context is established and resolved. |
| [TSyringe](./tsyringe.md) | Dependency injection for view models with TSyringe. |
| [Browser](./browser.md) | Working with browser APIs and navigation from a view model. |
| [Dialogs](./dialogs.md) | Driving dialogs from a view model. |
| [Identity](./identity.md) | Accessing identity and user context in MVVM. |
| [Messaging](./messaging.md) | Messaging and communication patterns in MVVM. |

New to the frontend? Start with the [React hooks](../react/index.md) first — MVVM is the next step up,
not the starting point.
