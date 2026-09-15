---
title: Controller query return types
description: Understand MVC data results, Arc wrapping, observable values, and null handling.
---
<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Data return types

Arc's default MVC GET handling wraps returned data in its nongeneric `QueryResult`. Unlike model-bound discovery, MVC does not require the action to live on the returned type.

| Return value | Default Arc GET behavior |
| --- | --- |
| A model or custom summary | Wrapped as `data` |
| A list, array, or materialized enumerable | Wrapped collection; no built-in slicing |
| `IQueryable<T>` | Renderer applies requested paging/sorting before serialization |
| `Task<T>` | MVC awaits the task before Arc processes its data |
| Runtime `ISubject<T>` or `IAsyncEnumerable<T>` | Streaming adapter chooses handling from the request |

A MongoDB `Find(...)` is fluent query construction; call `ToList()`/`ToListAsync()` to return materialized data. See [dependency injection](dependency-injection.md) for a complete async action.

## Query results

Do **not** return `QueryResult` from an ordinary wrapped controller GET to control the outer metadata. The action filter creates another `QueryResult` and treats the returned wrapper as data, producing a nested contract. A custom envelope is likewise ordinary data, not an instruction to replace Arc's outer envelope.

Prefer [IQueryable paging](paging.md). If you need complete control over status and body, opt out using `Cratis.Arc.AspNetResultAttribute` and return a conventional MVC result. Opting out also skips Arc query rendering/interception; you own the entire response contract and must use a compatible client.

## Nullable return types

A null response from a default Arc-wrapped MVC GET becomes a failed result with `exceptionMessages` containing `"Null data returned"`; it is not ordinary successful absence or an automatic 404. This differs from the [model-bound pipeline](../model-bound/return-types.md#collections-and-absence), where null can remain a success.

Choose deliberately:

- An empty collection means a successful collection with no matches.
- A non-null application response model can represent absence explicitly inside its data contract.
- `[AspNetResult]` with `NotFound()` provides ordinary MVC 404 semantics. The [route example](route-templates.md#bind-a-route-value) shows the complete action.

Do not convert database exceptions into null/empty successful results.

## Observable return types

Return `ISubject<T>` for a subject-backed query. An arbitrary Rx `IObservable<T>` is not sufficient: the controller adapter checks the **runtime value** for subject/async-enumerable support. Merely declaring `IObservable<T>` does not adapt a `Select(...)` result into a subject.

The return type does not establish a WebSocket; the request selects the transport. See [controller observable queries](observable-queries.md) for a complete GET action and subscription lifetime rules.
