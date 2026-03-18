# Observable Query Hub

The Observable Query Hub is a composite real-time streaming endpoint that multiplexes multiple observable query subscriptions over a single persistent connection.

Rather than creating one WebSocket or SSE connection per query, the hub provides **two fixed, well-known endpoints** — one for WebSocket and one for Server-Sent Events — that all observable queries in an application route through.

## Endpoints

| Transport | Endpoint | Direction |
|-----------|----------|-----------|
| WebSocket | `/.cratis/queries/ws` | Bidirectional — subscribe, unsubscribe, ping/pong |
| Server-Sent Events (SSE) | `/.cratis/queries/sse` | Server → client; one query per connection |

Both endpoints are registered automatically by `UseCratisArc()` and require no manual configuration.

## Why a Composite Hub?

Individual per-query WebSocket endpoints work but come with drawbacks at scale:

- Each browser tab opens a separate WebSocket per observable query.
- HTTP/1.1 limits the number of concurrent connections per origin.
- SSE has the same constraint and typically falls back to polling when the connection limit is reached.

The hub solves this by allowing a single WebSocket to carry updates for many queries simultaneously, and by providing a single, predictable SSE endpoint that clients can connect to for any query.

## Protocol

All messages exchanged over the hub share a common envelope:

```json
{
  "type": "<ObservableQueryHubMessageType>",
  "queryId": "<client-assigned-id>",
  "payload": { ... },
  "timestamp": 1234567890
}
```

| Field | Description |
|-------|-------------|
| `type` | One of the message types listed below. |
| `queryId` | Client-assigned identifier that correlates subscriptions with their result updates. Must be unique per subscription within a connection. |
| `payload` | Depends on `type` — see the table below. |
| `timestamp` | Unix milliseconds. Only populated for `ping` / `pong`. |

### Message Types

| Type | Direction | Payload |
|------|-----------|---------|
| `subscribe` (0) | Client → Server | `ObservableQuerySubscriptionRequest` |
| `unsubscribe` (1) | Client → Server | *(none)* |
| `queryResult` (2) | Server → Client | `QueryResult` |
| `unauthorized` (3) | Server → Client | *(none)* |
| `error` (4) | Server → Client | Error message string |
| `ping` (5) | Client → Server | *(timestamp only)* |
| `pong` (6) | Server → Client | *(timestamp echoed from ping)* |

### Subscribe Payload — `ObservableQuerySubscriptionRequest`

```json
{
  "queryName": "MyApp.Authors.Listing.AllAuthors",
  "arguments": { "filter": "active" },
  "page": 0,
  "pageSize": 25,
  "sortBy": "name",
  "sortDirection": "asc"
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `queryName` | ✅ | Fully qualified name of the observable query method (e.g. `MyApp.Features.Authors.Listing.AllAuthors`). |
| `arguments` | ☐ | Query-string arguments forwarded to the query performer. |
| `page` | ☐ | Zero-based page index for paged queries. |
| `pageSize` | ☐ | Number of items per page. |
| `sortBy` | ☐ | Field name to sort by (case-insensitive). |
| `sortDirection` | ☐ | `asc` or `desc`. |

## WebSocket Transport

Connect to `/.cratis/queries/ws` and send `subscribe` messages to start receiving updates.

### Subscribe

```json
{
  "type": 0,
  "queryId": "authors-list",
  "payload": {
    "queryName": "MyApp.Authors.Listing.AllAuthors"
  }
}
```

The server responds with one or more `queryResult` messages whenever the underlying data changes:

```json
{
  "type": 2,
  "queryId": "authors-list",
  "payload": {
    "isSuccess": true,
    "isAuthorized": true,
    "data": [ ... ],
    "validationResults": []
  }
}
```

### Unsubscribe

```json
{
  "type": 1,
  "queryId": "authors-list"
}
```

### Keep-alive (Ping / Pong)

Send a `ping` with the current Unix timestamp; the server echoes it as a `pong` with the same timestamp for round-trip latency measurement.

```json
{ "type": 5, "timestamp": 1740000000000 }
// Server responds:
{ "type": 6, "timestamp": 1740000000000 }
```

### Unauthorized

If the current user is not authorized to access the requested query, the server sends an `unauthorized` message and no data stream is established:

```json
{
  "type": 3,
  "queryId": "authors-list"
}
```

## SSE Transport

Connect to `/.cratis/queries/sse` using the `EventSource` API. Pass the fully qualified query name in the `query` query-string parameter. All other query-string parameters are forwarded as query arguments.

```http
GET /.cratis/queries/sse?query=MyApp.Authors.Listing.AllAuthors&filter=active
```

The server responds with the standard SSE content type (`text/event-stream`) and streams `data:` frames containing serialised `ObservableQueryHubMessage` envelopes whenever the underlying data changes.

Each SSE connection carries a **single query subscription**. To observe multiple queries, open multiple `EventSource` connections.

### Paging and Sorting via SSE

Pass paging and sorting directly as query-string parameters:

```http
GET /.cratis/queries/sse?query=MyApp.Authors.Listing.AllAuthors&page=0&pageSize=20&sortBy=name&sortDirection=asc
```

## Authorization

Authorization is enforced for every subscription through the standard query pipeline, including all registered `IQueryFilter` implementations.

- If the query performer has an `[Authorize]` attribute and the current user is not authenticated or lacks the required role, the subscription is rejected with an `unauthorized` message.
- If the query allows anonymous access (`[AllowAnonymous]`), the subscription is accepted regardless of authentication state.
- Authorization is re-evaluated on every new subscription, not cached for the lifetime of the connection.

## Frontend Integration

The TypeScript client (`@cratis/arc`) uses the hub automatically when the transport method is configured as SSE (the default).

### Configuring the transport

Pass `queryTransportMethod` to the `<Arc>` component:

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
| `QueryTransportMethod.ServerSentEvents` | Uses the SSE hub endpoint (default). |
| `QueryTransportMethod.WebSocket` | Uses the per-query WebSocket endpoint (legacy). |

### How SSE connections are built

When `ServerSentEvents` is selected, `ObservableQueryFor.subscribe()` connects to
`/.cratis/queries/sse?query=<fullyQualifiedQueryName>&<queryArgs>` using the browser's native `EventSource`. The connection retries automatically if the server becomes unavailable.

## See also

- [Observable Queries (model-bound)](./model-bound/observable-queries.md) — How to expose observable queries in the backend.
- [Query Pipeline](./query-pipeline.md) — How the query pipeline works, including filter hooks.
- [Authorization](../core/authorization.md) — Role-based authorization for queries and commands.
