---
title: Declaring availability with types
description: Declare audiences, availability blocks, conditions, scope markers and provided context values as C# types that map one to one onto Screenplay, and read the rules back into the model.
---

Availability is declared with types. What you write in C# is exactly what a Screenplay model can say, so the code-to-model generator
reads the rules back instead of reporting that they were left in code. A typo is a compile error, because nothing is named with a string.

| Screenplay | C# |
| --- | --- |
| `audience Plus` | `public class Plus : Audience;` |
| `availability DataQuality` | `public class DataQuality : AvailabilityBlock` |
| `for`, `with`, `not`, `when` lines | `For<T>()`, `With<T>()`, `Not<T>()`, `When` in the block |
| `available for Plus` | `[AvailableFor<Plus>]` |
| `available with DataQuality` | `[Available<DataQuality>]` |
| `available when <condition>` | `[AvailableWhen<HasPaidInvoice>]` |
| `available` on a module, feature or slice | a scope marker in its namespace |
| `provide vessel Vessel from query ...` | `[ProvideContext<Vessel>(...)]` |

## Audiences

An audience is a type that derives from `Audience`. Its name is the name of the audience, and its summary is the audience's description
in the model. The type says who the audience is for, never who is in it: membership is granted at run time through the
[audience source](audiences.md).

```csharp
/// <summary>Customers on the Plus commercial package.</summary>
public class Plus : Audience;

/// <summary>Tenants taking part in the preview.</summary>
public class Preview : Audience;
```

Audience types are found with `ITypes`, so there is no list of declared audiences to keep in step.

## Availability blocks

A block is a reusable rule. It is a type, and its lines are written with the operators the language has:

```csharp
/// <summary>Sold with Plus, once the vessel has reported.</summary>
public class DataQuality : AvailabilityBlock
{
    protected override IReadOnlyList<AvailabilityTerm> Lines =>
    [
        For<Plus>(),
        When.On<Vessel>(_ => _.HasReported).IsTrue()
    ];
}
```

All the lines of a block hold. The rest follows the language:

- A line holds one form: `for`, `with` or `when`. `&` between different forms is the same as putting the forms on lines of their own, so
  `For<Plus>() & When...` is two lines. `|` between different forms cannot be stated and the generator reports it rather than guessing.
- `For<T>()` and `With<T>()` combine with `&`, `|` and parentheses. `&` binds tighter than `|`.
- `Not<T>()` applies to one audience. The logic is three-valued: when the audiences cannot be decided the result stays unknown, and unknown never opens a gate.
- A `when` is one condition with its own `&` and `|`. `When.On<Vessel>(_ => _.Score).IsGreaterThan(3) | When.On<Vessel>(_ => _.HasReported).IsTrue()` is one `when`.

A block that builds on another uses `With<Other>()`. A cycle is refused at startup.

## Conditions

There are two forms.

The restricted form compiles to the Screenplay `when` grammar. `When.On<Vessel>(_ => _.CommercialPackage)` reads a property, or a chain of
properties, of a [provided read model](#provided-values), and `Is`, `IsNot`, `IsTrue`, `In`, `NotIn`, `IsGreaterThan`, `IsAtLeast`, `IsLessThan` and
`IsAtMost` compare it with constants. It reads `$context.vessel.commercialPackage`. An enumeration is stated by its member name.

The class form, `When.Condition<HasPaidInvoice>()` in a block or `[AvailableWhen<HasPaidInvoice>]` on a part, is an `IAvailabilityCondition` that can inject services,
for anything a comparison cannot say. The model cannot read the rules inside a class, so it states the condition as opaque: `when $context.hasPaidInvoice == true`,
and a `provide hasPaidInvoice Bool from host` that says the host decides it.

## Gating parts and scopes

Attributes refer to the types:

```csharp
[Command]
[AvailableFor<Plus, Preview>]
public record ResetScore(VesselId Vessel);

[Available<DataQuality>]
public class Feature : AvailabilityScope;
```

Every attribute is a line, and every line has to hold, so `[AvailableFor<Plus>, AvailableFor<Preview>]` needs both. To allow either, name both in one attribute,
`[AvailableFor<Plus, Preview>]` (up to three audiences), or use a block.

A scope marker, a type that derives from `AvailabilityScope`, gates everything in its namespace and the namespaces beneath it. Module, feature and slice names
come from the namespace as the proxy generator reads them, so a marker in the namespace of a feature becomes an `available` line on that feature.

## Provided values

`[ProvideContext<Vessel>(nameof(Vessel.VesselByKey), "arguments.vesselId")]` on a type provides the read model `Vessel` as `$context.vessel` for the namespace of
the type, keyed from the named argument of the command or query being decided. The name is the type name in camelCase. It becomes the Screenplay `provide` line. Set
`Required = false` for a value that may be absent: an absent value is unknown, which never opens a gate. The host registers an `IReadModelQuery` that executes the query.

## Code to model

The generator reads all of it back: `audience`, `availability` blocks with their `for`, `when`, `with` and `not` lines, module, feature and slice gates, and `provide` lines.

```screenplay excerpt
audience Plus
  description "Customers on the Plus commercial package"

availability DataQuality
  description "Sold with Plus, once the vessel has reported"
  for Plus
  when $context.vessel.hasReported == true
```

Only what the language cannot say is reported: a `|` that mixes forms, a block whose `Lines` is not one expression of terms, and a scope marker whose namespace names no part of the model.

## Migrating from the string forms

The string forms still work and are obsolete. Each names something the compiler cannot check and the model cannot read back.

| Before | After |
| --- | --- |
| `[AvailableFor("Plus", "Preview")]` | `[AvailableFor<Plus, Preview>]`, with `public class Plus : Audience;` |
| `[AvailableWith("DataQuality")]` | `[Available<DataQuality>]`, with `public class DataQuality : AvailabilityBlock` |
| `AvailabilityExpression.For`, `With`, `When`, `AllOf`, `AnyOf` | `For<T>()`, `With<T>()`, `When`, `&` and `\|` in an `AvailabilityBlock` |
| `AvailabilityCatalog` and `AvailabilityDefinition` | an `AvailabilityBlock` type, which the catalog is built from |
| `options.GateNamespace("Fleet.DataQuality", expression)` | `[AvailableFor<Plus>] public class Feature : AvailabilityScope;` in that namespace |
| `ContextValueCatalog.Declare(...)` with a `ReadModelContextValueProvider` | `[ProvideContext<Vessel>(...)]` |
| `DeclaredAudiences` | audience types, found with `ITypes` |

`AvailabilityExpression` keeps its tree as the engine's own representation. The factory methods that author an expression with strings are obsolete.
