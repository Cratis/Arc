---
title: Validation severity filtering
description: Select warning thresholds without mistaking overridable validation for authorization.
---

Model-bound Arc commands use an `allowedSeverity` threshold to decide which validation-filter failures and `Provide()` control results block execution. A permitted result is **removed**; it is not returned as an advisory message alongside success. Later filters still run and can reject the command.

There are two policy questions to look up here: how much feedback a caller may allow, and which failures the server insists on blocking. The HTTP cap limits the first. A declared blocking policy sets the second and cannot be relaxed by the caller. The warning-confirmation recipe below uses caller thresholds; it does not establish permission to execute.

## Thresholds

| `ValidationResultSeverity` | Numeric value | Meaning |
| --- | --- | --- |
| `Unknown` | 0 | Unclassified result |
| `Information` | 1 | Informational feedback |
| `Warning` | 2 | Acknowledgment-worthy feedback |
| `Error` | 3 | Validation error |

Without an explicit blocking policy or declared read protection `On`, an omitted threshold retains only `Error` results. An explicit caller threshold retains severities **greater than** that threshold. The comparison is exclusive:

- `Information` blocks warnings and errors.
- `Warning` allows warnings and blocks errors.
- `Unknown` blocks information, warnings, and errors.
- `Error` allows all currently defined severities, including errors.

`ICommandPipeline.Execute` and `Validate` accept a threshold in both scope-free and scope-explicit forms. Model-bound HTTP endpoints read the integer from `X-Allowed-Severity` and cap it at `Warning` by default. A non-numeric or out-of-range value is treated as absent. That cap belongs to the HTTP boundary, so an in-process call is not automatically capped. Controller actions use the separate MVC validation path; this header does not configure that path.

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

The effective threshold is the stricter of the declared policy and the caller's `allowedSeverity` or capped `X-Allowed-Severity`. A caller can demand *more* blocking, never less. Do not apply MVC caller opt-out behavior to this model-bound policy. When the command and its base types have no declaration, the command's own assembly can supply one:

```csharp
[assembly: Cratis.Arc.Commands.ModelBound.BlockOnValidationSeverity(Cratis.Arc.Validation.ValidationResultSeverity.Error)]
```

The command or base-type declaration takes precedence over the assembly policy. Generated TypeScript proxies carry the resolved assembly policy too.

## Read protection On supplies an Error floor

A command declaring read protection `On` gets an `Error` blocking floor unless it declares `[BlockOnValidationSeverity(...)]`. `Error` and `Unknown` always block; callers can still allow `Warning` and `Information`, or demand a stricter threshold. The floor applies to filters, validators, `Provide()` control results, and `Validate` through the built-in pipeline. A validation error returned by `Handle()`, including `Result<TEvent, ValidationResult>`, remains a rejection.

Arc resolves the command's inherited read-protection attribute before the attribute on the command's own assembly. A command-level `[Unprotected]` opts out. The floor follows the **declared** mode: lowering `ReadProtection:MaximumMode` does not remove it. An explicit, possibly inherited, `[BlockOnValidationSeverity(...)]` takes precedence over the implicit floor.

Under `Off` and `Measure`, validator errors remain blocking over HTTP by default because caller severity is capped at `Warning`. An explicit opt-in can relax them. Declare `[BlockOnValidationSeverity(ValidationResultSeverity.Error)]` when errors must always block, including for in-process callers; it does not guard stale reads.

## Allow callers to relax errors

A command can explicitly allow a higher HTTP caller threshold:

```csharp
[Command]
[AllowCallerValidationSeverity(ValidationResultSeverity.Error)]
public record AcknowledgeAdvice(string Name)
{
    public void Handle() { }
}
```

The opt-in is inherited. It replaces the host maximum, whether higher or lower. Configure the host under `Cratis:Arc:Command` (enum names or numbers are accepted):

```json
{
  "Cratis": {
    "Arc": {
      "Command": {
        "MaximumCallerAllowedSeverity": "Warning",
        "IgnoreCallerAllowedSeverity": false
      }
    }
  }
}
```

Resolve the HTTP cap in this order: host `IgnoreCallerAllowedSeverity` > command opt-in > host `MaximumCallerAllowedSeverity`. Ignoring the header restores no-header behavior even for an opted-in command. The host maximum defaults to `Warning`; setting it to `Error` permits callers to filter errors on commands without an opt-in. Neither setting overrides `[BlockOnValidationSeverity]` or the declared read-protection-On floor in the built-in pipeline. Out-of-range host maxima fail startup validation. An undefined command opt-in fails the command instead of granting a more permissive cap.

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

Use two requests when the user must see and confirm a warning. The following caller fragments use the command above, `ICommandPipeline pipeline`, and `Cratis.Arc.Validation`. The first request selects a threshold that **blocks** warnings, so handling waits for confirmation:

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

The second request runs the rules again. If nothing else fails, the command executes and returns its `Guid`. Do not use the default threshold for the first request: it would already allow the warning and run the handler. The UI offers confirmation, but a client can send the permissive request directly. This flow is not server-enforced proof of consent.

## Scope and limitations

The pipeline applies filtering to command-filter outcomes and during `Provide()` argument resolution. It does **not** re-filter a singular validation value returned by `Handle()` through its response handler. A warning returned from `Handle()` still makes the result invalid, so moving a warning into the handler changes the confirmation behavior.

The built-in response handler recognizes only a singular Arc `ValidationResult`. A validation array returned by `Handle()` is response data, not a collection of validation failures. `Provide()` separately supports `IEnumerable<ValidationResult>` control values.

Within the command-filter chain, authorization filters run first. Ordinary filters continue after permitted validation failures and stop on a blocking failure. Generated-value validation is separate and rejects any returned failure, regardless of this caller threshold. Enforce critical invariants at the operation or storage boundary; validation alone supplies no concurrency or integrity guarantee.

## Security considerations

The default HTTP cap keeps errors blocking unless the command or host explicitly opts in. Do not use validation severity for authentication, authorization, or non-overridable integrity enforcement.

Return an actual authorization verdict from an [authorization filter](./command-filters.md#cross-cutting-authorization-by-namespace). `CommandResult.Unauthorized` is independent of severity filtering. Check permissions before performing work, and enforce atomic state invariants in the service or storage operation that owns the change.

Continue with [pre-flight validation](./command-validation.md) or [frontend severity filtering](../../../frontend/core/validation/severity-filtering.md).
