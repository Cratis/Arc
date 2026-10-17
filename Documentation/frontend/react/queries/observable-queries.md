---
title: Observable Queries
description: Use observable queries in React for real-time updates, and choose between hub-multiplexed and direct per-query connections.
---

Observable queries provide real-time updates in React through `useObservableQuery()` and generated proxy helpers. Arc supports both centralized hub routing and direct per-query connections.

For backend implementation patterns, see [Controller-based Observable Queries](../../../backend/csharp/queries/controller-based/observable-queries.md), [Model-bound Observable Queries](../../../backend/csharp/queries/model-bound/observable-queries.md), and [Observable Query Hub](../../../backend/csharp/queries/observable-query-demultiplexer.md).

## Observable Query Transport

By default, observable queries connect through centralized hub endpoints. Two props on the `<Arc>` component control this behavior.

### `queryTransportMethod`

Selects the transport protocol used for the hub connection.

| Value | Description |
| ------- | ------------- |
| `QueryTransportMethod.ServerSentEvents` | Shared SSE hub at `/.cratis/queries/sse`, with subscribe/unsubscribe POSTs (React default). |
| `QueryTransportMethod.WebSocket` | WebSocket hub transport. |

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

### `queryDirectMode`

Controls whether observable queries connect directly to each query's own URL or route through the centralized hub.

- When `false` (default): queries are routed through the centralized hub endpoint (`/.cratis/queries/sse` or `/.cratis/queries/ws` depending on `queryTransportMethod`).
- When `true`: each observable query opens its own connection directly to the per-query URL, bypassing the hub entirely. This is useful during local development or when connecting to services that do not expose the centralized hub.

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

Non-Suspense observable hooks select a cache entry by query, arguments, page, page size and sort order. Changing any of these selects a matching subscription or starts one with the new settings. Two consumers on different pages do not share a subscription; matching consumers do.

The result immediately shows the matching entry's cached result, if one exists. Otherwise it resets to the query's default value with `isReady: false` and `isPerforming: true` until a result arrives. Rows and failure status from the previous entry are not shown as the new result, and later pushes to the previous subscription do not update the component.

A retained subscription stays live during the cache retention window. Returning to it shows its latest cached result without another subscribe. See [query instance caching](./query-instance-caching.md) for retention settings and [paging](./paging.md) for the paging and sorting controls.

## Related Pages

- [Observable Query Multiplexing](./observable-query-multiplexing.md) for connection pooling, SSE limits, and protocol behavior.
- [Change Stream](./change-stream.md) for item-level delta tracking on observable collections.
- [Use Observable Queries with cURL](../../../backend/csharp/queries/using-observable-queries-with-curl.md) for backend endpoint debugging workflows.
