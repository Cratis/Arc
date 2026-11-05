---
title: Cratis Arc proxies — generate model interfaces
description: Generate plain TypeScript model interfaces with Cratis Arc when consumers need object shapes, without constructors or deserialization metadata.
---

Cratis Arc can generate TypeScript model interfaces for consumers that need object shapes without runtime constructors or deserialization metadata. Use this output for models you build and read as plain objects; keep class output for models that Arc clients deserialize.

By default, the generator emits models as classes with `@field` decorators:

```ts
import { field } from '@cratis/fundamentals';

export class Grid extends Panel {
    @field(RowDefinition, true)
    rows!: RowDefinition[];
}
```

The decorator tells `@cratis/fundamentals` that `rows` contains `RowDefinition` instances. Its `JsonSerializer` uses that information to reconstruct the array's objects from JSON.

Emit-interfaces mode produces the same model shape as a TypeScript interface:

```ts
export interface Grid extends Panel {
    rows: RowDefinition[];
}
```

The interface keeps the model's documentation, base type and property-type imports. It has no constructor or decorators, so it does not need the decorator import from `@cratis/fundamentals`. Property types can still require imports from that package, as shown below.

The flag changes model output only. Command and query proxies still use their own templates and runtime dependencies; this is not a way to turn those clients into dependency-free interfaces.

## Enabling it

With [proxy generation configured](../getting-started.md), add this property inside your existing project:

```xml
<PropertyGroup>
    <CratisProxiesEmitInterfaces>true</CratisProxiesEmitInterfaces>
</PropertyGroup>
```

Regenerate the models and check that their declarations use `export interface` instead of `export class`, as in the `Grid` example.

### CLI

If you use the [direct executable](output-behavior.md#direct-executable), pass `--emit-interfaces`. The following command uses the documented `proxygenerator` alias and also enables library mode. Choose a generated-only output directory: the executable deletes that directory by default.

```bash
proxygenerator assembly.dll output-path --library-mode --emit-interfaces
```

## When to use it

Interface output suits a shared model package whose consumers construct plain objects and use TypeScript to check their shape. It removes the runtime dependency introduced by model decorators, which helps when the package must avoid that dependency.

Keep class output for models used by command, query or identity clients that require runtime constructors and Fundamentals' `JsonSerializer` metadata. An interface is erased from JavaScript, so it cannot replace a model constructor or supply the decorators needed to reconstruct nested values.

## What it does not remove

Interface output removes the model class and its decorators, not the types used by its properties. A model with a `TimeOnly` property still imports the TypeScript representation of that C# type:

```ts
import { TimeOnly } from '@cratis/fundamentals';

export interface ScheduleTriggerSourceSyntax extends TriggerSourceSyntax {
    at: TimeOnly;
}
```

Here, `TimeOnly` describes `at`; it is not a decorator import. Check the generated property imports before treating the model package as dependency-free.

## Combining with library mode

Use [library mode](library-mode.md) when the shared models are not referenced by an endpoint. Library mode decides which types the generator collects; emit-interfaces decides how collected models are rendered. Neither option enables the other.

Enabling emit-interfaces alone also changes models reached through commands and queries. For a shared library, configure both options in that library's generation setup so its shape-only output stays separate from runtime client models.
