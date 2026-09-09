---
title: Query health endpoint
description: Inspect tracked hub subscriptions and restrict the anonymous diagnostic feed before deployment.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Arc provides a live health feed at `/.cratis/queries/health`. The feed is itself a model-bound observable query, publishing the subscription state recorded by the hub health tracker.

> [!WARNING]
> The built-in query explicitly allows anonymous access and exposes cross-user connection IDs, query names, remote IP addresses, user agents, and user identities when available. It is diagnostic telemetry, not a safe public liveness endpoint. Restrict access before exposing an Arc host to untrusted callers; blocking only the direct health URL does not block a named health subscription through the hub.

## Why this endpoint exists

When something goes wrong with real-time data — a component freezes, a query never receives its first result, or a user reports stale data — the question is always: _is the issue in the frontend cache, the transport connection, or the backend subscription?_ The health endpoint gives you a backend-authoritative answer without attaching a debugger or tailing logs.

The query identifiers in the health snapshot use the same fully-qualified format (`{TypeFullName}.{MethodName}`) that the proxy generator writes into the generated TypeScript proxies. You can match a frontend cache entry to a backend subscriber by name alone.

## Subscribing to the feed

For a direct stream from a trusted diagnostic client, use the configured host origin and normal credentials:

```bash
curl --no-buffer --max-time 30 --header 'Accept: text/event-stream' \
  'https://localhost:5001/.cratis/queries/health'
```

For hub subscriptions, use the fully qualified name `Cratis.Arc.Queries.QueryHealth.ObserveHealth` and the [hub subscription lifecycle](observable-query-demultiplexer.md). Do not infer a generated TypeScript import path from the HTTP route; use the proxy actually generated into your application's configured output directory.

## Restrict exposure

This complete application filter denies the named health query unless the current caller is authenticated with the `QueryDiagnostics` role. It is discovered as a model-bound authorization query filter and therefore applies to both the generated health endpoint and named hub subscriptions, despite the built-in query's `[AllowAnonymous]` attribute:

```csharp
using System.Threading.Tasks;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;

namespace Application.Security;

public class RestrictQueryHealth(ICurrentPrincipalAccessor principalAccessor) : IAuthorizationQueryFilter
{
    public Task<QueryResult> OnPerform(QueryContext context)
    {
        if (context.Name.Value != "Cratis.Arc.Queries.QueryHealth.ObserveHealth")
        {
            return Task.FromResult(QueryResult.Success(context.CorrelationId));
        }

        var principal = principalAccessor.Current;
        var permitted = principal?.Identity?.IsAuthenticated == true &&
            principal.IsInRole("QueryDiagnostics");
        return Task.FromResult(permitted
            ? QueryResult.Success(context.CorrelationId)
            : QueryResult.Unauthorized(context.CorrelationId));
    }
}
```

Deploy this only with real authentication/role mapping and verify the filter is discovered. Test anonymous, authenticated non-role, and role-authorized callers against direct GET/QUERY, direct SSE/WebSocket, and both hub transports. This filter does not protect arbitrary MVC actions and does not re-check a previously authorized live stream; use [emission guards](observable-query-emission-guards.md) for ongoing revocation.

As an immediate deployment boundary, keep the entire Arc host private behind a trusted network or authenticated gateway. If restricting routes instead, include the direct health route **and** all hub/control routes or enforce the named-query filter above; exposing the hub while hiding only `/health` is insufficient. The current built-in anonymous attribute is not changed by an invented health-role option.

## Response shape

A single snapshot has two views of recorded subscription state: **connection-centric** and **query-centric**. These fields describe the model inside `QueryResult.data`, not the outer Arc envelope. Date values are ISO strings on the JSON wire; a generated client may deserialize them to `Date`.

### Top-level fields

| Field                | Type                           | Description                                                                                                  |
| -------------------- | ------------------------------ | ------------------------------------------------------------------------------------------------------------ |
| `connections`        | `QueryConnectionHealth[]`      | One entry per tracked hub connection (WebSocket or SSE); empty entries may remain briefly after unsubscribe. |
| `totalConnections`   | `number`                       | Total count of tracked connections.                                                                          |
| `totalSubscriptions` | `number`                       | Total count of recorded subscriptions across tracked connections.                                            |
| `querySubscriptions` | `QuerySubscriptionAggregate[]` | Query-centric view — one entry per distinct query name.                                                      |

### QueryConnectionHealth

| Field           | Type                          | Description                                                                                                       |
| --------------- | ----------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `connectionId`  | `string`                      | Unique connection identifier. WebSocket connections use an incrementing `ws-N` label; SSE connections use a GUID. |
| `protocol`      | `string`                      | `WebSocket` or `SSE`.                                                                                             |
| `establishedAt` | `Date`                        | When the tracker first registered a subscription for the connection.                                              |
| `subscriptions` | `QuerySubscriptionMetadata[]` | All subscriptions routed through this connection.                                                                 |

