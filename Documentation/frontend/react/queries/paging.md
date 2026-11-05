---
title: "React query paging: navigate server-side results"
description: "Page and sort collection results in React with Cratis Arc query helpers, server-side slicing, paging metadata and matching cached subscriptions."
---

Use a generated query's `useWithPaging(pageSize)` helper to load a collection one page at a time and connect it to navigation controls. The hook sends the requested page to the backend; it does not slice a full result set in the browser.

## How Paging Works

The backend must apply the requested paging. For `IQueryable<T>` results, Arc's query pipeline counts the matching items, applies sorting, then uses `.Skip()` and `.Take()` for the selected page. Observable providers can apply paging themselves; MongoDB `Observe()`, for example, reads paging from the query context and applies Skip/Limit.

Generated collection proxies expose `useWithPaging` and `useSuspenseWithPaging`. Before wiring up the controls, check that your backend supports the flow in [Controller-based Paging](../../../backend/csharp/queries/controller-based/paging.md) or [Model-bound Paging](../../../backend/csharp/queries/model-bound/paging.md).

## Enabling Paging

Replace `use` with `useWithPaging` and pass the number of items per page. This example focuses on navigation and assumes a generated, non-observable `AllAccounts` proxy whose items have `id`, `name`, and `balance`. Adjust the import to your proxy output path and add the [loading and failure handling](./usage.mdx) used by the rest of your screen:

```tsx
import { AllAccounts } from './Accounts/AllAccounts';

export const AccountList = () => {
    const [result, perform, setSorting, setPage, setPageSize] = AllAccounts.useWithPaging(25);

    return (
        <>
            <table>
                <thead>
                    <tr><th>Name</th><th>Balance</th></tr>
                </thead>
                <tbody>
                    {result.data.map(account => (
                        <tr key={String(account.id)}>
                            <td>{account.name}</td>
                            <td>{account.balance}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
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

For an ordinary query, `useWithPaging` returns the result followed by controls for refreshing, sorting and navigation:

| Index | Name | Type | Description |
| ----- | ---- | ---- | ----------- |
| 0 | `result` | `QueryResultWithState<T>` | Query result including paging metadata |
| 1 | `perform` | `() => Promise<void>` | Re-execute the query |
| 2 | `setSorting` | `(sorting: Sorting) => Promise<void>` | Change sort field and direction |
| 3 | `setPage` | `(page: number) => Promise<void>` | Navigate to a specific page (zero-based) |
| 4 | `setPageSize` | `(pageSize: number) => Promise<void>` | Change number of items per page |

An observable collection query returns `[result, setSorting, setPage, setPageSize]`. It receives updates through its subscription, so there is no `perform` delegate. Suspense variants keep the corresponding tuple shape. Single-result proxies do not generate paging helpers.

### Cached observable subscriptions

In non-Suspense observable hooks, changing page, page size or sort order selects a cache entry for those settings. If it needs a new subscription, that subscription sends the settings to the server. Consumers with matching queries, arguments and settings share one subscription; consumers on different pages do not.

The selected entry supplies the visible result. If it has no cached result, the hook shows the query's default value with `isReady: false` and `isPerforming: true`, rather than the previous page's rows. Returning while an entry is retained reuses its cached result and live subscription without subscribing again.

Ordinary paged queries also select a matching entry when these settings change, but fetch again. They expose matching cached data with `isPerforming: true` while the new request runs. Suspense uses separate resource caches; see [query instance caching](./query-instance-caching.md) for the sharing and retention rules.

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
| `MyObservableQuery.useWithPaging(pageSize)` | Observable paging with a subscription for the selected page and sort order |

Parameterized proxies accept `(pageSize, args?, sorting?)`; parameterless enumerable proxies accept `(pageSize, sorting?)`. Match the generated signature rather than inserting an arguments placeholder for a parameterless query.

## Sorting with Paging

Choose a sort order that makes the page sequence meaningful for your view. Changing sorting keeps the current page number; call `setPage(0)` as well if your controls should return to the first page. The change fetches the matching ordinary-query result or selects the matching observable subscription.

These ordinary-query fragments show initial sorting and a dynamic change:

```tsx
import { Sorting, SortDirection } from '@cratis/arc/queries';

// Initial sorting
const [result] = AllAccounts.use(new Sorting('name', SortDirection.ascending));

// Change sorting dynamically
const [paged, perform, setSorting] = AllAccounts.useWithPaging(25);
await setSorting(new Sorting('balance', SortDirection.descending));
```

## Important Requirement

For automatic ordinary-query paging, return `IQueryable<T>`. A materialized `IEnumerable<T>` or `List<T>` does not gain server-side slicing merely because its generated proxy has a paging helper. If your application or observable provider handles paging explicitly, it must use the paging context and supply the corresponding totals. Check both the returned rows and `result.paging` before relying on the navigation controls.

## See Also

- [Core Query Usage](./usage.mdx)
- [Suspense Queries](./suspense-queries.md)
- [Backend Query Pipeline](../../../backend/csharp/queries/query-pipeline.md)
