---
title: Cratis Arc Suspense queries in React
description: Cratis Arc Suspense hooks move initial query loading into React boundaries. Handle validation in the component and account for cached failures.
---

Cratis Arc Suspense query hooks let a parent display loading UI while a query is pending and catch errors outside the data-rendering component. Use them when the parent should own those states. The component still handles validation results, and resetting an error boundary does not by itself retry a failed query.

## How it works

Call `useSuspense()` inside a component beneath a React `<Suspense>` boundary. While the request is pending, the hook throws a promise and React renders the boundary's fallback. Once the request settles, the component either receives a `QueryResultWithState` or throws an error for an error boundary to catch.

For ordinary queries, server exceptions and transport failures such as network, CORS, or DNS errors become `QueryFailed`; unauthorized results become `QueryUnauthorized`. Transport failure messages are available in `exceptionMessages`. A validation result without exceptions or authorization failure returns normally with `isValid: false`, so the component still needs to inspect `isReady`, `isValid`, `validationResults`, and `isSuccess` before rendering data.

Observable Suspense waits for the first ready result over server-sent events (SSE) or WebSocket, ignoring transient `isReady: false` results. Exceptions and authorization failures reject the query during that first-ready phase. After fulfillment, later data updates do not suspend again, and later exceptions or authorization failures do not become new boundary errors. Do not use this boundary to monitor connection health or ongoing authorization.

## Observable collections and delta-only updates

> [!WARNING]
> Observable Suspense replaces `data` with each emission. It does not apply `changeSet` deltas (added, replaced, and removed items) to the previous collection. In the default shared-hub Delta mode, the first full snapshot can render correctly and the next update can empty the displayed list.

For delta feeds, use the non-Suspense observable `.use()` hook and render its loading state explicitly: it applies the changes to the previous cached collection. Use observable Suspense for collections only with a verified full-snapshot source, such as an integration configured with `ObservableQueryTransferMode.Full`. The ordinary, non-observable Suspense example below is not affected.

`observableQueryTransferMode` is not just a client display setting: the shared hub receives it in subscription requests and the server chooses full snapshots or delta emissions accordingly. Configure it before establishing subscriptions; it is global, not isolated by nested `<Arc>` providers. See [transfer-mode configuration](./configuration.md#change-stream-transfer-mode).

## Complete consumer example

In this example, `QueryBoundary` supplies the loading fallback and catches thrown errors. `ItemList` handles validation and other returned non-success states before reading `result.data`. The fragment assumes an ordinary collection query named `AllItems`, with string `id` and `name` fields. Render `Items` beneath your application's `<Arc>` provider:

```tsx
import { QueryBoundary, QueryFailed } from '@cratis/arc.react/queries';
import { AllItems } from './generated/AllItems';

function ItemList() {
    const [result, perform] = AllItems.useSuspense();
    if (!result.isReady) return <p>Result not ready.</p>;
    if (!result.isValid) return <p>Check the query inputs.</p>;
    if (!result.isSuccess) return <p>Could not load items.</p>;
    return (
        <section>
            <ul>{result.data.map(item => <li key={item.id}>{item.name}</li>)}</ul>
            <button onClick={() => void perform()}>Refresh</button>
        </section>
    );
}

export function Items() {
    return (
        <QueryBoundary loadingFallback={<p>Loading…</p>}
            onError={({ error, isQueryUnauthorized }) => (
                <p role="alert">{isQueryUnauthorized
                    ? 'You are not authorized.'
                    : error instanceof QueryFailed
                        ? 'The query failed on the server.'
                        : 'An unexpected error occurred.'}</p>
            )}>
            <ItemList />
        </QueryBoundary>
    );
}
```

While the initial request is pending, the boundary renders “Loading…”. After it settles, the list renders only when the result is ready, valid, and successful. The Refresh button calls `perform()`; its loading and retry behavior is described below.

Use `error instanceof QueryFailed` before reading `error.exceptionMessages`. The separate `isQueryFailed` boolean does not narrow the `Error` type in TypeScript. Server configuration can redact exception details, so user-visible messages must not depend on receiving them. Do not expose a full server stack trace in the fallback.

## Boundary components

`QueryErrorBoundary` catches render errors. Pair it with React's `<Suspense fallback={...}>`, or use `QueryBoundary`, which combines both.

| Prop | Component | Meaning |
| --- | --- | --- |
| `children` | Both | Protected subtree |
| `onError(info)` | Both | Error renderer; takes precedence over static fallback |
| `fallback` | Both | Static error UI |
| `loadingFallback` | `QueryBoundary` | Suspense loading UI, default null |

`QueryErrorInfo` contains `error: Error`, `isQueryFailed`, `isQueryUnauthorized`, and `reset()`. Reset clears **boundary state only**.

## Hooks

Generated `.useSuspense()` argument positions and tuples match `.use()`. An observable hook has no `perform` delegate, so its second element, when present, is `setSorting`. Only collection (enumerable) proxies generate `.useSuspenseWithPaging()`:

| Kind | Tuple |
| --- | --- |
| Ordinary | `[result, perform, setSorting]` |
| Ordinary paged | `[result, perform, setSorting, setPage, setPageSize]` |
| Observable enumerable | `[result, setSorting]` |
| Observable paged | `[result, setSorting, setPage, setPageSize]` |
| Observable single | `[result]` |

Raw hooks are `useSuspenseQuery`, `useSuspenseQueryWithPaging`, `useSuspenseObservableQuery`, and `useSuspenseObservableQueryWithPaging`. Their arguments begin with the query constructor; paged raw hooks take a `Paging` instance next. Prefer [generated signatures](./usage.mdx#return-tuple) for application code.

## Cache and re-fetching

Suspense keeps pending requests and results in module-level caches so a request survives React's attempts to render a waiting component. These caches are separate from `QueryInstanceCache`; its default 30-second retention window does not apply. Separate `<Arc>` providers do not isolate authenticated Suspense data.

For a successfully rendered ordinary query, `perform()` deletes the cached request and result, then requests another render. Refresh can therefore show the loading fallback again. The delegate reuses the hook's arguments: pass changed arguments to the hook, not to `perform()`. Sorting and page changes select a new cache key.

Calling `reset()` clears the error boundary and remounts its children, but it does not clear a failed query from the cache. If the query failed before the component first rendered successfully, no cleanup effect committed. The remounted component can immediately throw the same cached error without issuing another request. Use ordinary hooks for screens that need explicit failure and refresh controls unless your application has a tested way to recover from these cached failures.

`clearSuspenseQueryCache()` and `clearSuspenseObservableQueryCache()` are exported for test isolation. They clear global resources, not one component's failed query; do not casually wire them to a local Retry button.

## See also

For explicit loading, failure, and refresh handling in the component, continue with [query hook usage](./usage.mdx). These pages cover related choices:

- [Conditional queries](./conditional-queries.md) control when a query runs.
- [Query instance caching](./query-instance-caching.md) explains ordinary hooks' sharing and retention.
- [Observable transports](./observable-query-multiplexing.md) explains how live queries share connections.
