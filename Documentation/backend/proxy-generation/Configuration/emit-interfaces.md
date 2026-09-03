# Emit Interfaces

By default the generator emits each type as a **class** carrying `@field` decorators:

```ts
import { field } from '@cratis/fundamentals';

export class Grid extends Panel {
    @field(RowDefinition, true)
    rows!: RowDefinition[];
}
```

The decorators carry runtime type information so `@cratis/fundamentals` can reconstruct an instance from JSON — which is what a command or query proxy needs. They also mean every generated file imports `@cratis/fundamentals`.

**Emit-interfaces mode** produces plain TypeScript interfaces instead:

```ts
export interface Grid extends Panel {
    rows: RowDefinition[];
}
```

Same shape, same documentation, same inheritance, same imports for the types a property refers to — but no class, no decorators, and no import of `@cratis/fundamentals`.

## Enabling it

```xml
<PropertyGroup>
    <CratisProxiesEmitInterfaces>true</CratisProxiesEmitInterfaces>
</PropertyGroup>
```

### CLI

```bash
proxygenerator assembly.dll output-path --library-mode --emit-interfaces
```

## When to use it

Use it for a model that is **built and read but never deserialized** through Fundamentals' `JsonSerializer` — and, in particular, for a package that must not take a runtime dependency. A package deliberately published with no dependencies cannot accept one just to describe its own shapes, which otherwise rules out generating its TypeScript at all and leaves it hand-maintained.

Do **not** use it for command and query proxies. Those are deserialized from HTTP responses, and without the decorators the deserializer has no type information to reconstruct with.

## What it does not remove

Only the decorators and the import they need. A model that uses types **Fundamentals provides** still imports those, because they are the TypeScript representation of the C# types:

```ts
import { TimeOnly } from '@cratis/fundamentals';

export interface ScheduleTriggerSourceSyntax extends TriggerSourceSyntax {
    at: TimeOnly;
}
```

That import is a type, not a decorator, and no emission mode can remove it — the property genuinely is a `TimeOnly`.

## Combining with library mode

The two are usually used together: [library mode](library-mode.md) is what makes the generator emit a model that no command or query references, and emit-interfaces is what lets the result stay dependency-free.

Neither implies the other. Emit-interfaces on its own changes how the types reached from commands and queries are rendered, which is rarely what you want — see the warning above.
