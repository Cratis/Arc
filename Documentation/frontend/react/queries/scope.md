---
title: "React query scope: track loading in a section"
description: "Use Cratis Arc QueryScope to show one loading indicator for a React section, track request completion, and handle nested scopes and ViewModel state."
---

`QueryScope` lets several queries in one React section drive a single loading indicator. Wrap the section in a scope and pass `setIsPerforming` to update React state when its own query work starts or finishes. `<Arc>` does not mount a query scope for you.

## Using query scope in React

This component accepts a query-consuming panel as its child. Its status message stays visible while the scope has active query work:

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

Non-Suspense ordinary hooks track the request. Observable hooks track the wait for a new subscription's first result, not the lifetime of the connection, so a live feed does not remain permanently loading. Joining an already active subscription does not start a separate scope operation for each consumer.

Suspense hooks use separate resources and do not send these scope notifications. Use a [query boundary fallback](./suspense-queries.md) for their loading UI.

## When an ordinary query stops performing

Each execution of `useQuery` or `useQueryWithPaging` completes its scope operation exactly once: when the request settles, fails, or is retired because the component unmounted or started newer work. Changing arguments, paging or sorting can start that newer work, as can an explicit `perform` call.

Matching consumers can share a query instance. If a later request replaces an earlier one, the earlier consumer remains performing while it waits to adopt the replacement's result or failure. Retiring an execution does not abort a request another mounted consumer still needs.

If the consumer that started the replacement unmounts, the waiting consumer still receives the outcome. If the waiter unmounts instead, the request continues for the consumer that started it. The departed consumer's own scope operation ends in either case. See [query instance caching](./query-instance-caching.md) for the distinction between sharing an instance and broadcasting results.

A retained observable subscription has different ownership: if it is still waiting for its first result when its last consumer leaves, its scope remains performing until that result arrives or cache eviction tears down the subscription.

## Hierarchical scopes

Queries notify the nearest scope. An outer scope's `isPerforming` getter includes child activity, but a child's start or completion does not call the parent's `setIsPerforming` callback. A toolbar driven only by the parent's callback therefore does not update for child-only activity.

For one reactive indicator, keep the contributing queries under one scope or explicitly combine each section's callback state. Reading the parent's getter can tell you the current state; it does not subscribe your component to changes.

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

`withViewModel()` registers `IQueryScope` in the ViewModel child container, so an injected scope can supply its current `isPerforming` value. Making the ViewModel observable does not make the scope's mutable internals observable. For a reactive display, use `setIsPerforming` to update an observable property rather than relying on a computed getter alone.

For a single query's loading and failure UI, continue with [query usage](./usage.mdx). To track writes in a section, use [command scopes](../commands/scope.md).
