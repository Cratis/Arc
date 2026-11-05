---
title: "Cratis Arc React queries: read application data"
description: "Read backend data in React with generated Cratis Arc query hooks, then add paging, conditional requests, live updates and loading indicators."
---

Cratis Arc query hooks read backend data into React components through generated TypeScript proxies. Use an ordinary query for a request and response, or an observable query to keep a view updated as the backend sends new results.

Start with [core query usage](./usage.mdx) to call a generated proxy and handle its result. Once that read works, choose the next step below for the screen you are building.

## Capabilities

| When you need to… | What to use | Guide |
| ---------- | -------------- | ---------- |
| Configure how queries reach your backend | `<Arc />` props for transport, headers and stream transfer mode | [Configuration](./configuration.md) |
| Load data and handle loading or failure | `use()`, arguments and `QueryResultWithState` | [Core Query Usage](./usage.mdx) |
| Navigate a large result set | `useWithPaging`, page/sort callbacks and server paging metadata | [Paging](./paging.md) |
| Keep a view updated | Observable subscriptions over SSE, WebSocket or polling | [Observable Queries](./observable-queries.md) |
| Find out why a live query is not behaving as expected | Cache, transport and ownership snapshots | [Observable Query Diagnostics](./observable-query-diagnostics.md) |
| Let React boundaries handle loading and errors | `useSuspense()` with query and error boundaries | [Suspense Queries](./suspense-queries.md) |
| Wait for an argument or user selection | `when(condition)` to control automatic execution | [Conditional Queries](./conditional-queries.md) |
| React to individual collection changes | `useChangeStream()` and change deltas | [Change Stream](./change-stream.md) |
| Share connections across live queries | Hub routing and connection pool settings | [Observable Query Multiplexing](./observable-query-multiplexing.md) |
| Reuse data when returning to a view | Shared instances, subscriptions and cache retention | [Query Instance Caching](./query-instance-caching.md) |
| Show one loading indicator for a section | Query scope callbacks for local activity | [Query Scope](./scope.md) |

## Frontend Layering

The generated proxies use the query runtime beneath the React hooks. If you need to call queries outside React or inspect those contracts, continue with [Frontend Core Queries](../../core/queries/index.md).

## Backend References

If you are implementing the query as well as its React view, start with the backend overview and choose the query style used by your application:

- [Backend Queries Overview](../../../backend/csharp/queries/index.md)
- [Controller-based Queries](../../../backend/csharp/queries/controller-based/index.md)
- [Model-bound Queries](../../../backend/csharp/queries/model-bound/index.md)
- [Query Pipeline](../../../backend/csharp/queries/query-pipeline.md)
- [Backend Proxy Generation](../../../backend/csharp/proxy-generation/index.md)
