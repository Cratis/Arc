# Configuration

Configure query behavior centrally through the `<Arc />` component instead of per query instance.

## Query-Related Arc Props

| Prop | Type | Default | Purpose |
| ---- | ---- | ------- | ------- |
| `microservice` | `string` | `undefined` | Routes query requests to a named microservice in shared-ingress environments. |
| `apiBasePath` | `string` | `''` | Prepends a base API path to query requests. |
| `httpHeadersCallback` | `() => HeadersInit` | `undefined` | Adds dynamic headers (auth, cookies, custom headers) to query requests. |
| `queryTransportMethod` | `QueryTransportMethod` | `ServerSentEvents` | Selects SSE or WebSocket transport for observable query connections. |
| `queryDirectMode` | `boolean` | `false` | Bypasses centralized hubs and connects observable queries directly per query URL. |
| `queryConnectionCount` | `number` | `1` | Number of observable query hub connection slots. |
| `observableQueryTransferMode` | `ObservableQueryTransferMode` | `Delta` | Controls how `useChangeStream()` processes incoming snapshots and deltas. |

## Example

```tsx
import { Arc } from '@cratis/arc.react';
import { ObservableQueryTransferMode } from '@cratis/arc';
import { QueryTransportMethod } from '@cratis/arc/queries';

export const App = () => (
    <Arc
        microservice="my-app"
        apiBasePath="/api"
        queryTransportMethod={QueryTransportMethod.ServerSentEvents}
        queryDirectMode={false}
        queryConnectionCount={1}
        observableQueryTransferMode={ObservableQueryTransferMode.Delta}
        httpHeadersCallback={() => ({ Authorization: `Bearer ${getToken()}` })}
    >
        <MyRoutes />
    </Arc>
);
```

## Observable Query Transport

Use `queryTransportMethod`, `queryDirectMode`, and `queryConnectionCount` to control observable query connection behavior.

For transport semantics, hub behavior, SSE limits, and pooling details, see [Observable Query Multiplexing](./observable-query-multiplexing.md).

## Change Stream Transfer Mode

`observableQueryTransferMode` sets the global default for `useChangeStream()` processing.

| Value | Behavior |
| ----- | -------- |
| `ObservableQueryTransferMode.Delta` | Uses server-provided `ChangeSet` when available and falls back to client-side diffing. |
| `ObservableQueryTransferMode.Full` | Treats every snapshot as a full batch of additions. |

For hook-level behavior, see [Change Stream](./change-stream.md).

## Related

- [Arc Component](../arc.md)
- [Core Query Configuration](../../core/queries/configuration.md)
- [Observable Queries](./observable-queries.md)
