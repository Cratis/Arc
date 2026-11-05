---
title: Cratis Arc proxies — generate identity details
description: Generate TypeScript model classes from Cratis Arc identity providers so the frontend can read custom identity details with runtime deserialization metadata.
---

Cratis Arc generates TypeScript models for custom identity details when a provider implements `IProvideIdentityDetails<TDetails>`. Use this interface to keep the frontend's details model tied to the C# type instead of maintaining a separate TypeScript definition.

## Overview

The generic argument `TDetails` tells the generator which model the provider returns. With [proxy generation configured](getting-started.md), that model and the types it references join the output alongside commands and queries. You do not need a command or query to reference the details model for it to be discovered.

## The generic interface

Arc provides the following interface. This is an API reference excerpt, not a type to redeclare in your application:

```csharp
public interface IProvideIdentityDetails<TDetails> : IProvideIdentityDetails
    where TDetails : class;
```

The generic interface extends `IProvideIdentityDetails` without adding methods. Your provider still implements the base interface's `Provide` method; `TDetails` supplies the type information used during generation.

If you have not implemented a provider yet, follow the [Identity documentation](../identity/index.md), then return here to connect its details type to the frontend.

## How discovery works

The proxy generator scans the project assemblies it loads for concrete, non-abstract classes implementing `IProvideIdentityDetails<TDetails>`. It extracts `TDetails` from each matching provider and processes it like other complex model types.

By default, the result is a TypeScript class with serialization metadata: `@field` decorators that tell the runtime how to reconstruct its properties from JSON. Plain [interface output](Configuration/emit-interfaces.md) is a separate option, not the default; [library mode](Configuration/library-mode.md) controls which additional types are collected.

## Generated artifacts

For each discovered details type, the default output includes:

- A TypeScript model class matching the C# type's properties, with `@field` metadata for deserialization.
- Models for referenced complex types, subject to the usual type mappings and exclusions.
- An export in the appropriate `index.ts` file when index generation is enabled.

Keep these files generator-owned. When the C# details type changes, regenerate the proxies and compile the frontend rather than editing the generated class.

## Benefits of using the generic interface

| Feature                | `IProvideIdentityDetails`                 | `IProvideIdentityDetails<TDetails>`              |
| ---------------------- | ---------------------------------------- | ------------------------------------------------ |
| Runtime behavior       | Identical                                | Identical                                        |
| Details-type discovery | No generic details type declared         | Generic argument identifies the type to generate |
| Proxy generation       | Provider does not identify a details type | Details type included in generation              |
| Frontend typing        | Handwritten or discovered elsewhere      | Use the generated details model                  |

## Best practices

Use the generic interface when your provider returns a known details type. Keep that type focused on the data the UI needs; the generator produces a model, not a translation of C# methods or business logic. A C# record is one way to represent an immutable details payload.

Identity details are sent to the client. Do not put passwords, tokens or other secrets in them, and enforce authorization on the backend rather than trusting the UI's copy.

## Frontend integration

Use the generated class with the identity system in `@cratis/arc/identity`. For React consumers, pass it as the details constructor when configuring [React identity](../../../frontend/react/identity.md); that guide shows how to read the details and handle loading or absent values.

The class provides both a TypeScript shape for frontend code and runtime field metadata for deserialization. A TypeScript interface alone has no constructor or field metadata, and a type annotation does not guarantee that the backend supplied details.
