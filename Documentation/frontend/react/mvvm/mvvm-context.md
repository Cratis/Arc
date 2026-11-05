---
title: Cratis Arc MVVM context setup
description: Place the Cratis Arc MVVM provider and React Router around your views, and understand which configuration and dependencies are shared across the app.
---

The Cratis Arc `<MVVM>` component configures [MobX](https://mobx.js.org), the library that tracks view model state changes, and registers shared services with TSyringe. Place it near the application root before rendering views created with `withViewModel()`. It supplements your `<Arc>` host; it does not replace that host or provide routing.

## Place MVVM and the router

First configure [TSyringe's decorator metadata](./tsyringe.md). The MVVM package declares React and React DOM 18 or 19, and `react-router-dom` 7, as peer dependencies.

Render the following `App` beneath your existing `<Arc>` provider. Every `withViewModel()` component must also be inside a router, even if that view does not use route parameters: the wrapper calls React Router's `useParams` and `useSearchParams` internally. If your application already has a router, keep it rather than nesting another one. This example shows the MVVM/router portion of the host:

```tsx
import { BrowserRouter } from 'react-router-dom';
import { MVVM } from '@cratis/arc.react.mvvm';

export const App = () => {
    return (
        <MVVM>
            <BrowserRouter>
                {/* Your application, including every withViewModel() component */}
            </BrowserRouter>
        </MVVM>
    );
};
```

## Understand what initialization shares

`<MVVM>` calls `Bindings.initialize()`, which registers `INavigation` and `IIdentityProvider` as singletons, the browser's native `localStorage` as `ILocalStorage`, and a messenger instance. These registrations use TSyringe's shared container. Adding another `<MVVM>` subtree does not create an isolated set of services; use a [messenger scope](./messaging.md#scoped-messaging) when you need a local communication boundary.

Initialization accesses browser storage, and the navigation service observes `document.body` when resolved. This setup needs a browser environment; do not treat it as a server-rendering provider.

MobX configuration is application-wide too. By default, `<MVVM>` sets `enforceActions` to `'never'`, allowing view model methods to change observable state without explicit MobX actions. It also defers reactions to a microtask so they do not run synchronously during React rendering. The optional `mobx` prop overrides these defaults; changing it affects more than the nearest view.

## Bind the first view

Follow [using a view model](./using-view-model.md) to render a counter and change its state from a button. `withViewModel()` creates a child dependency container for the view and makes the resolved view model observable. That is the boundary for view-specific dependencies such as dialogs and the nearest messenger; the shared registrations above are not recreated per view.

When a view owns subscriptions, give its view model a `detached()` method to release them on disposal. Ordinary unmount disposal is skipped with `<Arc development>`, so check the [view model lifecycle](./using-view-model.md#view-model-lifecycle) before relying on it.
