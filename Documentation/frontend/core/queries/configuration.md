---
title: Query Configuration
description: Configure query microservice routing, API base path, observable transport, cache retention, and HTTP method, with or without React.
---

Queries can be configured for service routing and endpoint behavior in both React and non-React environments.

## Microservice Routing

Per query:

```typescript
query.setMicroservice('inventory-service');
```

In React applications, configure this globally with `<Arc microservice="..." />`.

## API Base Path

Per query:

```typescript
query.setApiBasePath('/api/v1');
```

In React applications, set this globally with `<Arc apiBasePath="..." />`.

## Observable Query Transport (`queryDirectMode`)

By default, observable queries use centralized hub endpoints. Set `Globals.queryDirectMode = true` to bypass the hub and connect directly per query.

```typescript
import { Globals } from '@cratis/arc';

Globals.queryDirectMode = true;
```

| Value | Description |
| ------- | ------------- |
| `false` (default) | Route through `/.cratis/queries/sse` or `/.cratis/queries/ws`. |
| `true` | Connect directly to each query URL using the separately selected SSE or WebSocket transport. |

In React, this is usually configured through `<Arc queryDirectMode={...} />`. See [React query integration](../../react/queries/index.md).

## Polling transport

If a proxy cuts long-lived connections, your host caps request duration, or you want to scale to zero between requests, use polling instead of keeping a WebSocket or SSE stream open. You still subscribe to the observable query, but Arc fetches its current snapshot through repeated HTTP GET requests.

Select polling explicitly before subscribing; Arc does not automatically select it or fall back between transports in this release:

```typescript
import { Globals } from '@cratis/arc';
import { QueryTransportMethod } from '@cratis/arc/queries';

Globals.queryTransportMethod = QueryTransportMethod.Polling;
Globals.queryPollingOptions = {
    intervalMs: 5_000,
    minIntervalMs: 1_000,
    maxIntervalMs: 60_000,
    backoffMultiplier: 2,
    jitterRatio: 0.2,
};
```

These are the defaults. The three interval fields use milliseconds: `intervalMs` is the normal delay after a successful poll, and `minIntervalMs` and `maxIntervalMs` bound every delay. After transient failures, `backoffMultiplier` grows the delay exponentially and `jitterRatio` adds symmetric random variation (0.2 means ±20%). Only one request runs at a time; the next delay starts after the previous response is handled.

To tune one generated query instance, call its setter before `subscribe`. This fragment assumes `query` is your generated `ObservableQueryFor` instance and `onResult` is your result callback:

```typescript
query.setPollingOptions({ intervalMs: 2_000 });
const subscription = query.subscribe(onResult);
// When the consumer is done:
subscription.unsubscribe();
```

The overrides apply on the next subscription. Polling always targets each query's own URL; `queryDirectMode` has no effect. Custom HTTP headers from the query's headers callback are attached to every poll. Arc emits the first snapshot, then suppresses identical results, ignoring only `correlationId` when comparing snapshots.

Network errors, non-JSON responses, server errors, and not-ready/timeout responses (202 and 408) trigger backoff. A server-error query envelope is delivered before retrying. Authorization failures (401, 403, or an envelope with `isAuthorized: false`) and validation failures (`isValid: false`) deliver the failure and stop polling until you create a new subscription.

A server may return `Cratis-Query-Poll-After` with the next delay **in seconds**. Arc clamps it to your minimum and maximum and prefers it over the normal or backoff delay. For cross-origin requests, the server must list `Cratis-Query-Poll-After` in `Access-Control-Expose-Headers` so the browser can read it. Selecting polling does not make the server emit this optional header.

In React, set `Globals.queryPollingOptions` before rendering and pass `queryTransportMethod={QueryTransportMethod.Polling}` to `<Arc>`. See [React observable transport](../../react/queries/observable-queries.md#querytransportmethod).

### When polling is the wrong fit

- Update latency follows the polling interval; use streaming when you need immediate updates.
- Request load grows with clients × queries. Sharing matching subscribers still comes from the query instance cache, not a polling hub.
- Changes between polls are coalesced into a snapshot, so polling is not a lossless event stream.
- Each poll re-runs the server's per-request subscription. Consider that cost for expensive observations.

## Query Cache Retention (`queryCacheRetentionMs`)

Controls how long (in milliseconds) the query cache keeps an entry alive after the last subscriber releases it. The default is `30 000` ms (30 seconds).

```typescript
import { Globals } from '@cratis/arc';

// Keep cached data for 60 seconds after the last subscriber unmounts.
Globals.queryCacheRetentionMs = 60_000;

// Schedule zero-delay cleanup after the last subscriber releases the entry.
Globals.queryCacheRetentionMs = 0;
```

In React applications, set this through `<Arc queryCacheRetentionMs={...} />` instead of modifying `Globals` directly. See [React query configuration](../../react/queries/configuration.md).

## HTTP query method

Ordinary query method selection (`Get`, `Query`, `Auto`) is separate from observable transport selection. See [using the HTTP QUERY method](../../../backend/csharp/queries/using-the-http-query-method.md) for global, per-query, and resolver configuration.

Native streaming handshakes cannot attach arbitrary callback headers; see [transport credentials](../../react/arc.md#http-headers-callback). React bindings also mutate global settings, so nested providers are not independent authenticated hubs.

## See Also

- [Observable Queries](../../react/queries/observable-queries.md)
- [Observable Query Multiplexing](../../react/queries/observable-query-multiplexing.md)
