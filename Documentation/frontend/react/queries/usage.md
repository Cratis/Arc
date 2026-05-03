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

For standard request/response queries, the tuple contains two elements:

- Query result
- Delegate for issuing the query again

Observable queries differ and are covered in [Observable Queries](./observable-queries.md).

## QueryResultWithState

The query result type in React is `QueryResultWithState`, which extends `QueryResult` with React-focused state.

From `QueryResult`:

| Property | Description |
| -------- | ----------- |
| `data` | The actual data returned in the expected type. |
| `isSuccess` | Whether the query completed successfully. |
| `isAuthorized` | Whether the query was authorized. |
| `isValid` | Whether the query input was valid. |
| `validationResult` | Validation errors returned by the backend. |
| `hasExceptions` | Whether exceptions were returned. |
| `exceptionMessages` | Exception messages from the backend. |
| `exceptionStackTrace` | Exception stack trace when available. |
| `paging` | Paging metadata (page, size, totals) when paging is enabled. |

Additional properties from `QueryResultWithState`:

| Property | Description |
| -------- | ----------- |
| `hasData` | Whether the result currently holds data. |
| `isPerforming` | Whether the query is currently fetching data. |

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

Queries automatically include HTTP headers from the `httpHeadersCallback` configured in [Arc](../arc.md). This lets you attach auth tokens, cookies, or custom headers for every query call without per-query setup.

## See Also

- [Paging](./paging.md)
- [Suspense Queries](./suspense-queries.md)
- [Conditional Queries](./conditional-queries.md)
- [Observable Queries](./observable-queries.md)
