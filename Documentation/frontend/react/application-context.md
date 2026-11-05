---
title: Cratis Arc React application context
description: Read the focused slice, current screen and open dialogs from anywhere under Arc to give shared UI the context of what the user is viewing.
---

Cratis Arc's `useApplicationContext()` lets shared UI read what the user is viewing, even when that UI sits outside the current feature. A feedback button in the application header can attach the focused slice and screen name to a report without knowing which page is mounted.

In this example, `send` is your application's feedback callback:

```tsx
import { useApplicationContext } from '@cratis/arc.react/applicationContext';

export const FeedbackButton = () => {
    const { location, screen, dialogs } = useApplicationContext();

    return <button onClick={() => send({ location: location.toString(), screen: screen.name })}>Give feedback</button>;
};
```

`<Arc>` provides the application context without extra setup. Use [slice context](./slice-context.md) when you need the location of the component calling the hook; use application context when you need the user's current focus.

## The snapshot

`useApplicationContext()` returns an immutable `ApplicationContextSnapshot`. It keeps the same identity until something in it changes.

| Member | Meaning |
| --- | --- |
| `location` | The `SliceLocation` of the focused scope: the page, or the top-most open dialog. |
| `activeSlices` | Every distinct slice location registered by mounted commands and queries in the focused scope, before they are folded into `location`. It updates when they unmount. |
| `screen` | `{ id, name, path }` as contributed by the host or router. Empty strings when nobody contributes one. |
| `dialogs` | The stack of open dialogs, bottom first, each `{ id, title?, location }`. See [dialogs](./dialogs.md#dialogs-and-slice-scope). |
| `contributions` | A `Record<string, unknown>` of host values. |

Arc combines the deepest active scopes in the focused layer into their closest common location. On a page, sibling slices `Billing/Invoices/Pay` and `Billing/Invoices/Listing` resolve to `Billing/Invoices`, while `activeSlices` preserves the distinct command and query registrations. Opening a dialog moves focus to that dialog; closing it restores the previous dialog or the page.

## Using it outside React

A host integration may need the same context without calling a React hook. Create an `ApplicationContext` and give it to `<Arc>`, then use `subscribe` for notifications and `getSnapshot` to read the current value:

```tsx
import { Arc } from '@cratis/arc.react';
import { ApplicationContext } from '@cratis/arc.react/applicationContext';

export const applicationContext = new ApplicationContext();

const unsubscribe = applicationContext.subscribe(() => {
    console.log(applicationContext.getSnapshot().location.toString());
});

export const Root = () => (
    <Arc applicationContext={applicationContext}>
        <App />
    </Arc>
);
```

Without the prop, `<Arc>` creates its own, available as `useApplicationContextStore()` and as `applicationContext` on `ArcContext`.

## Contributing the screen and values

Arc does not track routes, so it cannot supply your screen's name or path itself. Contribute those values from your router or host with `ApplicationContextContributor`, or call `useContributeApplicationContext()` from a component that has them:

```tsx
import { ApplicationContextContributor, useContributeApplicationContext } from '@cratis/arc.react/applicationContext';

// As a component
<ApplicationContextContributor screen={{ id: 'orders', name: 'Orders', path: '/orders' }} values={{ version: '1.4.0' }}>
    <OrdersPage />
</ApplicationContextContributor>

// Or as a hook
useContributeApplicationContext({ screen: { name: 'Orders' }, values: { version: '1.4.0' } });
```

The contribution updates while the component is mounted and is removed on unmount. You can pass a new object on each render. Later contributions win for each supplied screen field and value key; removing one reveals the earlier value again.

## Marking slices in the DOM

With `sliceMarkers` on, `<Slice>` and slice folder boundaries render a `display: contents` element carrying the location:

```tsx
<Arc sliceMarkers>
    <App />
</Arc>
```

```html
<div style="display: contents" data-cratis-module="Orders" data-cratis-feature="Editing" data-cratis-slice="ChangeOrder">...</div>
```

An element picker can find an element's nearest marked ancestor with `element.closest('[data-cratis-module]')` and read those three attributes. Unknown parts are empty strings. Markers are off by default, so no extra element is rendered unless you enable them.

A dialog renderer that uses a portal can mark its own header or container with `sliceMarkerAttributes(location)` or `<SliceMarker scope={...}>`. See [dialogs and slice scope](./dialogs.md#dialogs-and-slice-scope) for how the dialog keeps the location of the feature that opened it.
