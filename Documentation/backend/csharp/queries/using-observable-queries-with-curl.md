---
title: Use Cratis Arc observable queries with cURL
description: Inspect Cratis Arc observable queries with cURL, wait for a first allowed result, follow SSE updates, and distinguish pending reads from failures.
---
<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Use cURL to inspect a Cratis Arc observable query without building a frontend. A waiting GET returns one allowed result; Server-Sent Events (SSE) keeps the connection open for later updates. Choose the request that matches what you need to diagnose.

## Prerequisites

Start with a running Arc application exposing `/api/accounts/observe` from the [model-bound example](model-bound/observable-queries.md). The same direct-HTTP options work for [MVC GET observable actions](controller-based/observable-queries.md). Replace the origin, use a trusted certificate, and supply the application's normal credentials. The commands inspect that existing endpoint; they do not create it.

The account example requires the `AccountReader` role. Confirm the caller can access it before diagnosing a missing update. HTTP snapshots apply [read-model interception](read-model-interception.md) and [emission guards](observable-query-emission-guards.md), but the query must still authorize the caller and select only data they may read.

## Get the current snapshot

```bash
curl --include --max-time 35 \
  'https://localhost:5001/api/accounts/observe?waitForFirstResult=true'
```

Arc subscribes, waits for the first result that emission guards allow, then disposes the subscription. The server waits up to 30 seconds by default; `--max-time 35` gives the client a separate 35-second limit.

Keep `waitForFirstResult=true` for this MongoDB endpoint. Its `LifetimeAwareSubject` has no readable `Value` property, even after buffering a result. A no-wait GET returns 202 with `isReady: false` rather than reading the buffered value. That branch neither subscribes nor disposes the subject and can leave the eagerly started MongoDB watcher running.

For other producers, no-wait GET returns 200 only when Arc can read a current `Value` and the emission guards allow it; a suppressed value or no readable value returns 202. A denied value returns 403 without data. Use that mode only with a readable current value and verified cleanup when no subscription is created.

## Wait for the first payload

```bash
curl --include --max-time 15 \
  'https://localhost:5001/api/accounts/observe?waitForFirstResult=true&waitForFirstResultTimeout=10'
```

This request gives the server 10 seconds and the client 15 seconds. The server override must be positive and is measured in seconds. If a guard suppresses an emission, Arc keeps waiting within the original deadline rather than starting another 10-second wait.

Use the HTTP status and result together to decide what happened:

| Response | Meaning |
| --- | --- |
| 200 | An allowed result is ready, including a legitimately empty collection. |
| 202 on a no-wait request | No readable current value is available, or a guard suppressed it. |
| 403 from an emission guard | Delivery was denied, including when the guard failed; no data is returned. |
| 408 | The server's wait expired without an allowed result. |
| 500 | The source failed or completed before an allowed result, or result processing failed. |

Waiting does not bypass authorization or validation. Correct an invalid request or denied access before retrying it.

A waiting GET asks for a first allowed result, not a change since your previous request. MongoDB `ObserveSingle` emits null when its initial query finds no matching document; an observed collection can return an empty list. A timeout is different: the absence of an allowed emission does not prove there are no matching records.

## Stream updates over SSE

```bash
curl --no-buffer --max-time 60 \
  --header 'Accept: text/event-stream' \
  'https://localhost:5001/api/accounts/observe'
```

The `Accept` header selects the direct per-query SSE transport. `--no-buffer` lets cURL print frames as they arrive; each `data:` frame carries a `QueryResult`, not a hub message envelope. `--max-time 60` closes this diagnostic connection after a minute, so a cURL timeout at that limit is expected.

These shortened frames show the same account before and after a balance update. Other result metadata is omitted for readability:

```text
data: {"isReady":true,"isSuccess":true,"data":[{"id":"account-1","balance":100}],"changeSet":null}

data: {"isReady":true,"isSuccess":true,"data":[{"id":"account-1","balance":120}],"changeSet":null}
```

A connected stream is not proof that every result succeeded. Inspect result authorization and success fields before using its data. For several queries over one SSE connection, follow the [hub GET plus control POST lifecycle](observable-query-demultiplexer.md#sse-transport); adding `?query=...` to the hub GET does not subscribe.

## Repeated snapshot polling

When SSE is not convenient, poll deliberately with a delay. This bounded example makes five requests and backs off longer on transport/HTTP failure:

```bash
for attempt in 1 2 3 4 5; do
  if curl --silent --show-error --fail-with-body --max-time 20 \
    'https://localhost:5001/api/accounts/observe?waitForFirstResult=true&waitForFirstResultTimeout=15'; then
    printf '\n'
    sleep 2
  else
    printf '\nSnapshot request failed; backing off.\n' >&2
    sleep 5
  fi
done
```

Each successful request can return the same unchanged snapshot immediately. The delay prevents a tight request loop; it does not turn polling into a wait for changes. Use SSE when you need to follow updates continuously.

## Pick the right mode

| Goal | Mode |
| --- | --- |
| Read a MongoDB observable snapshot once | GET with `waitForFirstResult=true` |
| Read another producer's current value without subscribing | Ordinary GET only with a readable current value and verified no-subscription cleanup; handle 202 |
| Wait for a first allowed result | GET with `waitForFirstResult=true` |
| Follow one query live | Direct GET with `Accept: text/event-stream` |
| Follow several queries on one connection | [Hub protocol](observable-query-demultiplexer.md) |
| Periodically sample state | Delayed, bounded snapshot polling |

MongoDB observation failures are logged and forwarded through `OnError` before cleanup; normal cancellation, disposal, and completion are quiet. A failed observation needs a new observation rather than a subscription to the same terminated subject. Inspect provider logs alongside the failed query result to diagnose a stopped feed; see [MongoDB errors and recovery limits](../mongodb/observing-collections.md#errors-and-recovery-limits). For producer cleanup, see [subscription lifetime](model-bound/observable-queries.md#subscription-lifetime).
