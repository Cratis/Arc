# Core Query Usage

Use generated query proxies to retrieve data in React with strong typing and predictable state.

## Proxy Generation

Arc generates query proxies from backend queries (controller-based and model-bound). These proxies expose static React helpers such as `.use()` and `.useWithPaging()`.

See [Backend Proxy Generation](../../../backend/proxy-generation/index.md) for setup details.

## Basic Query Hook

From a React component, call the generated static `use()` method:

```typescript
export const MyComponent = () => {
    const [accounts, queryAccounts] = AllAccounts.use();

    return (
        <>
        </>
    );
};
```

All results are strongly typed from backend metadata.

## Return Tuple

Use the tuple shape emitted for your backend result. Omitting trailing elements while destructuring is fine; treating the tuple itself as the model is not.

| Proxy shape | `.use()` / `.useSuspense()` | `.useWithPaging()` / `.useSuspenseWithPaging()` |
| --- | --- | --- |
| Ordinary single or enumerable | `[result, perform, setSorting]` | Enumerable only: `[result, perform, setSorting, setPage, setPageSize]` |
| Observable enumerable | `[result, setSorting]` | `[result, setSorting, setPage, setPageSize]` |
| Observable single | `[result]` | Not generated |

Parameterized proxies emit `NameParameters`, not `INameParameters`. Their calls take `args` first and, for enumerables, optional `sorting` next. Parameterless enumerable calls take `sorting` first; parameterless single-result calls take no arguments. Paging prepends `pageSize` to these argument positions. For example, `BooksForAuthor.use({ authorId })` passes a named argument object, whereas parameterless `AllBooks.use(sorting)` does not take an `undefined` argument placeholder.

Observable queries differ and are covered in [Observable Queries](./observable-queries.md).

## QueryResultWithState

The query result type in React is `QueryResultWithState`. It implements the same `IQueryResult` contract as `QueryResult` and adds React-focused execution state.

Properties from the shared `IQueryResult` contract:

| Property              | Description                                                                   |
| --------------------- | ----------------------------------------------------------------------------- |
| `data`                | The actual data returned in the expected type.                                |
| `isSuccess`           | Whether the query completed successfully.                                     |
| `isReady`             | Whether the query has produced a result. `false` is transient, not a failure. |
| `isAuthorized`        | Whether the query was authorized.                                             |
| `isValid`             | Whether the query input was valid.                                            |
| `validationResults`   | Validation errors returned by the backend.                                    |
| `hasExceptions`       | Whether exceptions were returned.                                             |
| `exceptionMessages`   | Exception messages from the backend.                                          |
| `exceptionStackTrace` | Exception stack trace when available.                                         |
| `paging`              | Paging metadata (page, size, totals) when paging is enabled.                  |

React execution state:

| Property       | Description                                   |
| -------------- | --------------------------------------------- |
| `isPerforming` | Whether the query is currently fetching data. |

### Readiness and performing state

An observable query HTTP request can return `202 Accepted` with `isReady: false` before the observable has produced its first result. This is a transient server state: `isSuccess` is false, but `hasExceptions` remains false. Inspect `isReady` before treating an unsuccessful result as a failure.

`isReady` and `isPerforming` describe different things. Readiness says whether a result has been produced; performing says whether the client is currently fetching or subscribing. Initial React query state is not ready, while disabled or synthetic failure states are ready because no server result remains pending.

`IQueryResult.isReady` is optional for source compatibility: older Arc servers and third-party structural query-result objects may omit it. Arc normalizes query results received through its HTTP, WebSocket, and Server-Sent Events transports to `isReady: true` when the member is absent, while preserving an explicit `false`. SDK-produced `QueryResult` and `QueryResultWithState` instances always expose a concrete boolean.

## Query Parameters

Queries can expose parameters through backend attributes (for example `[FromQuery]` and route parameters). These become typed arguments on generated proxies.

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

Use the parameterized proxy from React:

```typescript
export const MyComponent = () => {
    const [accounts, queryAccounts] = StartingWith.use({ filter: '' });

    return (
        <>
        </>
    );
};
```

## HTTP Headers

Ordinary query fetches use `httpHeadersCallback`. Native EventSource/WebSocket handshakes cannot attach those arbitrary headers; SSE control POSTs are separate fetch requests. Cookies are browser-managed, not configurable `Cookie` request headers. See the [transport credentials matrix](../arc.md#http-headers-callback).

## See Also

- [Paging](./paging.md)
- [Suspense Queries](./suspense-queries.md)
- [Conditional Queries](./conditional-queries.md)
- [Observable Queries](./observable-queries.md)
