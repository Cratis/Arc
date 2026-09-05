---
title: Query health endpoint
description: Built-in real-time endpoint that exposes the live health of every observable query connection and subscription, with identifiers aligned to the frontend cache.
---

Every running Arc application exposes a live health feed for its observable query subsystem at `/.cratis/queries/health`. The feed is itself an observable query — it pushes a new snapshot every time a connection opens, a subscription changes, or a subscriber disconnects. No polling required.

## Why this endpoint exists

When something goes wrong with real-time data — a component freezes, a query never receives its first result, or a user reports stale data — the question is always: *is the issue in the frontend cache, the transport connection, or the backend subscription?* The health endpoint gives you a backend-authoritative answer without attaching a debugger or tailing logs.

The query identifiers in the health snapshot use the same fully-qualified format (`{TypeFullName}.{MethodName}`) that the proxy generator writes into the generated TypeScript proxies. You can match a frontend cache entry to a backend subscriber by name alone.

## Subscribing to the feed

The endpoint is a standard model-bound observable query. Subscribe to it the same way you subscribe to any other observable query in the frontend:

```typescript
import { ObserveHealth } from '/.cratis/queries/QueryHealth'; // generated proxy

const [health] = ObserveHealth.use();
// health.data — QueryHealth snapshot, updated in real time
```

The endpoint is anonymous — no authentication is required.

## Response shape

A single snapshot has two views of the same subscription state: **connection-centric** (one entry per physical transport connection) and **query-centric** (one entry per distinct query name). Both views are always present in the same snapshot.

### Top-level fields

| Field | Type | Description |
|---|---|---|
| `connections` | `QueryConnectionHealth[]` | One entry per open transport connection (WebSocket or SSE). |
| `totalConnections` | `number` | Total count of open connections. |
| `totalSubscriptions` | `number` | Total count of active subscriptions across all connections. |
| `querySubscriptions` | `QuerySubscriptionAggregate[]` | Query-centric view — one entry per distinct query name. |

### QueryConnectionHealth

| Field | Type | Description |
|---|---|---|
| `connectionId` | `string` | Unique connection identifier. WebSocket connections use an incrementing `ws-N` label; SSE connections use a GUID. |
| `protocol` | `string` | `WebSocket` or `SSE`. |
| `establishedAt` | `Date` | When the connection was opened. |
| `subscriptions` | `QuerySubscriptionMetadata[]` | All subscriptions routed through this connection. |

### QuerySubscriptionMetadata

| Field | Type | Description |
|---|---|---|
| `subscriptionId` | `string` | The client-generated query ID (`queryId` in the WebSocket protocol). |
| `queryIdentifier` | `string` | Fully-qualified query name — matches `queryName` on the generated TypeScript proxy. |
| `readModelType` | `string` | Fully-qualified name of the read model type only (without the method). |
| `connectedAt` | `Date` | When this subscription was first established. |
| `clientInfo` | `QuerySubscriptionClientInfo` | Remote IP, user agent, user identity, and protocol. |
| `lastPingSentAt` | `Date?` | Last time a keep-alive ping was sent to this subscriber. |
| `lastPongReceivedAt` | `Date?` | Last time a pong was received back. |
| `lastDataServedAt` | `Date?` | Last time a data frame was sent to this subscriber. |

### QuerySubscriptionAggregate

The query-centric view groups all physical subscribers for a single query name into one entry. The `queryName` field uses the same identifier format as the frontend proxy's `queryName` property, making it straightforward to correlate the two sides.

| Field | Type | Description |
|---|---|---|
| `queryName` | `string` | Fully-qualified query name — identical to `queryName` on the generated TypeScript proxy. |
| `totalSubscriptions` | `number` | Number of physical subscribers for this query. |
| `subscribers` | `QuerySubscriber[]` | One entry per physical connection/subscription pair. |

### QuerySubscriber

