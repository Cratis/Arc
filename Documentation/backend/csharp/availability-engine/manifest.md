---
title: The availability manifest
description: Read what a model declares about availability as data with IAvailabilityManifest, and expand any gate into its alternatives with Alternatives().
---

The availability manifest is a description of what a model declares: the audiences, the `availability` blocks, the gates,
the context values and the personas. It is data only. The engine never reads it to evaluate a gate, so a host can show it,
report it or compare it without affecting what is available. It needs no Arc and no hosting package.

## Reading the manifest

Inject `IAvailabilityManifest` and read `Current`:

```csharp
public class AvailabilityReport(IAvailabilityManifest manifest)
{
    public IEnumerable<string> DeclaredAudienceNames() =>
        manifest.Current.Audiences.Select(_ => _.Name.Value);
}
```

`InMemoryAvailabilityManifest` is the ready-made implementation. Its parameterless constructor holds
`AvailabilityManifest.Empty`, which declares nothing.

| Member of `AvailabilityManifest` | Meaning |
| --- | --- |
| `Origin` | Where the manifest comes from, for example the model file. Empty when unknown. |
| `Audiences` | `AudienceDeclaration`s: name, description and source. |
| `Blocks` | `AvailabilityBlockEntry`s: name, description, source and the block's alternatives. |
| `Gates` | `AvailabilityGateEntry`s: the guarded target and the gate's alternatives. |
| `ContextValues` | `ContextValueEntry`s: name, type, origin, scope and whether it is required. |
| `Personas` | `PersonaEntry`s: name and the audiences it is given. |

Every collection is immutable. Text that the model does not give is an empty string, never `null`.

## Alternatives

A gate is stored in disjunctive normal form: a list of `AvailabilityAlternative`s, any one of which opens it. An alternative
lists the audiences that must all be held and the `when` conditions, as authored, that must all hold.

| Gate | Alternatives |
| --- | --- |
| `for Admin` | `Admin` |
| `for Admin or Staff` | `Admin`, `Staff` |
| `for Admin and Beta` | `Admin` and `Beta` |
| `for (A or B) and C` | `A` and `C`, `B` and `C` |
| `when X` | the condition `X` only |
| `with Block` | the alternatives of `Block`, expanded |

Expand any expression yourself with `AvailabilityExpression.Alternatives()`. When the expression uses `with`, pass the
catalog that defines the blocks:

```csharp
var alternatives = expression.Alternatives(catalog);
```

Without a catalog, an expression that uses `with` throws `InvalidOperationException`. A block that is not defined throws
`UndefinedAvailabilityException`, and blocks that build on each other in a cycle throw `CircularAvailabilityException`.
Repeated alternatives are removed and the authored order is kept.

## Building entries from the engine types

`AvailabilityBlockEntry.From`, `AvailabilityGateEntry.From` and `ContextValueEntry.From` build entries from the engine's own
`AvailabilityDefinition`, `AvailabilityExpression` and `ContextValueDeclaration`. `DeclaredAudiences` also accepts
`AudienceDeclaration`s, and `AvailabilityDefinition` takes an optional `Description`. The existing constructors keep working
unchanged.
