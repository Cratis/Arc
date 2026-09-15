---
title: Use observable queries with cURL
description: Read snapshots, wait for a first result, stream direct SSE, and poll without a tight loop.
---
<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Prerequisites

Use a running Arc application's actual observable query URL. The commands below target the explicit `/api/accounts/observe` path from the [model-bound example](model-bound/observable-queries.md); the same direct-HTTP options work for [MVC GET observable actions](controller-based/observable-queries.md). Replace the origin and provide your application's normal credentials. These are runnable client checkpoints against that configured host, not commands that create a backend.

> [!WARNING]
> Observable HTTP snapshots currently bypass [read-model interception](read-model-interception.md). Never assume streaming-only masking protects this snapshot route.

## Get the current snapshot

```bash
curl --include --max-time 35 \
  'https://localhost:5001/api/accounts/observe?waitForFirstResult=true'
```

Use the wait option for this MongoDB endpoint: Arc subscribes to receive a first emission, then disposes the subscription. The server's default wait is 30 seconds; the client bound above is 35 seconds.

> [!WARNING]
> MongoDB's `LifetimeAwareSubject` has no readable `Value` property, even after buffering a result. An ordinary no-wait GET therefore returns **202 Accepted** with `isReady: false`, rather than reading that replayed value. The provider starts its watcher eagerly, but this HTTP branch neither subscribes nor disposes the subject and can leave the watcher running. Do not use no-wait snapshots for this provider.

For other producers, no-wait GET returns 200 only when Arc can read a current `Value`; otherwise it returns 202. Use that mode only with a readable current value and verified cleanup when no subscription is created.

## Wait for the first payload

```bash
curl --include --max-time 15 \
  'https://localhost:5001/api/accounts/observe?waitForFirstResult=true&waitForFirstResultTimeout=10'
```

The server waits for the first emission for up to 10 seconds; the default is 30 seconds. A positive timeout override is measured in seconds. `--max-time` bounds the client separately.

- A first emission returns 200 with data, including a legitimately empty collection.
- A wait timeout returns 408 with an error result.
- Completion before any emission returns 500 with an error result.
- An authorization/validation failure is not made successful by waiting.

This waits for **a first result**, not for a change since a previous request. `ObserveSingle` with no matching document may never emit; a filtered observed collection can express absence as an empty list.

## Stream updates over SSE

```bash
curl --no-buffer --max-time 60 \
  --header 'Accept: text/event-stream' \
  'https://localhost:5001/api/accounts/observe'
```

This requests the direct per-query SSE transport. Each `data:` frame carries a `QueryResult`, not a hub message envelope. `--max-time 60` deliberately closes the diagnostic stream after a minute; cURL reports a timeout when that bound is reached.

Illustrative shortened frames; other result metadata is omitted here only for readability:

```text
data: {"isReady":true,"isSuccess":true,"data":[{"id":"account-1","balance":100}],"changeSet":null}

data: {"isReady":true,"isSuccess":true,"data":[{"id":"account-1","balance":120}],"changeSet":null}
```

For several queries over one SSE connection, use the [hub GET plus control POST lifecycle](observable-query-demultiplexer.md#sse-transport). Adding `?query=...` to the hub GET does not subscribe.

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

This is **repeated snapshot polling, not change-aware long polling**. An unchanged current snapshot can return immediately on every request. Removing the delay creates a tight load-generating loop. Prefer SSE to follow changes continuously.

## Pick the right mode

| Goal | Mode |
| --- | --- |
| Read a MongoDB observable snapshot once | GET with `waitForFirstResult=true` |
| Read another producer's current value without subscribing | Ordinary GET only with a readable current value and verified no-subscription cleanup; handle 202 |
| Wait for a first emission | GET with `waitForFirstResult=true` |
| Follow one query live | Direct GET with `Accept: text/event-stream` |
| Follow several queries on one connection | [Hub protocol](observable-query-demultiplexer.md) |
| Periodically sample state | Delayed, bounded snapshot polling |

MongoDB source failures may be logged and completed rather than sent through an observable error channel. Inspect provider logs as well as HTTP output when a feed stops. For producer cleanup, see [subscription lifetime](model-bound/observable-queries.md#subscription-lifetime).
