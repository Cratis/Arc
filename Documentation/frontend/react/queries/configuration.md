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
| `queryCacheRetentionMs` | `number` | `30000` | How long to keep cached query data alive after the last subscriber unmounts. |

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

## Query Cache Retention

When a component that uses `useObservableQuery` unmounts — for example, when the user navigates to a different page — Arc holds the cached query data and the active server subscription alive for `queryCacheRetentionMs` milliseconds (default: 30 seconds) before evicting them.

This has two practical effects:

- **Instant navigation**: If the user returns to the same page within the retention window, cached data renders immediately instead of showing a loading state while the subscription re-establishes.
- **Bounded memory**: After the window expires, the cache entry and the underlying connection are released automatically, so long-lived single-page applications do not accumulate stale subscriptions.

```tsx
<Arc queryCacheRetentionMs={60_000}>
    {/* data survives for 60 s after the last subscriber unmounts */}
</Arc>
```

Set the value to `0` to restore the previous behavior of evicting the subscription as soon as the last subscriber unmounts:

```tsx
<Arc queryCacheRetentionMs={0}>
    {/* immediate eviction — original behavior */}
</Arc>
```

The default can also be adjusted globally without the React component:

```typescript
import { Globals } from '@cratis/arc';

Globals.queryCacheRetentionMs = 60_000;
```

> **Note:** The retention window applies per cache entry, not globally. Each query type and argument combination has its own independent timer.

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
