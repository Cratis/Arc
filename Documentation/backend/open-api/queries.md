# Queries

Arc queries return results wrapped in a `QueryResult` envelope that carries the data along with paging metadata and error information. The `QueryResultOperationTransformer` ensures this is accurately reflected in the generated API documentation.

## What the transformer does

For every endpoint whose method is identified as a query (not marked with `[AspNetResult]`), the transformer:

1. Replaces the 200 response schema with `QueryResult`.
2. Adds standard error response schemas for 400, 403, and 500 status codes.
3. Appends paging and sorting query parameters when the return type is an enumerable (`IEnumerable<T>`, `IQueryable<T>`, etc.).

## Pagination and sorting parameters

When a query endpoint returns an enumerable result, the following query parameters are added automatically:

| Parameter | Type | Description |
|-----------|------|-------------|
| `sortBy` | `string` | Field name to sort by |
| `sortDirection` | `string` (`asc` \| `desc`) | Sort direction |
| `pageSize` | `integer` | Number of items per page |
| `page` | `integer` | Page number (0-based) |

## Response status codes

| Status code | Meaning |
|-------------|---------|
| 200 | Query executed successfully |
| 400 | Invalid request or parameters |
| 403 | Forbidden — insufficient permissions |
| 500 | Unexpected server error |

## Opting out

Decorate the action with `[AspNetResult]` to bypass the transformer and expose the raw return type directly:

```csharp
[HttpGet]
[AspNetResult]
public Task<IEnumerable<Invoice>> GetInvoices() { ... }
```

