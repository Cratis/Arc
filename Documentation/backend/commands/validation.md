---
title: Command validation
description: Validate model-bound command input with property annotations or FluentValidation.
---

When input is incomplete, return useful feedback before performing work. Arc's model-bound pipeline runs validation filters before `Provide()` and `Handle()`. Blocking results stop execution and appear in `CommandResult.ValidationResults`.

Validation is not authorization, and a pre-flight check cannot guarantee that state remains unchanged. Read [severity filtering](./validation-severity-filtering.md) before relying on a rule as a non-overridable guard.

## Data annotations

For positional records, explicitly target the generated **properties**. Arc's command filter uses object-property validation, not constructor-parameter metadata.

This complete command allocates a `Guid`; it does not persist a business record:

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands.ModelBound;

[Command]
public record AllocateLabeledIdentifier(
    [property: Required] string Label,
    [property: Range(1, 100)] int Copies)
{
    public Guid Handle() => Guid.NewGuid();
}
```

An empty `Label` or an out-of-range `Copies` produces validation errors before `Handle()`, at the default severity threshold. Constructor annotations such as `[Required] string Label` do not provide the property metadata this filter reads. MVC record binding has different metadata rules; do not apply this advice mechanically to [controller-based commands](./controller-based.md).

## FluentValidation

For richer rules, derive a validator from `CommandValidator<T>`. Arc discovers it with the application's types; no per-validator registration is needed under normal Arc discovery.

This validator is an alternative to the annotations above, using the same command type:

```csharp
using Cratis.Arc.Commands;
using FluentValidation;

public class AllocateLabeledIdentifierValidator : CommandValidator<AllocateLabeledIdentifier>
{
    public AllocateLabeledIdentifierValidator()
    {
        RuleFor(command => command.Label).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Copies).InclusiveBetween(1, 100);
    }
}
```

Remove duplicate annotations if you select FluentValidation for those same rules. The [proxy generator](../proxy-generation/validation.md) extracts supported rules for early client feedback; server rules involving services or custom logic are not automatically equivalent client-side checks.

## Validator dependencies

Validators may accept registered services through their constructors. Arc resolves these from the command scope, also used for `Provide()` and `Handle()` dependencies. This application-level example supplies its complete policy dependency and makes no database assumptions:

```csharp
using Cratis.Arc.Commands;
using FluentValidation;

public record LabelPolicy(int MaximumLength);

public class LabelPolicyValidator : CommandValidator<AllocateLabeledIdentifier>
{
    public LabelPolicyValidator(LabelPolicy policy)
    {
        RuleFor(command => command.Label).MaximumLength(policy.MaximumLength);
    }
}
```

Host registration fragment, with `Microsoft.Extensions.DependencyInjection` imported:

```csharp
builder.Services.AddSingleton(new LabelPolicy(80));
```

Nullable dependency parameters may receive null when no value can be resolved. Non-nullable dependency parameters are required; resolution failure produces an exception outcome instead of silently constructing a validator with null. Use a nullable dependency when absence is a valid input to your rule, not to conceal a missing required service.

A service that reads storage must actually implement that lookup. Arc.Core does not assume projected event state or supply persistence for arbitrary dependency types. For provider-specific current-state resolution, see [use current state in a command](../../scenarios/use-current-state-in-a-command.md). [Chronicle read-model injection](../chronicle/read-models/injecting-into-commands.md) is an optional integration, not the default validator dependency model.

## Choosing a rejection phase

- Use a validator for input feedback before data provisioning and execution.
- Use `Provide()` to fetch handler data after filters; it can return validation control values to short-circuit. See [provide data to a command](../../scenarios/provide-data-to-a-command.md).
- Use a singular `ValidationResult` alternative from `Handle()` for a decision made during handling. See [response value handlers](./response-value-handlers.md). A failure returned after a service write does not undo that write.
- Use an [authorization filter](./command-filters.md#cross-cutting-authorization-by-namespace) for access control, not an overridable validation rule.

For early feedback without invoking `Provide()` or `Handle()`, continue to [pre-flight command validation](./command-validation.md).
