---
title: Application context
description: Read what the user is looking at from anywhere under Arc with useApplicationContext() - the focused slice location, active slices, screen, open dialogs and host contributions - and mark slices in the DOM.
---

`useSliceContext()` answers for the component that calls it. A feedback button in the top bar of the shell sits outside every slice scope, so it cannot know which slice the page shows. The application context answers the app-wide question: what is the user looking at right now?

```tsx
import { useApplicationContext } from '@cratis/arc.react/applicationContext';

export const FeedbackButton = () => {
    const { location, screen, dialogs } = useApplicationContext();

    return <button onClick={() => send({ location: location.toString(), screen: screen.name })}>Give feedback</button>;
};
```

`<Arc>` provides it, so there is no extra setup. See [slice context](./slice-context.md) for how a single scope resolves its location.

## The snapshot

`useApplicationContext()` returns an immutable `ApplicationContextSnapshot`. It keeps the same identity until something in it changes.

| Member | Meaning |
| --- | --- |
| `location` | The `SliceLocation` of the focused scope: the page, or the top-most open dialog. |
| `activeSlices` | Every distinct slice location registered by mounted commands and queries in the focused scope, before they are folded into `location`. It updates when they unmount. |
| `screen` | `{ id, name, path }` as contributed by the host or router. Empty strings when nobody contributes one. |
| `dialogs` | The stack of open dialogs, bottom first, each `{ id, title?, location }`. See [dialogs](./dialogs.md#dialogs-and-slice-scope). |
| `contributions` | A `Record<string, unknown>` of host values. |

The page location is the common location of the slice scopes mounted in the page: `<Slice>` boundaries and slice folders. Sibling slices `Billing/Invoices/Pay` and `Billing/Invoices/Listing` give `Billing/Invoices`; `activeSlices` still lists both.

## Using it outside React

The store behind the hook has `subscribe` and `getSnapshot`, so it works with `useSyncExternalStore` and with code that does not render, such as a picker overlay. Create the store first and give it to `<Arc>`:

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

Arc does not track routes. A host or router contributes the screen, and any other values, without Arc depending on a router:

```tsx
import { ApplicationContextContributor, useContributeApplicationContext } from '@cratis/arc.react/applicationContext';

// As a component
<ApplicationContextContributor screen={{ id: 'orders', name: 'Orders', path: '/orders' }} values={{ version: '1.4.0' }}>
    <OrdersPage />
</ApplicationContextContributor>

// Or as a hook
useContributeApplicationContext({ screen: { name: 'Orders' }, values: { version: '1.4.0' } });
```

The contribution lasts while the component is mounted, and may be a new object on every render. Parts of `screen` that are given override earlier contributions, and `values` are merged by key; later contributors win.

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

An element picker finds the slice of any element with `element.closest('[data-cratis-module]')` and reads the three attributes from that element. Parts that are unknown are empty strings. With `sliceMarkers` off, which is the default, nothing extra is rendered. A dialog whose renderer portals its content out of the tree can mark its own chrome with `sliceMarkerAttributes(location)` or `<SliceMarker scope={...}>`.
