---
title: Evaluating availability gates
description: Build and evaluate availability expressions with for, when and with parts, and read the three-valued result with its reason.
---

A gate is an `AvailabilityExpression`. It has three kinds of part and two combinators.

| Part | Factory | Holds when |
| --- | --- | --- |
| `for` | `AvailabilityExpression.For("Plus", "Preview")` | The request holds any of the audiences. |
| `when` | `AvailabilityExpression.When(condition)` | The `IAvailabilityCondition` holds. |
| `with` | `AvailabilityExpression.With("DataQuality")` | The named `AvailabilityDefinition` holds. |
| `and` | `AvailabilityExpression.AllOf(...)` | Every part holds. |
| `or` | `AvailabilityExpression.AnyOf(...)` | Any part holds. |

Parentheses are the shape of the tree: nest `AllOf` and `AnyOf`.

```csharp
var gate = AvailabilityExpression.AllOf(
    AvailabilityExpression.For("Plus", "Preview"),
    AvailabilityExpression.When(new ContextComparison("vessel.commercialPackage", ComparisonOperator.NotEqual, "Basic")));

var result = await evaluator.Evaluate(gate, request);

if (!result.IsAvailable)
{
    // result.Reason is AvailabilityReasons.NotAvailable
    // result.Causes names the parts, for example "for Plus"
}
```

## Three values

`AvailabilityResult.Outcome` is `Available`, `Unavailable` or `Unknown`. Only `Available` opens a gate.

| Combinator | Result |
| --- | --- |
| `and` | `Unavailable` if any part is, otherwise `Unknown` if any part is, otherwise `Available`. |
| `or` | `Available` if any part is, otherwise `Unknown` if any part is, otherwise `Unavailable`. An empty `or` is `Unavailable`. |

Parts are evaluated in authored order and evaluation stops at the first decisive part. A part after it is not
evaluated, so a failure in it does not surface.

An audience gate is `Unknown` when the request has no resolved audiences. A condition is `Unknown` when a value it reads
is absent, including for `NotEqual` and `NotIn`: an absent value never opens a gate.

## Conditions

`ContextComparison` compares a context path with literals. Its operators are `IsTrue`, `Equal`, `NotEqual`, `In`,
`NotIn`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan` and `LessThanOrEqual`. Numbers of different types compare by
value. Implement `IAvailabilityCondition` for anything else.

## Reusable blocks

An `AvailabilityDefinition` names a block. The lines of a block are combined with `and`, and a block can build on another
with `With`.

```csharp
var catalog = new AvailabilityCatalog()
    .Add(AvailabilityDefinition.FromLines(
        "DataQuality",
        AvailabilityExpression.For("Plus"),
        AvailabilityExpression.When(new ContextComparison("vessel.hasReported", ComparisonOperator.IsTrue))));

catalog.Validate();
```

`Validate` throws `UndefinedAvailabilityException` for a block that is not defined and `CircularAvailabilityException`
for a cycle. Call it at startup. The evaluator throws the same exceptions if it meets them at run time.

## Gates down a path

Gates combine down the path of the model: module, feature, slice, then command, query or screen.
`EvaluatePath(gates, request)` requires every gate to hold, outermost first. No gates means available.

## Failure is an error

The evaluator never turns a failure into a denial. A provider that throws surfaces as
`ContextValueProviderFailedException`, and a required value that is absent as `RequiredContextValueAbsentException`.
Hosts log the first and retry, and refuse the request with the reason of the second.
