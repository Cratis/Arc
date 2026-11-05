---
title: "Observable React queries: keep views up to date"
description: "Keep React views updated with Cratis Arc observable queries, choose SSE, WebSocket or polling, and handle argument changes and server failures."
---

An observable query keeps a React view updated as the backend produces new results. Call the generated proxy's `use()` method, or the underlying `useObservableQuery()` hook, to subscribe rather than manually repeat an ordinary query.

Start with [query usage](./usage.mdx) for the component call and result handling. The backend must expose an observable query: use the [controller-based](../../../backend/csharp/queries/controller-based/observable-queries.md) or [model-bound](../../../backend/csharp/queries/model-bound/observable-queries.md) pattern that fits your application. Shared connections also need the [observable query hub](../../../backend/csharp/queries/observable-query-demultiplexer.md).

## Observable Query Transport

React's `<Arc>` provider defaults to Server-Sent Events (SSE) through a shared hub. SSE carries results from the server to the browser; the client sends subscription control requests separately. Choose a transport with `queryTransportMethod`, then use `queryDirectMode` if streaming queries need to connect to their own routes instead of the hub.

### `queryTransportMethod`

Selects the transport protocol used for observable queries.

| Value | Description |
| ------- | ------------- |
| `QueryTransportMethod.ServerSentEvents` | SSE, using `/.cratis/queries/sse` and subscribe/unsubscribe POSTs in hub mode (React default). |
| `QueryTransportMethod.WebSocket` | WebSocket, using `/.cratis/queries/ws` in hub mode. |
| `QueryTransportMethod.Polling` | Repeated HTTP GET snapshots from each query's own URL, without a hub. |

```tsx
import { Arc } from '@cratis/arc.react';
import { QueryTransportMethod } from '@cratis/arc/queries';

export const App = () => (
    <Arc
        microservice="my-app"
        queryTransportMethod={QueryTransportMethod.ServerSentEvents}
    >
        <MyRoutes />
    </Arc>
);
```

For polling, pass `queryTransportMethod={QueryTransportMethod.Polling}` to `<Arc>` and set `Globals.queryPollingOptions` before rendering. `queryDirectMode` has no effect on polling. See [polling configuration, backoff and limits](../../core/queries/configuration.md#polling-transport).

### `queryDirectMode`

For SSE and WebSocket, leave `queryDirectMode` false to share hub connections across subscriptions. The hub routes each result back to the query that requested it.

Set it to true to connect each subscription to its query's own URL. Use this to inspect a per-query endpoint or connect to a host without hub endpoints. Direct mode keeps the selected transport: with React's default settings, the following example uses direct SSE, not WebSocket.

```tsx
import { Arc } from '@cratis/arc.react';

export const App = () => (
    <Arc
        microservice="my-app"
        queryDirectMode={true}
    >
        <MyRoutes />
    </Arc>
);
```

| Prop | Type | Default | Description |
| ------ | ------ | --------- | ------------- |
| `queryTransportMethod` | `QueryTransportMethod` | `ServerSentEvents` | Transport used for observable query connections. |
| `queryDirectMode` | `boolean` | `false` | When `true`, bypasses the hub and connects directly per query. |

## Changing arguments, paging and sorting

When a user changes a filter or page, non-Suspense observable hooks select a cache entry for the new query, arguments, page, page size and sort order. Matching consumers share a subscription; consumers on different pages do not.

The component immediately shows the new entry's cached result if one exists. Otherwise it shows the query's default value with `isReady: false` and `isPerforming: true` until a result arrives. The previous entry's rows and failure status are not presented as the new result, and its later updates no longer reach this component.

Subscriptions remain live during the cache retention window, which defaults to 30 seconds after the last consumer releases an entry. Returning within that window reuses its latest result and subscription. See [query instance caching](./query-instance-caching.md) to configure retention and [paging](./paging.md) to add navigation controls.

## Handle server-side observation failures

A failed observation is not an empty collection. For both SSE and WebSocket hub connections, a server `Error` message produces a result with `isReady: true`, `isSuccess: false`, `hasExceptions: true`, and the server error in `exceptionMessages`.

Check `result.hasExceptions` before displaying an empty-state message. Receiving an error does not itself restart the failed backend observation; recovery requires a new observation. For MongoDB-backed queries, follow [observation errors and recovery limits](../../../backend/csharp/mongodb/observing-collections.md#errors-and-recovery-limits) to distinguish failures from normal watch cancellation.

## Related Pages

- [Observable Query Multiplexing](./observable-query-multiplexing.md) for connection pooling, SSE limits, and protocol behavior.
- [Change Stream](./change-stream.md) for item-level delta tracking on observable collections.
- [Use Observable Queries with cURL](../../../backend/csharp/queries/using-observable-queries-with-curl.md) for backend endpoint debugging workflows.
