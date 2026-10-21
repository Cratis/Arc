---
title: Validation severity filtering
description: Select warning thresholds without mistaking overridable validation for authorization.
---

Some rules need acknowledgment rather than unconditional rejection. Model-bound Arc commands accept an `allowedSeverity` threshold for validation filters and `Provide()` control results. Results that the threshold permits are **removed**, not returned as advisory messages alongside success. Command filters continue past permitted failures, so an error from a later filter still rejects the command.

## Thresholds

| `ValidationResultSeverity` | Numeric value | Meaning |
| --- | --- | --- |
| `Unknown` | 0 | Unclassified result |
| `Information` | 1 | Informational feedback |
| `Warning` | 2 | Acknowledgment-worthy feedback |
| `Error` | 3 | Validation error |

For commands without an explicit blocking policy and without declared read protection `On`, omitting the threshold keeps only `Error` results. With a caller threshold, only results whose severity is **greater than** the threshold remain. Thus:

- `Information` blocks warnings and errors.
- `Warning` allows warnings and blocks errors.
- `Unknown` blocks information, warnings, and errors.
- `Error` allows all currently defined severities, including errors.

`ICommandPipeline.Execute` and `Validate` accept the threshold in both scope-free and scope-explicit forms. Model-bound HTTP endpoints read its integer value from `X-Allowed-Severity`. Controller actions use their own MVC validation path; do not assume this header configures MVC.

## Declare a blocking policy on a model-bound command

Mark a model-bound command when warning or information failures must reject it even if the caller supplies a permissive threshold:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

[Command]
[BlockOnValidationSeverity(ValidationResultSeverity.Information)]
public record SubmitApplication(string Name)
{
    public void Handle() { }
}
```

The attribute names the **lowest severity that blocks**: `Information` blocks information, warnings, and errors; `Warning` blocks warnings and errors. `Unknown` results also block any command with the attribute, because an unclassified failure must not become a successful command. The rejected result retains its original message and severity. The declared policy is inherited by derived command types and applies to command filters, `Provide()`, `Validate`, and in-process execution as well as HTTP, when commands run through Arc's built-in `CommandPipeline`. It does not apply to controller-action DTOs, which use the MVC validation path, or to custom `ICommandPipeline` implementations unless they implement the policy themselves. Generated TypeScript proxies carry the threshold for local validation and treat warnings as errors when the declared severity is `Warning` or stricter, but the server remains authoritative.

The effective threshold is the stricter of the declared policy and the caller's `allowedSeverity` or `X-Allowed-Severity`. A caller can demand *more* blocking, never less: passing `Error` or a larger integer does not permit a declared warning or information failure. This deliberately differs from the caller opt-out for controller actions in #21. Commands without the attribute retain caller-controlled filtering unless they declare read protection `On`.

## Read protection On supplies an Error floor

A command declaring read protection `On` gets an `Error` blocking floor unless it declares `[BlockOnValidationSeverity(...)]`. `Error` and `Unknown` always block; callers can still allow `Warning` and `Information`, or demand a stricter threshold. The floor applies to filters, validators, `Provide()` control results, and `Validate` through the built-in pipeline. A validation error returned by `Handle()`, including `Result<TEvent, ValidationResult>`, remains a rejection.

Arc resolves the command's inherited read-protection attribute before the attribute on the command's own assembly. A command-level `[Unprotected]` opts out. The floor follows the **declared** mode: lowering `ReadProtection:MaximumMode` does not remove it. An explicit, possibly inherited, `[BlockOnValidationSeverity(...)]` takes precedence over the implicit floor.

`Off` and `Measure` keep caller-controlled filtering. Without an explicit blocking policy, `X-Allowed-Severity: 3` can skip validator errors and let `Handle()` run. If you rely on validators under `Off`, declare `[BlockOnValidationSeverity(ValidationResultSeverity.Error)]`; this prevents severity filtering from dropping errors, but does not guard stale reads.

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

Authorization filters run first; ordinary filters continue after non-blocking validation failures and stop on a blocking failure. Critical invariants still need enforcement at the operation/storage boundary; a validator is not a concurrency or integrity guarantee.

## Security considerations

> [!WARNING]
> The model-bound HTTP endpoint currently accepts any parsable integer in `X-Allowed-Severity`, including `3` and larger. For commands under `Off` or `Measure` without an explicit blocking policy, a caller can therefore remove Error-severity results from the filtered stages. Do not use validation severity for authentication, authorization, or non-overridable integrity enforcement.

Return an actual authorization verdict from an [authorization filter](./command-filters.md#cross-cutting-authorization-by-namespace). `CommandResult.Unauthorized` is independent of severity filtering. Check permissions before performing work, and enforce atomic state invariants in the service or storage operation that owns the change.

Continue with [pre-flight validation](./command-validation.md) or [frontend severity filtering](../../../frontend/core/validation/severity-filtering.md).
