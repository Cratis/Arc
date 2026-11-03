---
title: Command validation
description: Validate model-bound command input with property annotations or FluentValidation.
---

When input is incomplete, return useful feedback before performing work. Arc's model-bound pipeline runs validation filters before `Provide()` and `Handle()`. Blocking results stop execution and appear in `CommandResult.ValidationResults`; their members are reported as camelCase field paths, including results returned from `Provide()` or `Handle()` (`Name` becomes `name`, and `Items[0].Name` becomes `items[0].name`).

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

For command types with generated validation metadata, annotations on `ConceptAs<T>` properties validate the underlying `.Value`. This includes `[Required]`, `[Range]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`, and `[RegularExpression]`. A nullable concept passes absence-tolerant rules unless you add `[Required]`. Annotated model-bound query parameters use the same generated value readers.

Types from other assemblies without generated metadata, and types skipped by the generator, keep the existing object-validation behavior: annotations receive the concept object rather than its value. In that fallback path, `[StringLength]` on a non-null concept throws instead of producing a validation result. Use a FluentValidation validator for those types.

## FluentValidation

Choose FluentValidation when a rule needs more than a property annotation, such as a service dependency or several conditions. Derive the validator from `CommandValidator<T>`. Arc discovers it with the application's types; no per-validator registration is needed under normal Arc discovery.

Use the same command type for the following alternative. The validator enforces the two field rules and adds a maximum label length; it does not change what the handler does:

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

Remove duplicate annotations if you select FluentValidation for those same rules, so one rejected field does not receive duplicate feedback. The [proxy generator](../proxy-generation/validation.md) extracts supported rules for early client feedback. Keep the server validator even when a client checks the field: services and custom logic do not automatically become equivalent client-side checks.

## Validator dependencies

Supply a registered service through the validator's constructor when the rule needs application policy. Arc resolves it from the command scope, also used for `Provide()` and `Handle()` dependencies. In this example the policy is a length limit, not a storage lookup:

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

Register the policy in the host so discovery can construct the validator. This is a host registration fragment, with `Microsoft.Extensions.DependencyInjection` imported:

```csharp
builder.Services.AddSingleton(new LabelPolicy(80));
```

An unresolved non-nullable dependency produces an exception outcome, not ordinary field feedback. Check the host registration if the rule cannot be constructed. Nullable dependency parameters may receive null when no value can be resolved; use them only when the rule deliberately handles absence. Making a required service nullable conceals a configuration error.

A service that reads storage must actually implement that lookup. Arc.Core does not assume projected event state or supply persistence for arbitrary dependency types. For provider-specific current-state resolution, see [use current state in a command](../../../scenarios/use-current-state-in-a-command.mdx). [Chronicle read-model injection](../chronicle/read-models/injecting-into-commands.md) is an optional integration, not the default validator dependency model.

## Choosing a rejection phase

Keep a field rule in validation when it must also run during pre-flight checks. Put a decision that needs handler-only data in the later phase that owns it, and expect pre-flight to miss that decision.

- Use a validator for input feedback before data provisioning and execution.
- Use `Provide()` to fetch handler data after filters; it can return validation control values to short-circuit. See [provide data to a command](../../../scenarios/provide-data-to-a-command.mdx).
- Use a singular `ValidationResult` alternative from `Handle()` for a decision made during handling. See [response value handlers](./response-value-handlers.md). A failure returned after a service write does not undo that write.
- Use an [authorization filter](./command-filters.md#cross-cutting-authorization-by-namespace) for access control, not an overridable validation rule.

Submit an invalid command and inspect the returned validation results before considering the rule finished. A failure at the default threshold prevents handling; a successful result from the identifier example only means it allocated a response, not that anything was persisted. If a rule must always block, configure its server-side severity policy rather than relying on the caller's threshold.

For early feedback without invoking `Provide()` or `Handle()`, continue to [pre-flight command validation](./command-validation.md).
