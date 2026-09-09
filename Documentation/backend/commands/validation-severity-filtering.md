---
title: Validation severity filtering
description: Select warning thresholds without mistaking overridable validation for authorization.
---

Some rules need acknowledgment rather than unconditional rejection. Model-bound Arc commands accept an `allowedSeverity` threshold for validation filters and `Provide()` control results. Results that the threshold permits are **removed**, not returned as advisory messages alongside success.

## Thresholds

| `ValidationResultSeverity` | Numeric value | Meaning |
| --- | --- | --- |
| `Unknown` | 0 | Unclassified result |
| `Information` | 1 | Informational feedback |
| `Warning` | 2 | Acknowledgment-worthy feedback |
| `Error` | 3 | Validation error |

With no explicit threshold, only `Error` results remain. With a threshold, only results whose severity is **greater than** the threshold remain. Thus:

- `Information` blocks warnings and errors.
- `Warning` allows warnings and blocks errors.
- `Unknown` blocks information, warnings, and errors.
- `Error` allows all currently defined severities, including errors.

`ICommandPipeline.Execute` and `Validate` accept the threshold in both scope-free and scope-explicit forms. Model-bound HTTP endpoints read its integer value from `X-Allowed-Severity`. Controller actions use their own MVC validation path; do not assume this header configures MVC.

## Create a warning

This complete command only allocates an identifier. Its validator warns about unusually long labels and rejects blank labels:

```csharp
using System;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using FluentValidation;

[Command]
public record AllocateBatchIdentifier(string Label)
{
    public Guid Handle() => Guid.NewGuid();
}

public class AllocateBatchIdentifierValidator : CommandValidator<AllocateBatchIdentifier>
{
    public AllocateBatchIdentifierValidator()
    {
        RuleFor(command => command.Label).NotEmpty();
        RuleFor(command => command.Label)
            .MaximumLength(20)
            .WithMessage("A shorter label is easier to read")
            .WithSeverity(Severity.Warning);
    }
}
```

Arc already maps FluentValidation's `Error`, `Warning`, and `Info` to its own severities. Do not add a custom mapping filter.

## Confirm a warning

These are two separate caller fragments, using the command above, `ICommandPipeline pipeline`, and `Cratis.Arc.Validation`. First submit with a threshold that **blocks** warnings:

```csharp
var command = new AllocateBatchIdentifier("A deliberately long batch label");
var result = await pipeline.Execute<Guid>(
    command,
    allowedSeverity: ValidationResultSeverity.Information);
```

The handler does not run and the warning remains in `result.ValidationResults`. Show it to the user. Only if the result is authorized, exception-free, has at least one validation result, and all remaining results are warnings should you offer a warning-only retry. Check `IsAuthorized`, `HasExceptions`, `ValidationResults.Any()`, and `ValidationResults.All(...)`; `All(...)` alone also accepts an empty collection.

After the user explicitly confirms, make a **new** request with the unchanged command:

```csharp
var confirmedResult = await pipeline.Execute<Guid>(
    command,
    allowedSeverity: ValidationResultSeverity.Warning);
```

The rules run again. If nothing else fails, the command executes and returns its `Guid`. With the default threshold, the first request would already execute, so it cannot implement this confirmation flow. Confirmation is a UI convention, not server-enforced proof of consent.

## Scope and limitations

Filtering applies after command filters and during `Provide()` argument resolution. It does **not** re-filter the singular validation value returned by `Handle()` through its response handler. A returned warning from `Handle()` therefore still makes that result invalid. Do not move a warning into a handler expecting this confirmation workflow to work unchanged.

The built-in response handler recognizes only a singular Arc `ValidationResult`. A validation array returned by `Handle()` is response data, not a collection of validation failures. `Provide()` separately supports `IEnumerable<ValidationResult>` control values.

There is also a current ordering limitation: the filter chain stops on its first unsuccessful result **before** the pipeline applies severity filtering. If that result contains only subsequently allowed warnings, execution may resume without running later ordinary filters. Do not assume every filter ran just because execution continued. Authorization filters run first, but critical invariants still need enforcement at the operation/storage boundary; a validator is not a concurrency or integrity guarantee.

## Security considerations

> [!WARNING]
> The model-bound HTTP endpoint currently accepts any parsable integer in `X-Allowed-Severity`, including `3` and larger. A caller can therefore remove Error-severity results from the filtered stages. Do not use validation severity for authentication, authorization, or non-overridable integrity enforcement.

Return an actual authorization verdict from an [authorization filter](./command-filters.md#cross-cutting-authorization-by-namespace). `CommandResult.Unauthorized` is independent of severity filtering. Check permissions before performing work, and enforce atomic state invariants in the service or storage operation that owns the change.

Continue with [pre-flight validation](./command-validation.md) or [frontend severity filtering](../../frontend/core/validation/severity-filtering.md).
