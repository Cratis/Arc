---
title: Identity details type generation
description: Generate runtime model classes from strongly typed identity providers.
---

The proxy generator can automatically discover and generate TypeScript types for your custom identity details. This enables type-safe access to identity information in your frontend application.

## Overview

When you implement a strongly-typed identity details provider using `IProvideIdentityDetails<TDetails>`, the proxy generator will:

1. Discover all implementations of `IProvideIdentityDetails<TDetails>` in your assemblies
2. Extract the `TDetails` type parameter
3. Generate TypeScript representations of these types
4. Include them in the standard proxy output alongside commands and queries

## The generic interface

Arc provides this generic interface declaration (an API reference excerpt, not a type to redeclare in your application):

```csharp
public interface IProvideIdentityDetails<TDetails> : IProvideIdentityDetails
    where TDetails : class;
```

This interface extends the base `IProvideIdentityDetails` interface but adds type information that the proxy generator can discover and use.

For detailed information on implementing identity providers, see the [Identity documentation](../identity/index.md).

## How discovery works

The proxy generator scans all loaded assemblies for classes that:

1. Are concrete (non-abstract) classes
2. Implement `IProvideIdentityDetails<TDetails>`
3. Have a valid type argument for `TDetails`

The `TDetails` type is then processed like any other complex type, generating a corresponding TypeScript **class with serialization metadata** by default. Plain interface output is a separate CLI mode; see [library mode](Configuration/library-mode.md).

## Generated artifacts

For each identity details type discovered, the generator creates:

1. **TypeScript class**: A model matching the C# type structure, with `@field` metadata for deserialization
2. **Nested Types**: Any complex types referenced by the details type
3. **Export entry**: Added to the appropriate `index.ts` file when index generation is enabled

## Benefits of using the generic interface

| Feature                | `IProvideIdentityDetails`        | `IProvideIdentityDetails<TDetails>`              |
| ---------------------- | -------------------------------- | ------------------------------------------------ |
| Runtime behavior       | Identical                        | Identical                                        |
| Details-type discovery | No generic details type declared | Generic argument identifies the type to generate |
| Proxy generation       | No type generated                | TypeScript type generated                        |
| Frontend type safety   | Manual typing required           | Automatic type safety                            |

## Best practices

1. **Use the Generic Interface**: Always prefer `IProvideIdentityDetails<TDetails>` over the base interface to enable proxy generation.

2. **Keep Types Simple**: Identity details types should be simple DTOs without complex logic or dependencies.

3. **Avoid Sensitive Data**: Never include sensitive information like tokens or passwords in identity details.

4. **Use Records**: Consider using C# records for immutable identity detail types.

## Frontend integration

The generated TypeScript types can be used with the identity system in `@cratis/arc/identity`. The types ensure that your frontend code has compile-time type safety when accessing identity details.

Pass the generated class as the details constructor when configuring [React identity](../../frontend/react/identity.md). A TypeScript interface alone carries no runtime field metadata. Identity details are client-visible UI data, not an authorization boundary.
