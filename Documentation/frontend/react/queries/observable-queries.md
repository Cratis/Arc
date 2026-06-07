# Observable Queries

Observable queries provide real-time updates in React through `useObservableQuery()` and generated proxy helpers. Arc supports both centralized hub routing and direct per-query connections.

For backend implementation patterns, see [Controller-based Observable Queries](../../../backend/queries/controller-based/observable-queries.md), [Model-bound Observable Queries](../../../backend/queries/model-bound/observable-queries.md), and [Observable Query Hub](../../../backend/queries/observable-query-demultiplexer.md).

## Observable Query Transport

By default, observable queries connect through centralized hub endpoints. Two props on the `<Arc>` component control this behavior.

### `queryTransportMethod`

Selects the transport protocol used for the hub connection.

| Value | Description |
|-------|-------------|
| `QueryTransportMethod.ServerSentEvents` | SSE hub — one `EventSource` per query, routed through `/.cratis/queries/sse` (default). |
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
|------|------|---------|-------------|
| `queryTransportMethod` | `QueryTransportMethod` | `ServerSentEvents` | Transport used for observable query connections. |
| `queryDirectMode` | `boolean` | `false` | When `true`, bypasses the hub and connects directly per query. |

## Related Pages

- [Observable Query Multiplexing](./observable-query-multiplexing.md) for connection pooling, SSE limits, and protocol behavior.
- [Change Stream](./change-stream.md) for item-level delta tracking on observable collections.
- [Use Observable Queries with cURL](../../../backend/queries/using-observable-queries-with-curl.md) for backend endpoint debugging workflows.
