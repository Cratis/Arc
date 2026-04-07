# Observable Query Multiplexing

Observable queries in Arc connect through a centralized hub rather than opening one WebSocket per query. This page explains how the multiplexing works from the frontend perspective and how to configure it.

For the server-side protocol reference — endpoints, message types, keep-alive configuration — see [Observable Query Hub](../../backend/queries/observable-query-hub.md).

## How It Works

When `queryDirectMode` is `false` (the default), every observable query subscription is routed through one of two fixed hub endpoints instead of connecting to a per-query URL:

| Transport | Hub Endpoint | Notes |
|-----------|-------------|-------|
| Server-Sent Events | `/.cratis/queries/sse` | One `EventSource` per query, multiplexed by query name via query-string |
| WebSocket | `/.cratis/queries/ws` | Single connection carrying N subscriptions via a typed protocol |

The frontend `ObservableQueryFor.subscribe()` constructs the correct URL from the query's fully qualified name and its arguments, then establishes a connection to the hub. The server resolves the query by name, runs it through the query pipeline (including authorization), and streams results back.

### SSE hub connection

When transport is SSE, `subscribe()` calls the hub as:

```http
GET /.cratis/queries/sse?query=<fullyQualifiedQueryName>&<queryArgs>
```

The `EventSource` re-establishes the connection automatically if the server becomes temporarily unavailable.

### WebSocket hub connection

When transport is WebSocket, `subscribe()` sends a typed `subscribe` message over a shared WebSocket connection. Refer to the [protocol reference](../../backend/queries/observable-query-hub.md#protocol) for the full message format.

## Configuring Transport and Mode

All multiplexing configuration flows through the `<Arc>` component.

### Selecting the transport method

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

| Value | Description |
|-------|-------------|
| `QueryTransportMethod.ServerSentEvents` | SSE hub — one `EventSource` per query (default). |
| `QueryTransportMethod.WebSocket` | WebSocket hub — single shared connection per application. |

### Bypassing the hub (direct mode)

Set `queryDirectMode={true}` to connect each observable query directly to its own per-query WebSocket URL, bypassing the hub entirely.

```tsx
export const App = () => (
    <Arc
        microservice="my-app"
        queryDirectMode={true}
    >
        <MyRoutes />
    </Arc>
);
```

Use direct mode when:

- Connecting to backend services that do not expose the centralized hub endpoints.
- Debugging individual query connections in isolation.

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `queryDirectMode` | `boolean` | `false` | When `true`, bypasses the hub and connects directly to each query's own URL. |

### Configuring the number of hub connections

By default a single centralized hub connection handles all subscriptions. Use `queryConnectionCount` to distribute subscriptions across multiple connections:

```tsx
<Arc
    microservice="my-app"
    queryConnectionCount={2}
/>
```

The `ObservableQueryConnectionPool` picks the least-loaded slot round-robin when a new subscription is created. Increasing the connection count can improve throughput for applications with many concurrent observable queries.

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `queryConnectionCount` | `number` | `1` | Number of hub connection slots maintained for observable queries. |

#### SSE connection limit (HTTP/1.1)

HTTP/1.1 browsers enforce a hard limit of **six simultaneous connections per origin**. Each SSE `EventSource` occupies one of those slots for as long as the page is open because the connection is kept alive indefinitely to receive server pushes. If all six slots are taken by `EventSource` connections, subscribe and unsubscribe POST requests cannot get a connection and queue indefinitely — queries appear to hang and produce no data.

Arc automatically caps the number of SSE hub connections at **4** regardless of what `queryConnectionCount` is set to. This leaves two slots free for the subscribe/unsubscribe POST calls and for ordinary `fetch` requests. A `console.warn` is emitted at startup when the configured count exceeds this limit.

```tsx
// This is capped at 4 automatically when using SSE — a warning is logged.
<Arc
    microservice="my-app"
    queryTransportMethod={QueryTransportMethod.ServerSentEvents}
    queryConnectionCount={10}
/>
```

The cap only applies to SSE. WebSocket connections do not consume HTTP/1.1 connection slots after the initial upgrade handshake, so `queryConnectionCount` is respected in full when using WebSocket transport.

> **Enable HTTP/2 to remove the limit.** HTTP/2 multiplexes all requests over a single TCP connection, making the per-origin slot limit irrelevant. When your server supports HTTP/2, `queryConnectionCount` may be set to any value without risk of connection starvation.
>
> References:
> - [HTTP/1.x connection management — MDN Web Docs](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Connection_management_in_HTTP_1.x)
> - [Server-Sent Events — HTML Living Standard](https://html.spec.whatwg.org/multipage/server-sent-events.html)

## Props Reference

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `queryTransportMethod` | `QueryTransportMethod` | `ServerSentEvents` | Transport used for hub connections. |
| `queryDirectMode` | `boolean` | `false` | When `true`, bypasses the hub entirely. |
| `queryConnectionCount` | `number` | `1` | Number of concurrent hub connections to maintain. |

## See also

- [Observable Query Hub](../../backend/queries/observable-query-hub.md) — Protocol reference, authorization semantics, and keep-alive configuration on the backend.
- [Query Instance Caching](./query-instance-caching.md) — How query instances are deduplicated and last-known results cached across components.
- [Queries](./queries.md) — General query hooks and usage patterns.
