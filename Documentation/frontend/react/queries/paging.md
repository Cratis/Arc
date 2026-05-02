# Paging

Arc supports server-side paging for query proxies when the backend query returns `IQueryable<T>`.

## How Paging Works

When the backend returns `IQueryable<T>`, the query pipeline applies `.Skip()` and `.Take()` on the server so only the requested page is fetched. Generated proxies expose `useWithPaging` and `useSuspenseWithPaging` for this flow.

For backend implementation details, see [Controller-based Paging](../../../backend/queries/controller-based/paging.md) and [Model-bound Paging](../../../backend/queries/model-bound/paging.md).

## Enabling Paging

Use `useWithPaging` instead of `use`, passing a page size:

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

## Return Tuple for Paged Queries

`useWithPaging` returns an extended tuple:

| Index | Name | Type | Description |
| ----- | ---- | ---- | ----------- |
| 0 | `result` | `QueryResultWithState<T>` | Query result including paging metadata |
| 1 | `perform` | `() => Promise<void>` | Re-execute the query |
| 2 | `setSorting` | `(sorting: Sorting) => Promise<void>` | Change sort field and direction |
| 3 | `setPage` | `(page: number) => Promise<void>` | Navigate to a specific page (zero-based) |
| 4 | `setPageSize` | `(pageSize: number) => Promise<void>` | Change number of items per page |

## Paging Metadata

Paging information is available on `result.paging`:

| Property | Type | Description |
| -------- | ---- | ----------- |
| `page` | `number` | Current zero-based page number |
| `size` | `number` | Items per page |
| `totalItems` | `number` | Total items across all pages |
| `totalPages` | `number` | Total number of pages |

## Hook Variants with Paging

| Hook | Description |
| ---- | ----------- |
| `MyQuery.useWithPaging(pageSize)` | Standard query with paging |
| `MyQuery.useSuspenseWithPaging(pageSize)` | Suspense-compatible query with paging |
| `MyObservableQuery.useWithPaging(pageSize)` | Observable query with paging |

Each variant accepts optional `args` and optional `sorting`.

## Sorting with Paging

Sorting is independent of paging and works with all query hooks.

```tsx
import { Sorting, SortDirection } from '@cratis/arc/queries';

// Initial sorting
const [result] = AllAccounts.use(undefined, new Sorting('name', SortDirection.ascending));

// Change sorting dynamically
const [paged, perform, setSorting] = AllAccounts.useWithPaging(25);
await setSorting(new Sorting('balance', SortDirection.descending));
```

## Important Requirement

Automatic paging requires the backend to return `IQueryable<T>`. If the backend returns `IEnumerable<T>` or `List<T>`, paging parameters are sent but ignored, and all rows are returned in one response.

## See Also

- [Core Query Usage](./usage.md)
- [Suspense Queries](./suspense-queries.md)
- [Backend Query Pipeline](../../../backend/queries/query-pipeline.md)
