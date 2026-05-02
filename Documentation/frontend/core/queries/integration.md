# Backend Integration

Core queries map directly to backend query endpoints and generated query proxies.

## Backend Query Styles

Arc supports both backend styles:

- [Controller-based Queries](../../../backend/queries/controller-based/index.md)
- [Model-bound Queries](../../../backend/queries/model-bound/index.md)

Both styles generate equivalent frontend proxy ergonomics.

## Proxy Generation Benefits

- Compile-time contract safety
- IntelliSense for query parameters and results
- Automatic sync with backend contract changes
- Consistent wrapper/result handling

For setup details, see [Backend Proxy Generation](../../../backend/proxy-generation/index.md).

## React Layer

For React hook-level patterns (`use`, Suspense, observable, paging), see [React Queries](../../react/queries/index.md).
