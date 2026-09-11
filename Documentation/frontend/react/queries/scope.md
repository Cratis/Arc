---
title: Query scope
description: Show local query activity using explicit state callbacks, without assuming nested scopes propagate notifications.
---

A section can contain several queries but only need one loading indicator. Wrap that section in `QueryScope` and use its `setIsPerforming` callback to drive React state. `<Arc>` does not mount a query scope for you.

## Using query scope in React

This composition fragment accepts a query-consuming panel as its child:

```tsx
import { useState, type ReactElement } from 'react';
import { QueryScope } from '@cratis/arc.react/queries';

export function QueryPanel({ children }: { children: ReactElement }) {
    const [isPerforming, setIsPerforming] = useState(false);
    return (
        <QueryScope setIsPerforming={setIsPerforming}>
            <p role="status">{isPerforming ? 'Loading…' : ''}</p>
            {children}
        </QueryScope>
    );
}
```

The non-Suspense query hooks notify while starting requests/subscriptions. Ordinary queries track the request; observable queries track the wait for the first result, not the lifetime of the connection. A long-lived subscription is not permanently “loading.” Current Suspense hooks use separate resources and do not call QueryScope's performing notifications; use their loading boundary instead.

## Hierarchical scopes

Queries notify the nearest scope. An outer scope's `isPerforming` **getter** includes child scopes, but a child's start/completion notification does not call the parent's `setIsPerforming` callback. Consequently, a toolbar driven by the parent's callback does not automatically update for child-only activity.

Keep queries under one scope when you need one reactive indicator, or explicitly combine each section's callback state. Parent notification propagation is a runtime follow-up candidate; getter aggregation alone is not reactive aggregation.

## Query scope API

| Member | Contract |
| --- | --- |
| `isPerforming` | Imperative read of own or child activity |
| `parent` | Parent scope, if nested |
| `addChildScope(scope)` | Registers a child for getter aggregation |
| `notifyPerformingStarted()` | Increments own activity count; notifies own callback on transition to active |
| `notifyPerformingCompleted()` | Decrements own count; notifies own callback when it reaches zero |
| `useQueryScope()` | Returns the nearest scope; does not subscribe to changes in its internals |

## Using query scope in view models

MVVM can inject `IQueryScope`, and a view-model getter can read `scope.isPerforming`. However, `withViewModel` making the view model observable does not make the injected scope's mutable internals observable. For a reactive display, bridge explicit scope callbacks to state rather than relying on a computed getter alone.

Continue with [query usage](./usage.md) or [command scopes](../commands/scope.md).
