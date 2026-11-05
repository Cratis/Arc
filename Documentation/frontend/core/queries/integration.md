---
title: Cratis Arc queries with generated TypeScript proxies
description: Call Cratis Arc backend queries from TypeScript through generated proxies, read typed results, and manage observable subscriptions without React.
---

Generated Cratis Arc query proxies let you fetch backend data or subscribe to updates from TypeScript without React. Instantiate the proxy for your backend query, configure its destination, and supply typed arguments; the core runtime builds the request and converts the response into model instances.

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

The import path depends on your configured proxy output folder. `perform(...)` returns a query-result envelope: an object containing the data and the outcome of the request, rather than the model alone. The example uses `isSuccess` before reading `data`. For an unsuccessful result, check `isReady`, `isAuthorized`, `isValid`, and `hasExceptions` to decide what to show in your application.

Required request arguments are checked before sending a request. Ordinary queries default to HTTP GET; arguments not consumed by route placeholders go in the query string. Generated validation rules also run before a request, so an invalid result may come from the client without reaching the server.

Reusing a `QueryFor` instance for another valid request aborts its previous HTTP request if it is still in flight. An `AbortError` from that HTTP request rejects the earlier `perform()` promise; discard that superseded attempt rather than treating it as a query failure. Separate query instances do not cancel one another. See [Request cancellation](./validation-and-behavior.md#request-cancellation) for the distinction between cancellation and a failed result.

## Subscribing to updates

For an observable proxy, call `subscribe(callback, args)` to receive query results as the backend emits updates. Keep the returned subscription and call `unsubscribe()` when your service or view no longer needs it. Calling `subscribe()` again on the same query instance disconnects its previous connection; `dispose()` disconnects the instance's current connection.

You can also call `perform(args)` on an observable proxy for a single HTTP result, but that call does not keep listening. If the backend has not produced a value yet, the result can have `isReady: false` and `isSuccess: false` without an exception. Treat that as pending, not as an empty successful result or a crash. See [Validation And Behavior](./validation-and-behavior.md) for readiness and argument checks.

## Proxy Generation Benefits

Regenerate proxies when backend routes, argument types, or result shapes change; do not repair generated files by hand. Configure `apiBasePath` only for an additional deployment prefix, not a duplicate of the generated route. If a gateway selects a service through a header, set `microservice` separately.

For setup and configuration, see [Backend Proxy Generation](../../../backend/csharp/proxy-generation/index.md). Choose the observable transport in [Query Configuration](configuration.md). If you need the React hook lifecycle instead of managing a subscription yourself, continue with [Observable Queries](../../react/queries/observable-queries.md).

## React Layer

React hooks provide lifecycle and state handling on top of the same contracts. See [React Queries](../../react/queries/index.md) rather than starting requests directly during component rendering.

## See also

- [Query Contracts](contracts.md)
- [Query Configuration](configuration.md)
