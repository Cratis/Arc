---
title: Backend Integration
description: How core TypeScript commands map to controller-based and model-bound backend command endpoints through generated proxies.
---

Core commands are designed to align with backend command endpoints and generated proxies.

## Backend Command Styles

Arc supports both backend styles:

- [Controller-based Commands](../../../backend/csharp/commands/controller-based.md)
- [Model-bound Commands](../../../backend/csharp/commands/model-bound/index.md)

Both styles produce strongly typed frontend command proxies.

## Proxy Generation Benefits

- Compile-time type safety
- IDE IntelliSense and navigation
- Automatic regeneration when backend contracts change
- No manual HTTP request wiring

For setup and configuration, see [Backend Proxy Generation](../../../backend/csharp/proxy-generation/index.md).

## React Layer

For higher-level usage patterns in React components, see [React Commands](../../react/commands/index.mdx).
