---
title: 'ARCCHR0015: Command handler must not return a nullable event'
description: Reject an event-producing command explicitly instead of declaring an optional event result.
---

## Rule

A model-bound `[Command]` whose public instance `Handle()` declares a nullable `[EventType]` event receives this warning. It applies to a direct `TEvent?`, `Task<TEvent?>`, `ValueTask<TEvent?>`, and nullable event branches inside `Result<…>` or `OneOf<…>`, including those unions inside an awaitable.

Return `Result<TEvent, ValidationResult>` and reject with `ValidationResult.Error(...)` when the command cannot record its event. You can also reject earlier in a validator or `Provide()`.

A nullable `ICommandOperation` result is a different, supported [optional operation contract](../../commands/operations/index.md). This rule does not report operations, non-event response types, or a nullable error branch beside a non-nullable event.

## Severity

Warning

## Example

These handler fragments assume an application event named `ExpenseSubmitted` with a decimal `Amount` property. See [returning events](../commands/events.md) for integration setup.

### Violation

```csharp
// ARCCHR0015: null would report success without recording an event.
public ExpenseSubmitted? Handle() =>
    Amount > 0 ? new ExpenseSubmitted(Amount) : null;
```

### Explicit rejection

```csharp
using Cratis.Arc.Validation;
using Cratis.Monads;

public Result<ExpenseSubmitted, ValidationResult> Handle() =>
    Amount > 0
        ? new ExpenseSubmitted(Amount)
        : ValidationResult.Error("Amount must be positive", [nameof(Amount)]);
```

The event branch is non-nullable. The rejection branch tells the caller why no event was recorded. For an input-only rule such as this one, a command validator is also appropriate.

## Quick fix

**Return an event or an explicit validation rejection** replaces a supported nullable event response with `Result<TEvent, ValidationResult>`. A literal `null` returned directly or in a conditional return branch becomes `ValidationResult.Error("TODO: explain why")`. Replace the placeholder with a meaningful explanation and, when relevant, the affected member names before using the command.

The fix supports direct event results, async `Task`/`ValueTask` handlers, and two-branch `Result`/`OneOf` responses containing exactly the nullable event and `ValidationResult`. It preserves the awaitable and leaves unrelated null values and nested functions alone. Both the original and edited project must compile.

There is no Fix All. No fix is offered for inherited, virtual, override or abstract handlers, additional union branches, or an edit that fails compilation. Non-async factories such as `Task.FromResult<TEvent?>`, casts, nullable local variables and callers depending on the old signature can require manual changes. Analyzer coverage is broader than fix coverage. The compiler check covers the current project, not downstream callers.

## Analysis boundaries

The analyzer examines declared return types, not whether a handler actually returns null. A handler declared nullable is reported even if every path currently returns an event. It recognizes framework types by identity, follows inherited event metadata, and checks inherited public instance handlers. An inherited handler is reported on the command declaration rather than on its shared base method.

Custom lookalike wrappers, erased `object` results, tuple elements, and collection contents are outside this rule. Nullability on the awaitable itself, such as `Task<TEvent>?`, is not a nullable event branch.

## Why this rule exists

A direct or awaited null event result currently succeeds without appending anything because the command pipeline skips response processing for null. A null event branch inside `Result` or `OneOf` takes a different path: the pipeline unwraps the branch and attempts to inspect the null response's type, producing a `NullReferenceException` failure. Neither outcome gives the caller a meaningful rejection reason.

This warning makes the declared shape visible at build time; it does not change runtime behavior.

## See also

- [Returning events](../commands/events.md)
- [Validation](../../commands/validation.md)
- [Optional command operations](../../commands/operations/index.md)
