---
title: Cratis Arc validation severity filtering
description: Cratis Arc filters command validation by severity. Block warnings before asking for confirmation, and keep mandatory rules enforced on the server.
---

Cratis Arc lets a command's UI stop on warnings before asking the user to proceed. The default client behavior allows warnings, so calling `execute()` and then asking for permission can be too late: a warning-only command may already have run. Choose the blocking threshold before the first execution attempt.

## Filtering logic

`command.execute(allowedSeverity?, ignoreWarnings?)` filters client validation results before sending a request. Without a declared `blockOnValidationSeverity` policy, it uses these rules:

| Choice | Blocking client results |
| --- | --- |
| Omitted severity | Error only |
| `Warning` | Error only |
| `Information` | Warning and Error |
| `Unknown` | Information, Warning, and Error |
| `ignoreWarnings: true` | Error only; overrides the severity choice |

The numeric enum is `Unknown = 0`, `Information = 1`, `Warning = 2`, `Error = 3`. An explicit severity blocks results greater than the allowed level. Passing `Unknown` therefore blocks Information, Warning, and Error, but does not itself block Unknown results.

A generated proxy can also carry `blockOnValidationSeverity`, the minimum severity that must block execution. With that policy, results at or above its level block, Unknown results also block, and neither argument can relax the policy. A caller can still choose a stricter threshold.

When a request is sent, the severity argument becomes the `X-Allowed-Severity` header; `ignoreWarnings: true` also sends `X-Ignore-Warnings: true`. The C# HTTP endpoint caps caller severity at Warning unless the command or host explicitly opts in. Do not pass Error as an override: without a declared client policy, it can filter out all client errors even though the server can still reject them.

Keep mandatory business, integrity, and security requirements server-enforced and non-overridable. Authorization is a separate pipeline decision, not a warning the user can dismiss.

## Warning confirmation workflow

To ask only when warnings exist, first execute with Information allowed. That attempt can run the command immediately when there are no blocking results; it is not a read-only check. Offer confirmation only when the attempt failed with a nonempty set of Warning results, no exceptions, and authorized status.

The helper below takes an already populated command and your application's confirmation function. Keep the draft unchanged while it runs so the user confirms the same content that will be submitted. The second attempt allows Warning results, but cannot override a declared policy that requires warnings to block:

```typescript
import { ICommand, CommandResult } from '@cratis/arc/commands';
import { ValidationResultSeverity } from '@cratis/arc/validation';

export async function executeWithWarningConfirmation<TContent, TResponse>(
    command: ICommand<TContent, TResponse>,
    confirm: (messages: string[]) => Promise<boolean>
): Promise<CommandResult<TResponse>> {
    const result = await command.execute(ValidationResultSeverity.Information);
    const warningsOnly = !result.isSuccess
        && result.isAuthorized
        && !result.hasExceptions
        && result.validationResults.length > 0
        && result.validationResults.every(item => item.severity === ValidationResultSeverity.Warning);

    if (!warningsOnly) return result;
    const confirmed = await confirm(result.validationResults.map(item => item.message));
    if (!confirmed) return result;
    return command.execute(ValidationResultSeverity.Warning);
}
```

A local warning can stop the attempt before the server checks authorization. The local `isAuthorized` value therefore does not prove that the server granted access; the final execution must still pass server authorization. An empty validation array with an authorization failure is not a warning-only result.

If the user must confirm every write, ask before the first execution, regardless of warnings. This helper is for warning confirmation, not a universal preflight check.

## Why validate is not a warning-preview replacement

`validate()` can stop at client rules without contacting the server. When it does reach the server, preflight uses its default severity filtering, so the call is not guaranteed to return every warning or prove server authorization. Validators and filters are application code and must independently avoid side effects. Use [preflight validation](../commands/validation.md) to understand what runs before the handler and what its result establishes.

## Server behavior and boundaries

After capping caller severity, the C# command pipeline applies its blocking-result policy. Opting in to a higher caller severity cannot remove a declared blocking policy or the Error blocking floor supplied by read protection set to On. Server policies and generated client rules can differ, so inspect the final result. A successful preflight or a confirmed warning does not reserve state or guarantee that a later execution will succeed.

Use warnings only for genuinely overridable advice, not data integrity, access control, or regulatory requirements. See [backend severity filtering](../../../backend/csharp/commands/validation-severity-filtering.md) for the authoritative pipeline behavior and [command filters](../../../backend/csharp/commands/command-filters.md) for application-owned checks.
