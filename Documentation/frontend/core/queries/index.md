---
title: Cratis Arc core queries without React
description: Use Cratis Arc core queries directly in TypeScript to fetch typed data or subscribe to updates, with validation, cancellation, and readiness handling.
---

Cratis Arc core queries let TypeScript services, view models, and non-React frontends read backend data through generated classes. Call `perform()` for one result or `subscribe()` on an observable query for updates, without constructing request URLs yourself. Neither requires Chronicle.

Each result carries data alongside authorization, validation, and readiness information. Read those fields before using the data: an observable query that has not produced a result yet is different from a failed query. When you use the classes in `@cratis/arc` directly, you also own request cancellation and subscription cleanup. For React components, [React Queries](../../react/queries/index.md) covers the hooks built on these classes.

Start with [Backend Integration](./integration.md) to call a generated query or subscribe to updates. Then use [Validation And Behavior](./validation-and-behavior.md) to handle missing arguments, superseded requests, and results that are not ready yet.

## Capabilities

| Capability | What it covers | Learn more |
| ---------- | -------------- | ---------- |
| Query contracts | `IQuery`, `IQueryFor`, sorting, paging, and typed execution | [Query Contracts](./contracts.md) |
| Runtime configuration | Microservice routing, API base path, and observable transport mode | [Configuration](./configuration.md) |
| Validation and behavior | Required arguments, validation results, cancellation, and readiness | [Validation And Behavior](./validation-and-behavior.md) |
| Backend integration | Calling generated proxies and managing observable subscriptions | [Backend Integration](./integration.md) |

## Related documentation

- [Validation](../validation/index.md)
- [Backend Queries](../../../backend/csharp/queries/index.md)
- [React Queries](../../react/queries/index.md)
