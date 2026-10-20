---
title: Backend Integration
description: Connect core TypeScript queries to controller-based and model-bound Arc endpoints through generated proxies and typed query results.
---

Generated query proxies connect TypeScript callers to Arc backend query endpoints without handwritten request URLs or response deserialization. Use the proxy that matches the backend query declaration, then configure its destination and supply its typed arguments.

## Backend Query Styles

Arc supports both backend styles:

- [Controller-based Queries](../../../backend/csharp/queries/controller-based/index.md) use ASP.NET Core controllers and routing.
- [Model-bound Queries](../../../backend/csharp/queries/model-bound/index.md) put discoverable query methods on a read model.

The backend chooses the route, parameters, and returned model. The proxy generator reads the compiled application and emits the corresponding route, parameter descriptors, model type, and one-shot or observable client. These are endpoint contracts, not a requirement to use Chronicle or event sourcing.

## Calling a generated query

For example, Arc's query-showcase sample declares a model-bound `ById(int id)` query returning one `ShowcaseItem`. Its generated `ById` proxy extends `QueryFor<ShowcaseItem, ByIdParameters>` and declares the route `/api/features/query-showcase/by-id`.

With those generated files available to your frontend:

```typescript
import { ById } from './Features/QueryShowcase/ById';

const query = new ById();
query.setOrigin('https://api.example.com');
const result = await query.perform({ id: 42 });

if (result.isSuccess) {
    console.log(result.data.name);
}
```

The import path depends on your configured proxy output folder. `perform(...)` returns a query-result envelope, not the model alone: inspect success, authorization, validation, and exception information before using the data. Required request arguments are checked before a request is sent. Ordinary queries default to HTTP GET; the core request builder places arguments not consumed by route placeholders in the query string.

## Proxy Generation Benefits

Regenerate proxies when backend routes, argument types, or result shapes change; do not repair generated files by hand. Configure `apiBasePath` only for an additional deployment prefix, not a duplicate of the generated route. If a gateway selects a service through a header, set `microservice` separately.

For setup and configuration, see [Backend Proxy Generation](../../../backend/csharp/proxy-generation/index.md). Observable queries use the streaming client contract instead of a one-shot `perform` call; see [Observable Queries](../../react/queries/observable-queries.md).

## React Layer

React hooks provide lifecycle and state handling on top of the same contracts. See [React Queries](../../react/queries/index.md) rather than starting requests directly during component rendering.

## See also

- [Query Contracts](contracts.md)
- [Query Configuration](configuration.md)
