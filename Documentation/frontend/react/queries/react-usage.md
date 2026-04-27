# React Usage

Queries integrate with React's component lifecycle through static hook methods generated on every query class.
This page covers the day-to-day patterns for regular queries, observable queries, paging, sorting, and
transport configuration.

## Regular Queries

A regular query fires an HTTP GET request once on mount and whenever its arguments change.
Use the generated `.use()` static method in your components:

```typescript
export const MyComponent = () => {
    const [accounts, queryAccounts] = AllAccounts.use();

    return (
        <ul>
            {accounts.data.map(a => <li key={a.id}>{a.name}</li>)}
        </ul>
    );
};
```

### Return Tuple

The `.use()` hook for a regular query returns a two-element tuple:

| Index | Type | Description |
|-------|------|-------------|
| 0 | `QueryResultWithState<T>` | The query result, including data, status flags, and paging info. |
| 1 | `PerformQuery` | A delegate you can call to re-execute the query. |

### Parameters

Queries can accept parameters — for example, a filter string. Backend parameters annotated with
`[FromQuery]` or as route values are included in the generated proxy:

```csharp
[HttpGet("starting-with")]
public IEnumerable<DebitAccount> StartingWith([FromQuery] string? filter) { ... }
```

Pass parameters as the first argument to `.use()`:

```typescript
export const MyComponent = () => {
    const [accounts, queryAccounts] = StartingWith.use({ filter: '' });

    return (<></>);
};
```

> **Note**: Route values are also generated as parameters when the parameter carries `[HttpGet]`/`[HttpPost]` route tokens.

## Observable Queries

An observable query opens a persistent connection to the backend and receives real-time pushes whenever the
underlying data changes. Use `.use()` on an observable query class:

```typescript
export const LiveFeed = () => {
    const [feed] = AllFeedItems.use();

    return (
        <ul>
            {feed.data.map(item => <li key={item.id}>{item.message}</li>)}
        </ul>
    );
};
```

The observable query `.use()` method returns only the `QueryResultWithState<T>` (no re-execute delegate).

## Paging

When the backend returns `IQueryable<T>`, paging is applied server-side. Use `useWithPaging` instead of `use`:

```tsx
export const AccountList = () => {
    const [result, perform, setSorting, setPage, setPageSize] = AllAccounts.useWithPaging(25);

    return (
        <>
            <DataTable value={result.data} />
            <p>
                Page {result.paging.page + 1} of {result.paging.totalPages}
                ({result.paging.totalItems} total items)
            </p>
            <button
                disabled={result.paging.page === 0}
                onClick={() => setPage(result.paging.page - 1)}>
                Previous
            </button>
            <button
                disabled={result.paging.page >= result.paging.totalPages - 1}
                onClick={() => setPage(result.paging.page + 1)}>
                Next
            </button>
        </>
    );
};
```

### Return Tuple for Paged Queries

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | `result` | `QueryResultWithState<T>` | Query result including paging metadata. |
| 1 | `perform` | `() => Promise<void>` | Re-execute the query. |
| 2 | `setSorting` | `(sorting: Sorting) => Promise<void>` | Change sort field and direction. |
| 3 | `setPage` | `(page: number) => Promise<void>` | Navigate to a specific page (zero-based). |
| 4 | `setPageSize` | `(pageSize: number) => Promise<void>` | Change the number of items per page. |

### Paging Metadata

Paging information is available on `result.paging`:

| Property | Type | Description |
|----------|------|-------------|
| `page` | `number` | Current zero-based page number. |
| `size` | `number` | Items per page. |
| `totalItems` | `number` | Total items across all pages. |
| `totalPages` | `number` | Total number of pages. |

> **Important**: Automatic paging requires `IQueryable<T>` from the backend. `IEnumerable<T>` or `List<T>` returns all rows regardless of paging parameters.

## Sorting

Sorting works with all query hooks. Pass a `Sorting` instance or use the `setSorting` callback:

```tsx
import { Sorting, SortDirection } from '@cratis/arc/queries';

// Initial sort
const [result] = AllAccounts.use(undefined, new Sorting('name', SortDirection.ascending));

// Dynamic sort
const [result, perform, setSorting] = AllAccounts.use();
await setSorting(new Sorting('balance', SortDirection.descending));
```

### Hook Variants

| Hook | Description |
|------|-------------|
| `MyQuery.use(args?, sorting?, isEnabled?)` | Standard query. |
| `MyQuery.useWithPaging(pageSize, args?, sorting?, isEnabled?)` | Standard query with paging. |
| `MyObservableQuery.use(args?, sorting?, isEnabled?)` | Observable query. |
| `MyObservableQuery.useWithPaging(pageSize, args?, sorting?, isEnabled?)` | Observable query with paging. |

Suspense variants are documented in [Suspense Queries](../suspense-queries.md).

## Observable Query Transport

By default, observable queries connect through the centralized hub endpoint. Two props on the `<Arc>` component
control the transport behaviour.

### `queryTransportMethod`

| Value | Description |
|-------|-------------|
| `QueryTransportMethod.ServerSentEvents` | SSE hub — default. Routes through `/.cratis/queries/sse`. |
| `QueryTransportMethod.WebSocket` | WebSocket — connects to the per-query WebSocket URL. |

```tsx
import { Arc } from '@cratis/arc.react';
import { QueryTransportMethod } from '@cratis/arc/queries';

export const App = () => (
    <Arc microservice="my-app" queryTransportMethod={QueryTransportMethod.ServerSentEvents}>
        <MyRoutes />
    </Arc>
);
```

### `queryDirectMode`

When `true`, each observable query opens its own connection directly to the per-query URL, bypassing the
centralized hub. Useful during local development or against services that do not expose the hub.

```tsx
<Arc microservice="my-app" queryDirectMode={true}>
    <MyRoutes />
</Arc>
```

See [Observable Query Multiplexing](../observable-query-multiplexing.md) for full transport details.

## See Also

- [Queries Overview](./index.md)
- [Query Scope](./scope.md) — Track performing state across multiple queries.
- [Suspense Queries](../suspense-queries.md) — Suspense and ErrorBoundary integration.
- [Conditional Queries](../conditional-queries.md) — The `when(condition)` pattern.
- [Change Stream](../change-stream.md) — Fine-grained delta streaming.
