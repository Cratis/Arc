# Backend Integration

Core commands are designed to align with backend command endpoints and generated proxies.

## Backend Command Styles

Arc supports both backend styles:

- [Controller-based Commands](../../../backend/commands/controller-based.md)
- [Model-bound Commands](../../../backend/commands/model-bound/index.md)

Both styles produce strongly typed frontend command proxies.

## Proxy Generation Benefits

- Compile-time type safety
- IDE IntelliSense and navigation
- Automatic regeneration when backend contracts change
- No manual HTTP request wiring

For setup and configuration, see [Backend Proxy Generation](../../../backend/proxy-generation/index.md).

## React Layer

For higher-level usage patterns in React components, see [React Commands](../../react/commands/index.md).
