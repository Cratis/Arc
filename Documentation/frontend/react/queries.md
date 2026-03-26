# Queries

Queries in the frontend is divided into the following:

- The underlying `IQueryFor<>`, `IObservableQueryFor<>` interfaces
- The React hooks; `useQuery()` and `useObservableQuery()` for standard usage, and `useSuspenseQuery()` / `useSuspenseObservableQuery()` for [React Suspense](./suspense-queries.md) boundaries
- Proxy generator that generates TypeScript from the C# code to leverage the constructs.

## HTTP Headers

Queries automatically include any HTTP headers provided by the `httpHeadersCallback` configured in the [Arc](./arc.md). This enables you to dynamically include authentication cookies, authorization tokens, or other custom headers with every query request without manual configuration for each query.

## Proxy Generation

Starting with the latter; the [proxy generator](../../backend/proxy-generation/index.md) you'll get the queries generated directly to use
in the frontend. The proxies generated can be imported directly into your code.

## Query

From a React component you can now use the static `use()` method:

```typescript
export const MyComponent = () => {
    const [accounts, queryAccounts] = AllAccounts.use();

    return (
        <>
        </>
    )
};
```

> Note: All data resulting from a query will be strongly typed based on the metadata provided by the proxy generator.

### Return tuple

If the query is a regular request / response type of query, the tuple returned contains two elements.
If it is an observable query, it only returns the first element of the tuple.

The return values are:

- The query result
- Delegate for issuing the query again

#### QueryResultWithState

The query result returned is a type called `QueryResultWithState` this is a sub type of `QueryResult`
adding properties that are relevant when working in React.

From the base `QueryResult` one gets the following properties:

| Property | Description |
| -------- | ----------- |
| data     | The actual data returned in the type expected. |
| isSuccess | Boolean telling whether or not the query was successful or not. |
| isAuthorized | Boolean telling whether or not the query was authorized or not. |
| isValid | Boolean telling whether or not the query was valid or not. |
| ValidationResult | Collection with any validation errors. |
| hasExceptions | Boolean telling whether or not the query had exceptions or not. |
| exceptionMessages | Collection with any exception messages. |
| exceptionStackTrace | The stack trace for the exception if there was one. |
| paging | Contains paging information, with current page number, page size, total number of items and total number pages |

On top of this `QueryResultWithState` adds the following properties:

| Property | Description |
| -------- | ----------- |
| hasData  | Boolean indicating whether or not there is data in the result. |
| isPerforming | Boolean that is true when an operation is working to get data from the server. |

### Parameters

Queries can have parameters they can be used for instance for filtering.
Lets say you have a query called `StartingWith`:

```csharp
[HttpGet("starting-with")]
public IEnumerable<DebitAccount> StartingWith([FromQuery] string? filter)
{
    var filterDocument = Builders<DebitAccount>
        .Filter
        .Regex("name", $"^{filter ?? string.Empty}.*");

    return _collection.Find(filterDocument).ToList();
}
```

The `filter` parameter will be part of the generated proxy, since it has the `[FromQuery]`
attribute on it. Using the proxy requires you to now specify the parameter as well:

```typescript
export const MyComponent = () => {
    const [accounts, queryAccounts] = StartingWith.use({ filter: '' });

    return (
        <>
        </>
    )
};
```

> Note: Route values will also be considered parameters and generated when adorning
> a method parameter with `[HttpPost]`.

## Suspense-Compatible Hooks

Arc also provides Suspense-compatible variants of these hooks — `useSuspenseQuery()` and `useSuspenseObservableQuery()` — that integrate with React's `<Suspense>` and `ErrorBoundary` patterns, letting components declaratively describe loading and error states without inspecting `isPerforming` or `hasExceptions` manually.

See [Suspense Queries](./suspense-queries.md) for details and examples.

## Paging

When the backend query returns `IQueryable<T>`, the query pipeline applies server-side paging automatically — appending `.Skip()` and `.Take()` at the database level so only the requested page of data is fetched. The generated TypeScript proxy includes `useWithPaging` and `useSuspenseWithPaging` methods.

### Enabling paging

Use `useWithPaging` instead of `use`, passing the desired page size:

