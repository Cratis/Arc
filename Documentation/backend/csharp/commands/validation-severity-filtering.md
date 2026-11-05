---
title: Cratis Arc validation severity and warning confirmation
description: Cratis Arc filters validation results by severity. Set server-side blocking rules and ask callers to confirm warnings without bypassing authorization.
---

Cratis Arc validation severity lets a model-bound command stop on warnings, then accept them after the caller confirms. Use `allowedSeverity` to choose which validation-filter failures and `Provide()` control results block execution; keep authorization separate from that choice.

When the threshold permits a validation result, Arc removes it. It does not appear as an advisory message alongside success, and later filters still run and can reject the command. To show a warning before execution, the first request must use a threshold that blocks it.

Choose the server's blocking policy before adding confirmation. The HTTP cap limits how much feedback callers can allow, while a declared blocking policy sets failures they cannot waive. The confirmation flow below requests a more permissive threshold; it does not grant permission to execute.

## Thresholds

| `ValidationResultSeverity` | Numeric value | Meaning |
| --- | --- | --- |
| `Unknown` | 0 | Unclassified result |
| `Information` | 1 | Informational feedback |
| `Warning` | 2 | Acknowledgment-worthy feedback |
| `Error` | 3 | Validation error |

Without an explicit blocking policy or declared read protection `On`, omitting the threshold leaves only `Error` results blocking. When a caller supplies a threshold, results block only when their severity is **greater than** that threshold:

- `Information` blocks warnings and errors.
- `Warning` allows warnings and blocks errors.
- `Unknown` blocks information, warnings, and errors.
- `Error` allows all currently defined severities, including errors.

Pass the threshold to `ICommandPipeline.Execute` or `Validate`, whether Arc creates the service scope or you supply one. For model-bound HTTP endpoints, send the integer in `X-Allowed-Severity`. These endpoints cap it at `Warning` by default and treat non-numeric or out-of-range values as absent.

The HTTP cap does not apply to in-process calls. MVC controller actions have a separate validation path that this header does not configure.

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

The attribute names the lowest severity that blocks. In this example, `Information` blocks information, warnings, and errors. Choosing `Warning` instead would block warnings and errors. `Unknown` also blocks any command with the attribute, so an unclassified failure cannot turn into success. Rejected results keep their original messages and severities.

Derived command types inherit the policy. Arc's built-in `CommandPipeline` enforces it for command filters and `Provide()` during execution, and for filters during pre-flight `Validate`. It applies to in-process calls as well as HTTP. MVC controller-action DTOs use their own validation path; custom `ICommandPipeline` implementations must implement this policy themselves.

Generated TypeScript proxies carry the threshold for local validation and treat warnings as errors when the declared severity is `Warning` or stricter. The server remains authoritative. Its effective threshold is the stricter of the declared policy and the caller's `allowedSeverity` or capped `X-Allowed-Severity`: callers can demand more blocking, never less. MVC caller opt-out behavior does not relax this model-bound policy.

When neither the command nor its base types declare a policy, the command's own assembly can supply one:

```csharp
[assembly: Cratis.Arc.Commands.ModelBound.BlockOnValidationSeverity(Cratis.Arc.Validation.ValidationResultSeverity.Error)]
```

The command or base-type declaration takes precedence over the assembly policy. Generated TypeScript proxies carry the resolved assembly policy too.

## Read protection On supplies an Error floor

A command declaring read protection `On` gets an implicit `Error` blocking floor when no explicit blocking policy applies. Under that floor, `Error` and `Unknown` always block. Callers can still allow `Warning` and `Information`, or request a stricter threshold. The built-in pipeline enforces the floor for filter validation, `Provide()` control results, and pre-flight validation. A validation error returned by `Handle()`, including `Result<TEvent, ValidationResult>`, remains a rejection.

Arc resolves the command's inherited read-protection attribute before the attribute on the command's own assembly. A command-level `[Unprotected]` opts out. The floor follows the declared mode: lowering `ReadProtection:MaximumMode` does not remove it. An explicit command, inherited-base, or assembly `[BlockOnValidationSeverity(...)]` policy takes precedence over the implicit floor.

