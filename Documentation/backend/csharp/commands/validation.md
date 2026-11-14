---
title: Cratis Arc command validation with annotations or FluentValidation
description: Cratis Arc validates model-bound command input before handling. Use property annotations or FluentValidation to return field feedback to callers.
---

Cratis Arc validates model-bound command input before `Provide()` and `Handle()` run, so callers can correct rejected fields before the handler performs work. Use property annotations for simple field rules or FluentValidation when a rule needs application policy or several conditions.

Blocking results stop execution and appear in `CommandResult.ValidationResults`. Member names are camelCase field paths: `Name` becomes `name`, and `Items[0].Name` becomes `items[0].name`. Results returned later from `Provide()` or `Handle()` use the same member naming.

Validation supplies input feedback, not permission to execute. Use authorization for access control, and read [severity filtering](./validation-severity-filtering.md) before relying on a validation rule that must not be waived. A pre-flight check also cannot guarantee that state stays unchanged before execution.

## Data annotations

On a positional record, use `property:` to put annotations on the generated properties. Arc's command filter reads those properties, not constructor-parameter metadata.

The following complete command allocates a `Guid` without persisting a business record:

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

At the default severity threshold, an empty `Label` or an out-of-range `Copies` stops the command with validation errors before `Handle()`. Omitting `property:`, as in `[Required] string Label`, leaves the annotation on the constructor parameter where this filter cannot use it. [Controller-based commands](./controller-based.md) use MVC record binding, which has different metadata rules.

If a field uses `ConceptAs<T>`, a typed wrapper around a value, generated validation metadata lets annotations validate its underlying `.Value`. This supports `[Required]`, `[Range]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`, and `[RegularExpression]`. Add `[Required]` when absence itself must fail: a nullable concept otherwise passes rules that tolerate absence. Annotated model-bound query parameters use the same generated value readers.

Use FluentValidation for concept properties on types without generated metadata, including types from other assemblies that lack it and types the generator skips. Their annotations receive the concept object instead of its value. On that fallback path, `[StringLength]` on a non-null concept throws rather than returning field feedback.

## FluentValidation

Derive a FluentValidation validator from `CommandValidator<T>`. Arc discovers it among the application's types, so you do not normally register each validator individually.

This alternative validates the same `AllocateLabeledIdentifier` command. It keeps the label and copies rules, then adds a maximum label length without changing the handler:

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

If you choose FluentValidation for these rules, remove the matching annotations so the caller does not receive duplicate feedback. The [proxy generator](../proxy-generation/validation.md) extracts supported rules for early client feedback. Keep the server validator: a client-side field check does not replace service dependencies or custom server logic.

## Validator dependencies

When a rule needs application policy, accept a registered service in the validator's constructor. Arc resolves it from the same command scope used for `Provide()` and `Handle()` dependencies. This alternative validator takes a length-limit policy; it does not read storage:

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

Register the policy in the host so Arc can construct the validator. The following registration fragment assumes `Microsoft.Extensions.DependencyInjection` is imported:

```csharp
builder.Services.AddSingleton(new LabelPolicy(80));
```

If Arc cannot resolve a non-nullable dependency, the command returns an exception outcome instead of field feedback. Check the host registration when construction fails. A nullable parameter may receive null when no value can be resolved; use it only when the rule deliberately handles absence, not to hide a missing required service.

If the rule needs stored state, supply a service that performs the lookup or configure a read-model provider. Arc.Core does not assume projected event state or provide persistence for arbitrary dependency types. Follow [use current state in a command](../../../scenarios/use-current-state-in-a-command.mdx) for provider-specific resolution. [Chronicle read-model injection](../chronicle/read-models/injecting-into-commands.md) requires the optional integration; it is not the default dependency model.

## Reading the tenant and caller

Use the `IValidationContext` extension methods `GetTenant()`, `GetCausedBy()` and `IsSystem()` when a rule needs the current tenant or caller without a constructor dependency. A tenant identifies the customer or organization whose operation is running.

The following FluentValidation fragment assumes an `ArchiveDocument` command with a `Reason` property. For Arc command discovery, use `CommandValidator<ArchiveDocument>` as the base class, as in the earlier examples. The caller check supplies feedback only: enforce signed-in access with an [authorization filter](./command-filters.md#cross-cutting-authorization-by-namespace), not this overridable validation message:

```csharp
using Cratis.Arc.Validation;
using FluentValidation;

public class ArchiveDocumentValidator : AbstractValidator<ArchiveDocument>
{
    public ArchiveDocumentValidator() =>
        RuleFor(command => command.Reason)
            .Must((command, reason, context) => context.GetCausedBy() is { IsAnonymous: false, IsSystem: false })
            .WithMessage("Only signed-in people can archive documents.");
}
```

Arc gives each command or query one snapshot of its tenant and caller, shared by filters, generated values and child validators (`SetValidator`, `RuleForEach`, `ChildRules`). A nested command or query gets its own snapshot; when it completes, the outer snapshot becomes current again. Values resolve on first read, so switching tenant or principal inside a validator does not change values already read from the snapshot.

The snapshot also determines when these reads can succeed:

- Reading outside Arc's command or query pipeline (a bare `ValidatorInvoker.Invoke`, a custom `IValidatorInvoker`, or MVC controller model validation) throws `OperationSnapshotUnavailable`. The operation is then unauthorized (403), and no allowed validation severity can hide it.
- `GetTenant()` returns the resolved tenant, or `TenantId.NotSet` when none was resolved. It is membership-verified only when an `ITenantMembership` hook is registered; see [tenant membership](../tenancy/membership.md). When the caller's membership is denied, or the caller's claims were changed in place after the operation began, the read fails closed and the operation is unauthorized.
- `GetCausedBy()` is Arc's view of the caller; an anonymous caller is `CausingIdentity.Anonymous`. Chronicle resolves event authorship separately, so do not use this validation-context value as the event's audit record.

## Choosing a rejection phase

Keep a field rule in a validator when the caller needs it during pre-flight checks, before execution. A decision that needs data available only during handling belongs later, but pre-flight cannot report that decision.

- Use a validator for input feedback before data provisioning and execution.
- Use `Provide()` to fetch handler data after filters; it can return validation control values to short-circuit. See [provide data to a command](../../../scenarios/provide-data-to-a-command.mdx).
- Use a singular `ValidationResult` alternative from `Handle()` for a decision made during handling. See [response value handlers](./response-value-handlers.md). A failure returned after a service write does not undo that write.
- Use an [authorization filter](./command-filters.md#cross-cutting-authorization-by-namespace) for access control, not an overridable validation rule.

Check the rule with an invalid command and inspect the returned validation results. At the default threshold, a blocking error prevents handling. Success from the identifier example means a response was allocated; nothing was persisted. If the rule must always block, configure its server-side severity policy rather than relying on the caller's threshold.

Once the validator returns the expected feedback, add [pre-flight command validation](./command-validation.md) to let callers check input without invoking `Provide()` or `Handle()`.