### QuerySubscriptionMetadata

| Field                | Type                          | Description                                                                         |
| -------------------- | ----------------------------- | ----------------------------------------------------------------------------------- |
| `subscriptionId`     | `string`                      | The client-generated query ID (`queryId` in the WebSocket protocol).                |
| `queryIdentifier`    | `string`                      | Fully-qualified query name — matches `queryName` on the generated TypeScript proxy. |
| `readModelType`      | `string`                      | Fully-qualified name of the read model type only (without the method).              |
| `connectedAt`        | `Date`                        | When this subscription was first established.                                       |
| `clientInfo`         | `QuerySubscriptionClientInfo` | Remote IP, user agent, user identity, and protocol.                                 |
| `lastPingSentAt`     | `Date?`                       | Last time a keep-alive ping was sent to this subscriber.                            |
| `lastPongReceivedAt` | `Date?`                       | Last time a pong was received back.                                                 |
| `lastDataServedAt`   | `Date?`                       | Last time a data frame was sent to this subscriber.                                 |

### QuerySubscriptionAggregate

The query-centric view groups all physical subscribers for a single query name into one entry. The `queryName` field uses the same identifier format as the frontend proxy's `queryName` property, making it straightforward to correlate the two sides.

| Field                | Type                | Description                                                                              |
| -------------------- | ------------------- | ---------------------------------------------------------------------------------------- |
| `queryName`          | `string`            | Fully-qualified query name — identical to `queryName` on the generated TypeScript proxy. |
| `totalSubscriptions` | `number`            | Number of physical subscribers for this query.                                           |
| `subscribers`        | `QuerySubscriber[]` | One entry per physical connection/subscription pair.                                     |

### QuerySubscriber

| Field                | Type                          | Description                                         |
| -------------------- | ----------------------------- | --------------------------------------------------- |
| `connectionId`       | `string`                      | The parent connection this subscriber belongs to.   |
| `protocol`           | `string`                      | `WebSocket` or `SSE`.                               |
| `subscriptionId`     | `string`                      | The client-generated subscription identifier.       |
| `connectedAt`        | `Date`                        | When this subscription was established.             |
| `clientInfo`         | `QuerySubscriptionClientInfo` | Remote IP, user agent, user identity, and protocol. |
| `lastPingSentAt`     | `Date?`                       | Last ping sent.                                     |
| `lastPongReceivedAt` | `Date?`                       | Last pong received.                                 |
| `lastDataServedAt`   | `Date?`                       | Last data frame sent.                               |

### QuerySubscriptionClientInfo

| Field             | Type      | Description                          |
| ----------------- | --------- | ------------------------------------ |
| `remoteIpAddress` | `string?` | Client IP address.                   |
| `userAgent`       | `string?` | Browser or client user-agent string. |
| `userId`          | `string?` | Authenticated user identity, if any. |
| `protocol`        | `string`  | `WebSocket` or `SSE`.                |

## A snapshot in JSON

Illustrative `QueryResult.data` value, not a captured response or the full envelope:

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

The hub records both multiplexed WebSocket and multiplexed SSE subscriptions. WebSocket connections have `ws-N` identifiers; SSE hub connections have GUID identifiers and may carry multiple subscriptions each. Do not confuse this with direct per-query SSE.

The current registration call sites are in the demultiplexer. Do not treat the feed as a complete inventory of direct per-query WebSocket/SSE connections, or of idle hub connections with no registered subscriptions. `querySubscriptions` groups the subscriptions that were actually recorded; absence from this feed alone does not prove no direct client is watching a query.

## Cross-stack correlation

The `queryName` in `QuerySubscriptionAggregate` and the `queryIdentifier` in `QuerySubscriptionMetadata` both use the format `{TypeFullName}.{MethodName}` — the same string the proxy generator writes as the `queryName` field on generated TypeScript proxy classes. Because both sides share the same identifier, you can match a frontend cache entry to its backend subscriptions by name.

For example, if the frontend diagnostics report that the cache entry for `MyApp.Authors.Listing.AllAuthors` is not subscribed, you can verify in the backend health feed whether a subscription for that name exists at all, which connection carries it, and when data was last served.

See [Observable Query Diagnostics](../../frontend/react/queries/observable-query-diagnostics.md) for how to access the matching frontend diagnostics.

## See also

- [Observable Query Demultiplexer](./observable-query-demultiplexer.md) — How multiplexed and direct-mode connections work.
- [Frontend: Observable Query Diagnostics](../../frontend/react/queries/observable-query-diagnostics.md) — The frontend diagnostics surface and how to correlate it with this endpoint.