Under `Off` and `Measure`, validator errors still block over HTTP by default because caller severity is capped at `Warning`. A command or host can opt in to a more permissive cap. Declare `[BlockOnValidationSeverity(ValidationResultSeverity.Error)]` when errors must always block, including for in-process callers. That declaration controls validation rejection; it does not protect against stale reads.

## Allow callers to relax errors

If a command intentionally lets callers waive errors, opt it in to a higher HTTP threshold:

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

The host's `IgnoreCallerAllowedSeverity` setting takes precedence: when `true`, it restores no-header behavior even for an opted-in command. Otherwise, the command opt-in replaces the host's `MaximumCallerAllowedSeverity`. The host maximum defaults to `Warning`; setting it to `Error` also lets callers waive errors on commands without an opt-in.

Neither cap overrides `[BlockOnValidationSeverity]` or the implicit read-protection-On floor in the built-in pipeline. An out-of-range host maximum fails startup validation. An undefined command opt-in fails the command rather than granting a more permissive cap.

## Create a warning

Separate advice the caller may accept from input the command needs. This complete command allocates an identifier without persisting a business record. Its validator warns about long labels and rejects blank ones:

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

Arc maps FluentValidation's `Error`, `Warning`, and `Info` to its own severities, so this validator needs no custom mapping filter.

## Confirm a warning

Use two requests to show a warning before the caller decides whether to continue. The following caller fragments use the command above, `ICommandPipeline pipeline`, and `Cratis.Arc.Validation`. The first request allows `Information`, which blocks warnings and errors before the handler runs:

```csharp
var command = new AllocateBatchIdentifier("A deliberately long batch label");
var result = await pipeline.Execute<Guid>(
    command,
    allowedSeverity: ValidationResultSeverity.Information);
```

The warning remains in `result.ValidationResults`. Show it to the caller and offer a retry only when the result is authorized and exception-free, contains at least one validation result, and every remaining result is a warning. Check `IsAuthorized`, `HasExceptions`, `ValidationResults.Any()`, and `ValidationResults.All(...)`. The `Any()` check matters because `All(...)` alone also accepts an empty collection.

After the caller explicitly confirms, make a new request with the unchanged command:

```csharp
var confirmedResult = await pipeline.Execute<Guid>(
    command,
    allowedSeverity: ValidationResultSeverity.Warning);
```

The second request runs the rules again. If nothing else fails, the command executes and returns its `Guid`. Errors still block, and a declared policy can keep warnings blocking too.

Do not use the default threshold for the first request: it already allows warnings and would run the handler. The UI asks for confirmation, but a client can send the permissive request directly. This flow is not server-enforced proof of consent.

## Scope and limitations

The pipeline applies filtering to command-filter outcomes and during `Provide()` argument resolution. It does **not** re-filter a singular validation value returned by `Handle()` through its response handler. A warning returned from `Handle()` still makes the result invalid, so moving a warning into the handler changes the confirmation behavior.

The built-in response handler recognizes only a singular Arc `ValidationResult`. A validation array returned by `Handle()` is response data, not a collection of validation failures. `Provide()` separately supports `IEnumerable<ValidationResult>` control values.

Within the command-filter chain, authorization filters run first. Ordinary filters continue after permitted validation failures and stop on a blocking failure. Generated-value validation is separate and rejects any returned failure, regardless of this caller threshold. Enforce critical invariants at the operation or storage boundary; validation alone supplies no concurrency or integrity guarantee.

## Security considerations

The default HTTP cap keeps errors blocking unless the command or host explicitly opts in. Do not use validation severity for authentication, authorization, or non-overridable integrity enforcement.

Return an actual authorization verdict from an [authorization filter](./command-filters.md#cross-cutting-authorization-by-namespace). `CommandResult.Unauthorized` is independent of severity filtering. Check permissions before performing work, and enforce atomic state invariants in the service or storage operation that owns the change.

Use [pre-flight validation](./command-validation.md) for early feedback without running `Provide()` or `Handle()`. For client-side rules and warning handling, continue to [frontend severity filtering](../../../frontend/core/validation/severity-filtering.md).