| Field | Type | Description |
|---|---|---|
| `connectionId` | `string` | The parent connection this subscriber belongs to. |
| `protocol` | `string` | `WebSocket` or `SSE`. |
| `subscriptionId` | `string` | The client-generated subscription identifier. |
| `connectedAt` | `Date` | When this subscription was established. |
| `clientInfo` | `QuerySubscriptionClientInfo` | Remote IP, user agent, user identity, and protocol. |
| `lastPingSentAt` | `Date?` | Last ping sent. |
| `lastPongReceivedAt` | `Date?` | Last pong received. |
| `lastDataServedAt` | `Date?` | Last data frame sent. |

### QuerySubscriptionClientInfo

| Field | Type | Description |
|---|---|---|
| `remoteIpAddress` | `string?` | Client IP address. |
| `userAgent` | `string?` | Browser or client user-agent string. |
| `userId` | `string?` | Authenticated user identity, if any. |
| `protocol` | `string` | `WebSocket` or `SSE`. |

## A snapshot in JSON

```json
{
  "connections": [
    {
      "connectionId": "ws-1",
      "protocol": "WebSocket",
      "establishedAt": "2026-06-10T14:03:00Z",
      "subscriptions": [
        {
          "subscriptionId": "all-authors-main",
          "queryIdentifier": "MyApp.Authors.Listing.AllAuthors",
          "readModelType": "MyApp.Authors.Listing",
          "connectedAt": "2026-06-10T14:03:01Z",
          "lastPingSentAt": "2026-06-10T14:04:00Z",
          "lastPongReceivedAt": "2026-06-10T14:04:00Z",
          "lastDataServedAt": "2026-06-10T14:03:01Z",
          "clientInfo": {
            "protocol": "WebSocket",
            "remoteIpAddress": "127.0.0.1",
            "userAgent": "Mozilla/5.0 ...",
            "userId": "alice@example.com"
          }
        }
      ]
    }
  ],
  "totalConnections": 1,
  "totalSubscriptions": 1,
  "querySubscriptions": [
    {
      "queryName": "MyApp.Authors.Listing.AllAuthors",
      "totalSubscriptions": 1,
      "subscribers": [
        {
          "connectionId": "ws-1",
          "protocol": "WebSocket",
          "subscriptionId": "all-authors-main",
          "connectedAt": "2026-06-10T14:03:01Z",
          "lastPingSentAt": "2026-06-10T14:04:00Z",
          "lastPongReceivedAt": "2026-06-10T14:04:00Z",
          "lastDataServedAt": "2026-06-10T14:03:01Z",
          "clientInfo": {
            "protocol": "WebSocket",
            "remoteIpAddress": "127.0.0.1",
            "userAgent": "Mozilla/5.0 ...",
            "userId": "alice@example.com"
          }
        }
      ]
    }
  ]
}
```

## Transport modes

The health endpoint covers both transport modes equally. When a user opens the application in multiplexed WebSocket mode, their queries appear under a single `ws-N` connection. When they run in direct SSE mode (one connection per query), each query gets its own entry in `connections` with a GUID as its `connectionId`.

The `querySubscriptions` array always gives a unified query-level view regardless of which transport the clients are using. That makes it the right starting point when you want to know how many users are subscribed to a specific query — you do not need to filter the `connections` array yourself.

## Cross-stack correlation

The `queryName` in `QuerySubscriptionAggregate` and the `queryIdentifier` in `QuerySubscriptionMetadata` both use the format `{TypeFullName}.{MethodName}` — the same string the proxy generator writes as the `queryName` field on generated TypeScript proxy classes. Because both sides share the same identifier, you can match a frontend cache entry to its backend subscriptions by name.

For example, if the frontend diagnostics report that the cache entry for `MyApp.Authors.Listing.AllAuthors` is not subscribed, you can verify in the backend health feed whether a subscription for that name exists at all, which connection carries it, and when data was last served.

See [Observable Query Diagnostics](../../frontend/react/queries/observable-query-diagnostics.md) for how to access the matching frontend diagnostics.

## See also

- [Observable Query Demultiplexer](./observable-query-demultiplexer.md) — How multiplexed and direct-mode connections work.
- [Frontend: Observable Query Diagnostics](../../frontend/react/queries/observable-query-diagnostics.md) — The frontend diagnostics surface and how to correlate it with this endpoint.
