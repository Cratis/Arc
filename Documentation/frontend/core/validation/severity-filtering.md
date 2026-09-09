---
title: Validation severity filtering
description: Choose which validation severities block execution and avoid executing warning-only commands before confirmation.
---

Warnings are useful only if the UI knows whether they block the write. Arc's default allows warnings, so calling `execute()` and then asking for permission is too late for a warning-only command.

## Filtering logic

`command.execute(allowedSeverity?, ignoreWarnings?)` applies these client rules and communicates the choice to the server:

| Choice | Blocking client results |
| --- | --- |
| Omitted severity | Error only |
| `Warning` | Error only |
| `Information` | Warning and Error |
| `Unknown` | Information, Warning, and Error |
| `ignoreWarnings: true` | Error only; overrides the severity choice |

The numeric enum is `Unknown = 0`, `Information = 1`, `Warning = 2`, `Error = 3`. Explicit severity retains results **greater than** the allowed level. Do not pass Error as an override: it can filter all client errors. Mandatory business, integrity, and security requirements must remain server-enforced and non-overridable. Authorization is a separate pipeline decision, not merely a warning.

## Warning confirmation workflow

If your rule is “ask only when warnings exist,” first execute with **Information** allowed, not the default. That attempt can execute immediately if there are no warnings/errors; it is an execution attempt, not a read-only preview. When warnings block it, require a nonempty warning-only result, no exceptions, and authorized status before offering an override.

This standalone helper takes an already populated command and an application-owned confirmation function. Freeze the draft while it runs so confirmation applies to the same content:

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

A local warning can short-circuit before server authorization. The local `isAuthorized` value is therefore not proof that the server granted access; the final execution still must pass server authorization. Never interpret an empty validation array plus an authorization failure as “warnings only.”

If the user must confirm **every** write, ask before the first execution, regardless of warnings. Do not use this helper as a universal preflight preview.

## Why validate is not a warning-preview replacement

`validate()` can stop at client rules without contacting the server, and server preflight uses its default severity filtering. It is not guaranteed to return all warnings or prove server authorization. Validators and filters remain application code and must independently avoid side effects. See [preflight validation](../commands/validation.md).

## Server behavior and boundaries

The generated endpoint reads the severity headers and the command pipeline applies its blocking-result policy. Server policies and projected client rules can differ; always inspect the final result. A successful earlier preflight or confirmed warning does not reserve state or guarantee later execution success.

Use warnings only for genuinely overridable advice, not data integrity, access control, or regulatory requirements. See [backend severity filtering](../../../backend/commands/validation-severity-filtering.md) for the authoritative pipeline behavior and [command filters](../../../backend/commands/command-filters.md) for application-owned checks.
