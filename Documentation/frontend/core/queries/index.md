---
title: Queries
description: Use Arc's low-level TypeScript query contracts for ordinary requests and observable subscriptions.
---

Core queries in Arc are the low-level TypeScript and JavaScript primitives used to retrieve data from backend query endpoints. Ordinary queries use HTTP request and response semantics; observable queries subscribe to an emitting backend source. Neither requires Chronicle.

This section focuses on contracts and runtime behavior in `@cratis/arc`. For React hook-based usage, see [React Queries](../../react/queries/index.md).

## Capabilities

| Capability | What it covers | Learn more |
| ---------- | -------------- | ---------- |
| Query contracts | `IQuery`, `IQueryFor`, sorting, paging, and typed execution | [Query Contracts](./contracts.md) |
| Runtime configuration | Microservice routing, API base path, and observable transport mode | [Configuration](./configuration.md) |
| Validation and behavior | Client-side validation, request behavior, and error categories | [Validation And Behavior](./validation-and-behavior.md) |
| Backend integration | Controller-based/model-bound mapping and proxy generation | [Backend Integration](./integration.md) |

## Related documentation

- [Validation](../validation/index.md)
- [Backend Queries](../../../backend/queries/index.md)
- [React Queries](../../react/queries/index.md)
