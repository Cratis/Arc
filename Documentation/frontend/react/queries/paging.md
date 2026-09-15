# Paging

Enumerable query proxies expose paging helpers. The backend must implement paging: ordinary automatic pipeline paging uses `IQueryable<T>`, while observable providers can apply paging themselves (for example MongoDB `Observe()` uses the query context's Skip/Limit).

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

For an **ordinary** query, `useWithPaging` returns an extended tuple:

| Index | Name | Type | Description |
| ----- | ---- | ---- | ----------- |
| 0 | `result` | `QueryResultWithState<T>` | Query result including paging metadata |
| 1 | `perform` | `() => Promise<void>` | Re-execute the query |
| 2 | `setSorting` | `(sorting: Sorting) => Promise<void>` | Change sort field and direction |
| 3 | `setPage` | `(page: number) => Promise<void>` | Navigate to a specific page (zero-based) |
| 4 | `setPageSize` | `(pageSize: number) => Promise<void>` | Change number of items per page |

For an **observable enumerable** query the tuple is `[result, setSorting, setPage, setPageSize]`; there is no `perform` delegate. Single-result proxies do not generate paging helpers. Suspense variants preserve these tuple shapes.

### Cached observable subscriptions

Initial paging and sorting are sent when an observable subscription opens. In the current **non-Suspense** hooks with the default retained instance cache, subsequent `setPage`, `setPageSize`, and `setSorting` calls update local settings but reuse the established subscription instead of opening one with new arguments. Do not rely on those setters to navigate or reorder an already subscribed result.

This limitation is separate from backend paging support and does not describe ordinary one-shot queries. Suspense uses separate resource caches; do not infer its behavior from this non-Suspense path. See [query instance caching](./query-instance-caching.md) and verify the chosen hook/provider combination before adding live paging controls.

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
| `MyObservableQuery.useWithPaging(pageSize)` | Initial observable paging; see the cached-subscription limitation above |

Parameterized proxies accept `(pageSize, args?, sorting?)`; parameterless enumerable proxies accept `(pageSize, sorting?)`. Match the generated signature rather than inserting an arguments placeholder for a parameterless query.

## Sorting with Paging

Sorting is independent of paging. The following ordinary-query fragment shows initial sorting and a dynamic change; cached non-Suspense observables have the limitation described above.

```tsx
import { Sorting, SortDirection } from '@cratis/arc/queries';

// Initial sorting
const [result] = AllAccounts.use(new Sorting('name', SortDirection.ascending));

// Change sorting dynamically
const [paged, perform, setSorting] = AllAccounts.useWithPaging(25);
await setSorting(new Sorting('balance', SortDirection.descending));
```

## Important Requirement

Ordinary automatic query-pipeline paging requires `IQueryable<T>`. A materialized `IEnumerable<T>` or `List<T>` does not gain automatic server slicing merely because a paging hook exists. An application/provider may explicitly consume paging context, including for observable `ISubject` results. Verify both returned rows and paging metadata against that provider.

## See Also

- [Core Query Usage](./usage.md)
- [Suspense Queries](./suspense-queries.md)
- [Backend Query Pipeline](../../../backend/queries/query-pipeline.md)
