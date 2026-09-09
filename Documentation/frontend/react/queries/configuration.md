# Configuration

Configure query behavior centrally through the `<Arc />` component instead of per query instance.

## Query-Related Arc Props

| Prop | Type | Default | Purpose |
| ---- | ---- | ------- | ------- |
| `microservice` | `string` | `undefined` | Routes query requests to a named microservice in shared-ingress environments. |
| `apiBasePath` | `string` | `''` | Prepends a base API path to query requests. |
| `httpHeadersCallback` | `() => HeadersInit` | `undefined` | Adds headers to ordinary fetches and SSE control POSTs, not native stream handshakes. |
| `queryTransportMethod` | `QueryTransportMethod` | `ServerSentEvents` | Selects SSE or WebSocket transport for observable query connections. |
| `queryDirectMode` | `boolean` | `false` | Bypasses centralized hubs and connects observable queries directly per query URL. |
| `queryConnectionCount` | `number` | `1` | Number of observable query hub connection slots. |
| `observableQueryTransferMode` | `ObservableQueryTransferMode` | `Delta` | Selects shared-hub server full/delta emissions and `useChangeStream()` fallback processing. |
| `queryCacheRetentionMs` | `number` | `30000` | How long to keep cached query data alive after the last subscriber unmounts. |
| `eventSourceFactory` | `(url: string) => EventSource` | `undefined` | Custom factory for creating the `EventSource` instances used by SSE observable query connections. Falls back to the global `EventSource` constructor when not set. |

## Example

```tsx
import { Arc } from '@cratis/arc.react';
import { ObservableQueryTransferMode } from '@cratis/arc';
import { QueryTransportMethod } from '@cratis/arc/queries';

export const App = () => (
    <Arc
        microservice="my-app"
        queryTransportMethod={QueryTransportMethod.ServerSentEvents}
        queryDirectMode={false}
        queryConnectionCount={1}
        observableQueryTransferMode={ObservableQueryTransferMode.Delta}
    >
        <MyRoutes />
    </Arc>
);
```

The example's `MyRoutes` is your application's route component. For bearer authentication, configure the [fetch/stream credential paths](../arc.md#http-headers-callback) separately; setting callback headers does not authenticate a native EventSource or WebSocket handshake.

For ordinary query HTTP method selection (`Get`, `Query`, `Auto`), see [using the HTTP QUERY method](../../../backend/queries/using-the-http-query-method.md). It is separate from observable transport selection.

## Query Cache Retention

When the last consumer of a `useObservableQuery` cache entry releases it — for example, when the user navigates away — Arc schedules cleanup after `queryCacheRetentionMs` milliseconds (default: 30 seconds). Until then, cached data and the active server subscription remain available. Another consumer acquiring the entry cancels its pending cleanup.

This has two practical effects:

- **Instant navigation**: If the user returns to the same page within the retention window, cached data renders immediately instead of showing a loading state while the subscription re-establishes.
- **Subscription cleanup**: After the window expires without another consumer, the cache entry is evicted and its subscription is torn down. This does not necessarily close a shared hub connection that serves other subscriptions.

```tsx
<Arc queryCacheRetentionMs={60_000}>
    {/* data survives for 60 s after the last subscriber unmounts */}
</Arc>
```

Set the value to `0` to schedule zero-delay cleanup after the last consumer releases the entry. Cleanup is asynchronous, not synchronous eviction during unmount:

```tsx
<Arc queryCacheRetentionMs={0}>
    {/* scheduled zero-delay cleanup after the last consumer releases the entry */}
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

## Custom EventSource Factory

By default, SSE observable query connections use the global `EventSource` constructor. Override it for an environment-specific compatible implementation or browser credentialed cross-origin SSE. The factory receives only the connection URL; callback headers are not passed to it.

Set `eventSourceFactory` to substitute your own SSE client without changing anything else about how observable queries work:

```tsx
import { Arc } from '@cratis/arc.react';
export const App = () => (
    <Arc eventSourceFactory={(url) => new EventSource(url, { withCredentials: true })}>
        <main>Your query components</main>
    </Arc>
);
```

The returned value must satisfy the `EventSource` interface expected by Arc. This applies to direct and hub SSE connections, not WebSockets. The server must allow the credentialed origin through CORS. `withCredentials` affects the streaming handshake only: SSE control POSTs are separate fetch calls and currently do not set `credentials: 'include'`. Cross-origin cookie-only hub authentication therefore needs additional, independently verified support; the factory alone does not solve it.

The default can also be set globally without the React component:

```typescript
import { Globals } from '@cratis/arc';

Globals.eventSourceFactory = (url) => new EventSource(url, { withCredentials: true });
```

## Change Stream Transfer Mode

`observableQueryTransferMode` is global and is forwarded in shared-hub subscription requests, controlling server emissions as well as `useChangeStream()` fallback processing. Configure it before subscriptions are established; nested `<Arc>` providers do not isolate modes.

| Value | Shared-hub server emissions | `useChangeStream()` fallback without a server changeset |
| ----- | -------- | -------- |
| `ObservableQueryTransferMode.Delta` | Initial full snapshot, then delta-only collection updates | Compares snapshots locally |
| `ObservableQueryTransferMode.Full` | Full snapshots, without server changesets | Treats the processed snapshot as additions |

A received server changeset takes precedence over local fallback in either mode. Prefer ordinary observable `.use()` for delta feeds: it reconstructs collections, whereas [observable Suspense currently does not](./suspense-queries.md#observable-collections-and-delta-only-updates). For hook-level behavior, see [Change Stream](./change-stream.md).

## Related

- [Arc Component](../arc.md)
- [Core Query Configuration](../../core/queries/configuration.md)
- [Observable Queries](./observable-queries.md)
