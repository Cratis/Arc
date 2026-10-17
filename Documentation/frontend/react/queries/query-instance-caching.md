---
title: Query instance caching
description: Understand instance reuse, observable subscription sharing, and the default 30-second retention window.
---

Navigating away from a live list and back should not always start from an empty screen. Arc retains query instances and last-known data, but instance reuse and request deduplication are different contracts.

## How it works

`<Arc>` creates one `QueryInstanceCache` per mount. Keys use the generated stable `queryName` and JSON-serialized arguments with top-level keys sorted. `useQuery` and `useQueryWithPaging` add the current page, page size and sort order to that key; `useObservableQuery` does not. No key includes origin or credentials, so do not treat the cache as an isolation boundary for independent views or users.

| Hook family | Shared instance | Network/results |
| --- | --- | --- |
| `useObservableQuery` | Yes, for the same cache key | Shares subscription ownership and registers cache listeners for updates |
| `useQuery` / `useQueryWithPaging` | Only for the same query, arguments, page and sort order | Each enabled mount performs a request; a superseded mount adopts the superseding result, with no cache-listener broadcast |
| Suspense hooks | Separate module-level resource caches | See [Suspense queries](./suspense-queries.md); not governed by this cache's retention setting |

An ordinary query cache hit can seed a new component with successful cached data, but it does not eliminate the new request.

### Ordinary queries are isolated by arguments, paging and sorting

Two components using the same ordinary query with different arguments, pages or sort orders get separate instances. Neither fetches the other's page or aborts the other's request. When a component changes its arguments, page or sort order, it moves to the matching entry and fetches again. A successful result is cached only under the key it answers, so the previous arguments keep their own last known result.

Components that agree on all of them share one instance, and an instance runs one request at a time. If the first component's request is still in flight when the second component mounts, the second request aborts it. The first component then adopts the outcome of the request that superseded it — its data or its failure — instead of staying on its initial, never-ready state. See [query scope](./scope.md#when-an-ordinary-query-stops-performing) for what happens when either component unmounts first.

Adoption follows that one superseding request; it is not a broadcast. Only a request for the same arguments is adopted, so an explicit `perform` with other arguments updates only the component that called it, and so does any request made while no other component is waiting. `useObservableQuery` differs: its cache listeners push every update to all mounted consumers of an entry.

Observable queries key only by query and arguments, and set paging and sorting on the shared instance. Do not assume independent paging or sorting between observable consumers of the same query and arguments.

## Lifecycle

When the last consumer releases an entry, Arc schedules eviction after `queryCacheRetentionMs`, **30,000 ms by default**. Cached data and an established observable subscription remain alive during that window. A new consumer cancels pending eviction and can reuse them. On expiry the entry is removed and its subscription torn down.

```mermaid
flowchart LR
    Mount[First consumer mounts] --> Entry[Create entry and subscribe]
    Entry --> Shared[Additional consumers share entry]
    Shared --> Release[Last consumer releases]
    Release --> Timer[30-second retention timer]
    Timer -->|consumer returns| Shared
    Timer -->|expires| Evict[Unsubscribe and evict]
```

Configure retention once when mounting Arc:

```tsx
import { Arc } from '@cratis/arc.react';

export const App = () => (
    <Arc queryCacheRetentionMs={60_000}>
        <main>Your query components</main>
    </Arc>
);
```

A value of zero schedules eviction without the retention delay. A cache entry expiring does not imply every pooled hub connection immediately closes.

## React StrictMode compatibility

Reacquiring an entry cancels its cleanup timer. Provider teardown also defers disposal so an immediate remount can cancel it. That provider-disposal mechanism is distinct from the per-entry retention window; normal last-consumer release is not always `setTimeout(0)`.

## Relationship with the conditional when hook

Disabled non-Suspense hooks still create/look up and acquire cache entries. The condition suppresses their automatic request/subscription, not allocation or cache access. Another consumer can already own a live subscription for the same entry. A previously established subscription can also survive through retention.

Use the explicit condition to choose UI, rather than treating `hasData` as an enabled flag. Generated single-result defaults are `{}`, so `hasData` can be true without loaded data. See [conditional queries](./conditional-queries.md).

## Trust and ownership limits

Cached UI data can outlive its last viewer. Clearing identity or reconnecting observables is not a data purge, and nested providers do not isolate the global transport multiplexer. Treat account-switching and sensitive-data eviction as an explicit application concern. See [Arc provider boundaries](../arc.md).

## See also

- [Observable query multiplexing](./observable-query-multiplexing.md)
- [Query diagnostics](./observable-query-diagnostics.md)
- [Query usage](./usage.mdx)
