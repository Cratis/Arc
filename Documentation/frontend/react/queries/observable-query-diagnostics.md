---
title: Observable Query Diagnostics
description: Inspect the observable-query cache and transport from React with getSnapshot() and snapshots$, and correlate it with backend query health.
---

Cratis Arc's observable query diagnostics expose browser-side cache, transport, and ownership snapshots to React developer tools. Use them to investigate local subscription state, then correlate with backend query health when you need server-side evidence.

The diagnostics service is exposed through `ArcContext` as `observableQueryDiagnostics`. It provides two ways to read the current state:

- `getSnapshot()` creates a point-in-time snapshot, publishes it to current `snapshots$` subscribers, and returns it.
- `snapshots$` receives those publications and snapshots published by ownership tracking changes while the stream has subscribers. It does not replay a snapshot on subscription or publish automatically for every cache result or transport change.

Diagnostics snapshots are only exposed through this API surface. Arc does not emit browser `window` events for diagnostics updates.

## What the snapshot contains

Each snapshot includes:

- transport configuration
- multiplexer connection state
- cache state and cache size
- ownership labels recorded by query hooks
- a health summary

`health.allQueriesConnected` combines at least one connected shared-hub slot with `cache.healthy`. The cache is healthy when every entry with active consumers has a locally registered subscription teardown. `health.disconnectedQueryCount` counts disconnected hub slots, not disconnected query entries. Neither field confirms delivery to every query; direct connections and polling are not represented by the shared multiplexer, so interpret these fields alongside `transport` and the individual cache entries.

This is intentionally diagnostic data, not application state. Use it for tooling, not for rendering core business screens.

## Watching snapshots from React

Read an initial snapshot and subscribe to `snapshots$` for subsequent publications. The panel below updates when a snapshot is published; it does not poll. For a fresh reading after a cache or connection change, call `getSnapshot()` explicitly or sample it on a timer whose cleanup clears the timer.

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

The query hooks can tag a cache entry with a human-readable owner label. Ownership is stored as one label per cache key, not a list of every component sharing the entry: another tracking call for the same key overwrites the label, and ending tracking removes it.

Use the optional `owner` argument on `useQuery()` or `useObservableQuery()` when you want that metadata to appear in the snapshot.

```tsx
const [result] = useObservableQuery(MyObservableQuery, args, sorting, true, 'OrdersPage');
```

If you omit the owner, the hooks track an empty-string label. The ownership maps still contain that key, so the panel's `Owners` count measures tracked query keys, not distinct components or non-empty labels.

## Correlating with the backend health endpoint

The frontend diagnostics show one side of the picture — what the browser cache and transport layer know. When you need to verify that the server is actually pushing data to the right subscribers, Arc provides a matching backend endpoint at `/.cratis/queries/health`.

The backend health feed requires authentication outside Development by default. Use a signed-in diagnostic client, including for named subscriptions over WebSocket or SSE, and ensure the caller has any roles configured through `ArcOptions.QueryHealth.Roles`. This does not affect the local frontend diagnostics API.

The two surfaces use the same query identifier format. Each entry in `snapshot.cache.entries` has a `queryName` field in the form `{TypeFullName}.{MethodName}`. The backend health endpoint exposes a `querySubscriptions` array where each entry's `queryName` field uses the exact same string. You can match them by name.

A typical investigation flow:

1. Pull `snapshot.cache.entries` from the frontend diagnostics.
2. Find the entry whose `queryName` matches the query you are investigating.
3. Check whether `subscribed` is `true` (the frontend registered the subscription teardown; this is local state, not a server acknowledgement) and whether `hasResult` is `true` (a result is cached, which can include an error or validation result rather than successful business data).
4. Open `/.cratis/queries/health` or subscribe to it in a dev tool component, then look for the matching `queryName` in `querySubscriptions`.
5. Inspect the matching subscriber's `lastDataServedAt` for recorded data sends and `lastPongReceivedAt` for heartbeat responses. A pong is not evidence that query data changed, and neither timestamp proves the browser received or rendered a result.

If the frontend reports `subscribed: true` but `hasResult: false`, compare a fresh frontend snapshot with the matching backend subscriber. A missing or old `lastDataServedAt` can guide investigation of the data source, but does not by itself identify the cause; an unchanged query may legitimately emit nothing new.

If the backend shows no matching subscriber, check the transport, authentication, query name, and timing of the two snapshots. Local teardown registration does not prove that the backend accepted the subscription.

If the frontend reports `subscribed: false`, no subscription teardown is currently registered for that entry. A disabled query can have this state, as can an entry whose subscription was torn down during a connection reset. Check the enable condition and `subscriberCount` before treating it as a connection failure. Releasing the last consumer normally retains the entry and its subscription until cache retention expires, then evicts the entry.

See [Query Health Endpoint](../../../backend/csharp/queries/query-health.md) for the full backend API and response shape.

## Related pages

- [Observable Queries](./observable-queries.md)
- [Observable Query Multiplexing](./observable-query-multiplexing.md)
- [Query Instance Caching](./query-instance-caching.md)
- [Backend: Query Health Endpoint](../../../backend/csharp/queries/query-health.md)