```tsx
export const AccountList = () => {
    const [result, perform, setSorting, setPage, setPageSize] = AllAccounts.useWithPaging(25);

    return (
        <>
            <DataTable value={result.data}>
                <Column field="name" header="Name" />
                <Column field="balance" header="Balance" />
            </DataTable>
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

### Return tuple for paged queries

The `useWithPaging` hook returns an extended tuple:

| Index | Name | Type | Description |
| ----- | ---- | ---- | ----------- |
| 0 | `result` | `QueryResultWithState<T>` | The query result including paging metadata |
| 1 | `perform` | `() => Promise<void>` | Re-execute the query |
| 2 | `setSorting` | `(sorting: Sorting) => Promise<void>` | Change sort field and direction |
| 3 | `setPage` | `(page: number) => Promise<void>` | Navigate to a specific page (zero-based) |
| 4 | `setPageSize` | `(pageSize: number) => Promise<void>` | Change the number of items per page |

### Paging metadata

Paging information is available on `result.paging`:

| Property | Type | Description |
| -------- | ---- | ----------- |
| `page` | `number` | Current zero-based page number |
| `size` | `number` | Items per page |
| `totalItems` | `number` | Total items across all pages |
| `totalPages` | `number` | Total number of pages |

### All hook variants with paging

| Hook | Description |
| ---- | ----------- |
| `MyQuery.useWithPaging(pageSize)` | Standard query with paging |
| `MyQuery.useSuspenseWithPaging(pageSize)` | Suspense-compatible with paging |
| `MyObservableQuery.useWithPaging(pageSize)` | Observable query with paging |

Each variant accepts an optional `args` parameter (for filtered queries) and an optional `sorting` parameter.

### Sorting

Sorting is independent of paging and works with all query hooks. Pass a `Sorting` instance to the hook or use the `setSorting` callback:

```tsx
import { Sorting, SortDirection } from '@cratis/arc/queries';

// Initial sorting
const [result] = AllAccounts.use(undefined, new Sorting('name', SortDirection.ascending));

// Change sorting dynamically
const [result, perform, setSorting] = AllAccounts.use();
await setSorting(new Sorting('balance', SortDirection.descending));
```

> **Important**: Automatic paging requires the backend to return `IQueryable<T>`. If the backend returns `IEnumerable<T>` or `List<T>`, the paging parameters are sent but ignored — all rows are returned in a single response.


## Observable Query Transport

By default, observable queries connect through the centralized hub endpoints. Two props on the `<Arc>` component control this behaviour.

### `queryTransportMethod`

Selects the transport protocol used for the hub connection.

| Value | Description |
|-------|-------------|
| `QueryTransportMethod.ServerSentEvents` | SSE hub — one `EventSource` per query, routed through `/.cratis/queries/sse` (default). |
| `QueryTransportMethod.WebSocket` | WebSocket — connects to the per-query WebSocket URL. |

```tsx
import { Arc } from '@cratis/arc.react';
import { QueryTransportMethod } from '@cratis/arc/queries';

export const App = () => (
    <Arc
        microservice="my-app"
        queryTransportMethod={QueryTransportMethod.ServerSentEvents}
    >
        <MyRoutes />
    </Arc>
);
```

### `queryDirectMode`

Controls whether observable queries connect directly to each query's own URL or route through the centralized hub.

- When `false` (default): queries are routed through the centralized hub endpoint (`/.cratis/queries/sse` or `/.cratis/queries/ws` depending on `queryTransportMethod`).
- When `true`: each observable query opens its own connection directly to the per-query URL, bypassing the hub entirely. Useful during local development or when connecting to services that do not expose the centralized hub.

```tsx
import { Arc } from '@cratis/arc.react';

export const App = () => (
    <Arc
        microservice="my-app"
        queryDirectMode={true}
    >
        <MyRoutes />
    </Arc>
);
```

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `queryTransportMethod` | `QueryTransportMethod` | `ServerSentEvents` | Transport used for connections. |
| `queryDirectMode` | `boolean` | `false` | When `true`, bypasses the hub and connects directly per query. |

See [Observable Query Multiplexing](./observable-query-multiplexing.md) for full details on transport selection, direct mode, and connection count configuration. See [Observable Query Hub](../../backend/queries/observable-query-hub.md) for server-side protocol reference and keep-alive settings.

## Conditional Queries

Every generated query and observable query class exposes a `when(condition)` static method. When `condition` is `false` the underlying hook is a no-op — no request is made and `QueryResultWithState.empty()` is returned — while still calling the hook unconditionally so React's rules of hooks are never violated.

```tsx
// Subscription only starts once authorId is available
const [feed] = LiveFeed.when(!!authorId).use({ author: authorId ?? '' });
```

See [Conditional Queries](./conditional-queries.md) for the full guide including paging, Suspense, memoization, and the raw `isEnabled` parameter on the underlying hooks.

