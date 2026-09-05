# Observable Query Diagnostics

Observable query diagnostics give you a live view into the query subsystem from React. They are useful when you need to build developer tools, inspect cache behavior, or show the current transport state in an internal dashboard.

The diagnostics service is exposed through `ArcContext` as `observableQueryDiagnostics`. It provides two ways to read the current state:

- `getSnapshot()` for a point-in-time read.
- `snapshots$` for a live stream of updates.

Diagnostics snapshots are only exposed through this API surface. Arc does not emit browser `window` events for diagnostics updates.

## What the snapshot contains

Each snapshot includes:

- transport configuration
- multiplexer connection state
- cache state and cache size
- ownership information for hooks that opted into tracking
- a health summary

This is intentionally diagnostic data, not application state. Use it for tooling, not for rendering core business screens.

## Watching snapshots from React

Subscribe to `snapshots$` when you want live updates.

```tsx
import { useContext, useEffect, useState } from 'react';
import { ArcContext } from '@cratis/arc.react';
import { ObservableQueryDiagnosticsSnapshot } from '@cratis/arc/queries';

export const QueryDiagnosticsPanel = () => {
    const arc = useContext(ArcContext);
    const [snapshot, setSnapshot] = useState<ObservableQueryDiagnosticsSnapshot | undefined>();

    useEffect(() => {
        const diagnostics = arc.observableQueryDiagnostics;
        if (!diagnostics) {
            return;
        }

        setSnapshot(diagnostics.getSnapshot());

        const subscription = diagnostics.snapshots$.subscribe(setSnapshot);
        return () => subscription.unsubscribe();
    }, [arc.observableQueryDiagnostics]);

    if (!snapshot) {
        return <p>No diagnostics available.</p>;
    }

    return (
        <dl>
            <dt>Connected</dt>
            <dd>{snapshot.health.allQueriesConnected ? 'Yes' : 'No'}</dd>
            <dt>Tracked queries</dt>
            <dd>{snapshot.cache.entryCount}</dd>
            <dt>Owners</dt>
            <dd>{Object.keys(snapshot.ownership.ownersByQueryKey).length}</dd>
        </dl>
    );
};
```

## Tracking ownership

The query hooks can optionally tag a cache entry with a human-readable owner label. This lets diagnostics show which component is responsible for each tracked query instance.

Use the optional `owner` argument on `useQuery()` or `useObservableQuery()` when you want that metadata to appear in the snapshot.

```tsx
const [result] = useObservableQuery(MyObservableQuery, args, sorting, true, 'OrdersPage');
```

If you omit the owner, the diagnostics stream still works. You just get the connection and cache information without ownership labels.

## Correlating with the backend health endpoint

The frontend diagnostics show one side of the picture — what the browser cache and transport layer know. When you need to verify that the server is actually pushing data to the right subscribers, Arc provides a matching backend endpoint at `/.cratis/queries/health`.

The two surfaces use the same query identifier format. Each entry in `snapshot.cache.entries` has a `queryName` field in the form `{TypeFullName}.{MethodName}`. The backend health endpoint exposes a `querySubscriptions` array where each entry's `queryName` field uses the exact same string. You can match them by name.

A typical investigation flow:

1. Pull `snapshot.cache.entries` from the frontend diagnostics.
2. Find the entry whose `queryName` matches the query you are investigating.
3. Check whether `subscribed` is `true` (the frontend has established a server subscription) and whether `hasResult` is `true` (at least one data frame has arrived).
4. Open `/.cratis/queries/health` or subscribe to it in a dev tool component, then look for the matching `queryName` in `querySubscriptions`.
5. Inspect `lastDataServedAt` and `lastPongReceivedAt` to confirm the backend is actively serving data.

If the frontend reports `subscribed: true` but `hasResult: false`, and the backend shows a matching subscriber with a stale `lastDataServedAt`, the problem is a data source that has not emitted since the subscription opened.

If the frontend reports `subscribed: false`, the subscription request never reached the backend — look for a connection problem in the multiplexer or SSE transport.

See [Query Health Endpoint](../../../backend/queries/query-health.md) for the full backend API and response shape.

## Related pages

- [Observable Queries](./observable-queries.md)
- [Observable Query Multiplexing](./observable-query-multiplexing.md)
- [Query Instance Caching](./query-instance-caching.md)
- [Backend: Query Health Endpoint](../../../backend/queries/query-health.md)
