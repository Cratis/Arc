# Queries

React queries in Arc provide strongly typed data-access patterns for request/response reads, paged views, and live observable streams.

This page is an overview of capabilities. Detailed behavior is documented on the dedicated pages below.

## Capabilities

| Capability | What It Covers | Learn More |
| ---------- | -------------- | ---------- |
| Arc-level configuration | Query-related `<Arc />` props for transport, headers, and stream transfer mode | [Configuration](./configuration.md) |
| Core query usage | `use()` patterns, arguments, and `QueryResultWithState` | [Core Query Usage](./usage.md) |
| Paging and sorting | `useWithPaging`, page/sort callbacks, and paging metadata | [Paging](./paging.md) |
| Observable streams | Real-time subscriptions, transport selection, and direct mode | [Observable Queries](./observable-queries.md) |
| Suspense integration | `useSuspense()` with query boundaries and error boundaries | [Suspense Queries](./suspense-queries.md) |
| Conditional execution | `when(condition)` patterns for safe query activation | [Conditional Queries](./conditional-queries.md) |
| Change deltas | `useChangeStream()` and transfer-mode behavior | [Change Stream](./change-stream.md) |
| Connection behavior | Multiplexing, connection count, and hub routing | [Observable Query Multiplexing](./observable-query-multiplexing.md) |
| Instance lifecycle | Shared query instances and cache behavior | [Query Instance Caching](./query-instance-caching.md) |

## Frontend Layering

For low-level query contracts and non-React runtime behavior, see [Frontend Core Queries](../../core/queries/index.md).

## Backend References

- [Backend Queries Overview](../../../backend/queries/index.md)
- [Controller-based Queries](../../../backend/queries/controller-based/index.md)
- [Model-bound Queries](../../../backend/queries/model-bound/index.md)
- [Query Pipeline](../../../backend/queries/query-pipeline.md)
- [Backend Proxy Generation](../../../backend/proxy-generation/index.md)
