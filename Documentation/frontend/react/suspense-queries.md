# Suspense Queries

React 18 introduced first-class support for [Suspense](https://react.dev/reference/react/Suspense) and
[ErrorBoundary](https://react.dev/reference/react/Component#catching-rendering-errors-with-an-error-boundary)
patterns. Arc provides Suspense-compatible variants of every query hook so components can declaratively
express their loading and error states without managing `isPerforming` or `hasExceptions` flags manually.

## How It Works

Suspense-compatible hooks use React's throw-a-Promise protocol:

1. The hook is called inside a component tree wrapped in `<Suspense>` and a `QueryErrorBoundary`.
2. While the query is in-flight the hook **throws a Promise**. React catches it, shows the `<Suspense>` `fallback`, and re-renders the component when the Promise resolves.
3. On success the component renders normally, with the result already available.
4. On failure the hook **throws an error** (`QueryFailed` or `QueryUnauthorized`). The nearest `QueryErrorBoundary` catches it and shows its fallback.

This means the component body contains only the **happy-path** rendering logic — loading and error handling are entirely declarative through the surrounding boundaries.

```tsx
<QueryErrorBoundary onError={({ isQueryUnauthorized, reset }) =>
    isQueryUnauthorized
        ? <p>Not authorized. <button onClick={reset}>Retry</button></p>
        : <p>Something went wrong. <button onClick={reset}>Retry</button></p>
}>
    <Suspense fallback={<Spinner />}>
        <ItemList />  {/* suspends until data arrives */}
    </Suspense>
</QueryErrorBoundary>
```

> **Note**: Because these hooks suspend during loading, they **must** be rendered inside a `<Suspense>` boundary; otherwise React will throw an unhandled Promise to the root.

## Boundary Components

Arc ships two ready-to-use boundary components so you don't need to write your own class-based `ErrorBoundary`.

### QueryErrorBoundary

A class-based error boundary that catches `QueryFailed` and `QueryUnauthorized` thrown by the Suspense query hooks. Use it when you want to control the `<Suspense>` and `QueryErrorBoundary` separately.

```tsx
import { QueryErrorBoundary } from '@cratis/arc.react/queries';

<QueryErrorBoundary
    onError={({ error, isQueryFailed, isQueryUnauthorized, reset }) => (
        <div>
            <p>{isQueryUnauthorized ? 'Not authorized' : 'Server error'}</p>
            {isQueryFailed && <p>{error.exceptionMessages.join(', ')}</p>}
            <button onClick={reset}>Retry</button>
        </div>
    )}
>
    <Suspense fallback={<Spinner />}>
        <ItemList />
    </Suspense>
</QueryErrorBoundary>
```

**Props:**

| Prop | Type | Description |
| ---- | ---- | ----------- |
| `onError` | `(info: QueryErrorInfo) => ReactNode` | Called when an error is caught. Return the fallback UI. |
| `fallback` | `ReactNode` | Static fallback to render when an error is caught. `onError` takes precedence if both are provided. |
| `children` | `ReactNode` | The subtree to protect. |

**`QueryErrorInfo`:**

| Property | Type | Description |
| -------- | ---- | ----------- |
| `error` | `Error` | The raw error that was caught. |
| `isQueryFailed` | `boolean` | `true` when the error is a `QueryFailed`. |
| `isQueryUnauthorized` | `boolean` | `true` when the error is a `QueryUnauthorized`. |
| `reset()` | `() => void` | Resets the boundary so the child subtree is re-mounted. |

### QueryBoundary

A convenience wrapper that combines `<Suspense>` and `QueryErrorBoundary` in a single component. Use this for the common case where you want to handle both loading and error states together.

```tsx
import { QueryBoundary } from '@cratis/arc.react/queries';

<QueryBoundary
    loadingFallback={<Spinner />}
    onError={({ isQueryUnauthorized, reset }) =>
        isQueryUnauthorized
            ? <p>Not authorized. <button onClick={reset}>Retry</button></p>
            : <p>Something went wrong. <button onClick={reset}>Retry</button></p>
    }
>
    <ItemList />
</QueryBoundary>
```

**Props:**

| Prop | Type | Description |
| ---- | ---- | ----------- |
| `loadingFallback` | `ReactNode` | Rendered by the inner `<Suspense>` while the query is loading. Defaults to `null`. |
| `onError` | `(info: QueryErrorInfo) => ReactNode` | Called when an error is caught. Return the fallback UI. |
| `fallback` | `ReactNode` | Static error fallback. `onError` takes precedence if both are provided. |
| `children` | `ReactNode` | The subtree to protect. |

## Hooks

### useSuspenseQuery

Use `useSuspenseQuery` for standard request/response HTTP queries.

```typescript
import { useSuspenseQuery } from '@cratis/arc.react/queries';

function ItemList() {
    const [result, perform] = useSuspenseQuery(AllItems);

    return (
        <ul>
            {result.data.map(item => <li key={item.id}>{item.name}</li>)}
        </ul>
    );
}
```

**Signature:**

```typescript
useSuspenseQuery<TDataType, TQuery, TArguments>(
    query: Constructor<TQuery>,
    args?: TArguments,
    sorting?: Sorting
): [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting]
```

| Return value | Type | Description |
| ------------ | ---- | ----------- |
| `result` | `QueryResultWithState<TDataType>` | The resolved query result. Never `undefined` when the component renders. |
| `perform` | `() => Promise<void>` | Re-runs the query. Clears the cache and triggers a new Suspense cycle. |
| `setSorting` | `(sorting: Sorting) => Promise<void>` | Changes the sort order and triggers a new Suspense cycle. |

#### With Paging

```typescript
import { useSuspenseQueryWithPaging, Paging } from '@cratis/arc.react/queries';

function PagedList() {
    const [result, perform, setSorting, setPage, setPageSize] =
        useSuspenseQueryWithPaging(AllItems, new Paging(0, 20));

    return (
        <>
            <ul>
                {result.data.map(item => <li key={item.id}>{item.name}</li>)}
            </ul>
            <button onClick={() => setPage(result.paging.page + 1)}>Next page</button>
        </>
    );
}
```

### useSuspenseObservableQuery

Use `useSuspenseObservableQuery` for WebSocket-based observable queries. The component suspends until the **first message** is received over the WebSocket, then re-renders reactively on every subsequent update without suspending again.

```typescript
import { useSuspenseObservableQuery } from '@cratis/arc.react/queries';

function LiveFeed() {
    const [result] = useSuspenseObservableQuery(LiveItems);

    return (
        <ul>
            {result.data.map(item => <li key={item.id}>{item.name}</li>)}
        </ul>
    );
}
```

**Signature:**

```typescript
useSuspenseObservableQuery<TDataType, TQuery, TArguments>(
    query: Constructor<TQuery>,
    args?: TArguments,
    sorting?: Sorting
): [QueryResultWithState<TDataType>, SetSorting]
```

| Return value | Type | Description |
| ------------ | ---- | ----------- |
| `result` | `QueryResultWithState<TDataType>` | The latest result from the observable. Never `undefined` when the component renders. |
| `setSorting` | `(sorting: Sorting) => Promise<void>` | Changes the sort order, unsubscribes the old stream, and subscribes a new one. |

#### With Paging

```typescript
import { useSuspenseObservableQueryWithPaging, Paging } from '@cratis/arc.react/queries';

function PagedFeed() {
    const [result, setSorting, setPage, setPageSize] =
        useSuspenseObservableQueryWithPaging(LiveItems, new Paging(0, 20));
    // ...
}
```

## Error Types

When a query fails the hook throws one of two typed errors, both of which extend `Error`.

### QueryFailed

Thrown when the server returns `hasExceptions: true`. Carries the server-side exception details.

| Property | Type | Description |
| -------- | ---- | ----------- |
| `exceptionMessages` | `string[]` | One or more exception messages from the server. |
| `exceptionStackTrace` | `string` | The full server-side stack trace. |

### QueryUnauthorized

Thrown when the server returns `isAuthorized: false`.

## Using the Proxy Static Methods

Generated query proxies expose `useSuspense()` and `useSuspenseWithPaging()` static methods that forward directly to the hooks above, giving the same ergonomics as the existing `use()` method.

```typescript
// Regular query via proxy
function ItemList() {
    const [result, perform] = AllItems.useSuspense();
    return <ul>{result.data.map(i => <li key={i.id}>{i.name}</li>)}</ul>;
}

// With arguments
function FilteredList() {
    const [result] = AllItems.useSuspense({ filter: 'active' });
    return <ul>{result.data.map(i => <li key={i.id}>{i.name}</li>)}</ul>;
}

// Observable query via proxy
function LiveFeed() {
    const [result] = LiveItems.useSuspense();
    return <ul>{result.data.map(i => <li key={i.id}>{i.name}</li>)}</ul>;
}
```

## Complete Example

The following example uses `QueryBoundary` — the simplest way to wrap a Suspense component in Arc.

```tsx
import { QueryBoundary, QueryFailed } from '@cratis/arc.react/queries';
import { AllItems } from './generated/queries';

function ItemList() {
    const [result, perform] = AllItems.useSuspense();

    return (
        <>
            <ul>
                {result.data.map(item => <li key={item.id}>{item.name}</li>)}
            </ul>
            <button onClick={perform}>Refresh</button>
        </>
    );
}

export function App() {
    return (
        <QueryBoundary
            loadingFallback={<p>Loading…</p>}
            onError={({ error, isQueryFailed, isQueryUnauthorized, reset }) => (
                <div>
                    {isQueryUnauthorized && <p>You are not authorized to view this data.</p>}
                    {isQueryFailed && (
                        <>
                            <p>Failed to load data:</p>
                            <ul>
                                {(error as QueryFailed).exceptionMessages.map((m, i) => (
                                    <li key={i}>{m}</li>
                                ))}
                            </ul>
                        </>
                    )}
                    <button onClick={reset}>Retry</button>
                </div>
            )}
        >
            <ItemList />
        </QueryBoundary>
    );
}
```

## Comparison with Standard Hooks

| Feature | `useQuery` / `useObservableQuery` | `useSuspenseQuery` / `useSuspenseObservableQuery` |
| ------- | --------------------------------- | ------------------------------------------------- |
| Loading state | `result.isPerforming === true` | Component suspends; `<Suspense fallback>` renders |
| Error state | `result.hasExceptions === true` | Hook throws; `QueryErrorBoundary` catches |
| Authorization failure | `result.isAuthorized === false` | Hook throws `QueryUnauthorized`; `QueryErrorBoundary` catches |
| Requires `<Suspense>` boundary | No | **Yes** (or use `QueryBoundary`) |
| Requires error boundary | No (optional) | **Strongly recommended** (`QueryErrorBoundary` or `QueryBoundary`) |
| Re-run trigger | `perform()` callback | `perform()` callback — clears cache, re-suspends |
| Proxy static method | `.use()` | `.useSuspense()` |

## Cache and Re-Fetching

Suspense hooks maintain a module-level cache so the in-flight Promise and its resolved result survive React's Suspense retry cycle on uncommitted components. When you call `perform()` (or `setSorting()` / `setPage()`) the cache entry is cleared and the component enters a new Suspense cycle.

> **Testing**: Call `clearSuspenseQueryCache()` or `clearSuspenseObservableQueryCache()` in your test teardown to prevent state leaking between tests.

```typescript
import { clearSuspenseQueryCache, clearSuspenseObservableQueryCache } from '@cratis/arc.react/queries';

afterEach(() => {
    clearSuspenseQueryCache();
    clearSuspenseObservableQueryCache();
});
```

