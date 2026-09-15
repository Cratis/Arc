---
title: Suspense queries
description: Move initial waiting into React boundaries while retaining explicit validation handling and understanding retry limitations.
---

Suspense lets a list delegate initial waiting to a surrounding loading boundary. Arc supplies Suspense variants for ordinary and observable queries, but they do not turn every unsuccessful result into a thrown error.

## How it works

A pending resource throws a Promise. React renders the Suspense fallback and retries rendering when it settles. Ordinary query resources throw `QueryFailed` for results with exceptions and `QueryUnauthorized` for authorization failure. Invalid validation results can still resolve normally: inspect `isValid`, `validationResults`, readiness, and success in your component.

Observable Suspense waits for the first **ready** result over either SSE or WebSocket. Initial exception/authorization failures reject the resource. Subsequent successful emissions update it without suspending again. Current observable handling does not promote later exception/authorization messages into new boundary failures after fulfillment; do not rely on this boundary as a connection-health or continuous-authorization monitor.

## Observable collections and delta-only updates

> [!WARNING]
> Observable Suspense currently stores each emission's data directly; it does **not** reconstruct a collection from delta-only `changeSet` updates. In the default shared-hub Delta mode, the first full snapshot can render correctly and the next update can empty the displayed list.

For delta feeds, prefer the ordinary observable `.use()` hook and render its loading state explicitly: it applies the changeset to the previous cached collection. Use observable Suspense for collections only with a verified full-snapshot source. The ordinary (non-observable) Suspense example below is not affected.

`observableQueryTransferMode` is not just a client display setting: the shared hub receives it in subscription requests and the server chooses full snapshots or delta emissions accordingly. Configure it before establishing subscriptions; it is global, not isolated by nested `<Arc>` providers. See [transfer-mode configuration](./configuration.md#change-stream-transfer-mode).

## Complete consumer example

This consumer fragment assumes an ordinary enumerable generated `AllItems` proxy with string `id` and `name` fields, rendered beneath `<Arc>`:

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

Use `error instanceof QueryFailed` before reading `error.exceptionMessages`. The separate `isQueryFailed` boolean does not narrow the `Error` type in TypeScript. Exception details may be redacted; do not promise or expose a full server stack trace to every reader.

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

Generated `.useSuspense()` argument positions and tuples match `.use()`. Only enumerable proxies generate `.useSuspenseWithPaging()`:

| Kind | Tuple |
| --- | --- |
| Ordinary | `[result, perform, setSorting]` |
| Ordinary paged | `[result, perform, setSorting, setPage, setPageSize]` |
| Observable enumerable | `[result, setSorting]` |
| Observable paged | `[result, setSorting, setPage, setPageSize]` |
| Observable single | `[result]` |

Raw hooks are `useSuspenseQuery`, `useSuspenseQueryWithPaging`, `useSuspenseObservableQuery`, and `useSuspenseObservableQueryWithPaging`. Their arguments begin with the query constructor; paged raw hooks take a `Paging` instance next. Prefer [generated signatures](./usage.md#return-tuple) for application code.

## Cache and re-fetching

Suspense uses module-level resource caches so in-flight work survives renders that suspend before committing. These are separate from `QueryInstanceCache` and its 30-second retention option. They are not per-provider authenticated data isolation.

For a successfully rendered ordinary query, `perform()` deletes that resource and requests another render. Its current implementation reuses the hook's arguments; supply changed arguments to the hook rather than relying on an argument passed to the refresh delegate. Sorting/page changes select a new resource key.

**Reset is not retry.** A first-render rejected resource can remain in the cache because no effect committed to clean it up. Clicking `reset()` alone can immediately throw the same rejection without issuing a new request. Fine-grained failed-resource invalidation and reliable first-load retry are runtime follow-up candidates. Until an application has a tested recovery strategy, use ordinary hooks for screens requiring explicit recoverable error controls.

`clearSuspenseQueryCache()` and `clearSuspenseObservableQueryCache()` are exported for test isolation. They clear global resources, not one component's failed query; do not casually wire them to a local Retry button.

## See also

- [Conditional queries](./conditional-queries.md)
- [Query instance caching](./query-instance-caching.md)
- [Observable transports](./observable-query-multiplexing.md)
