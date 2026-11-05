---
title: Cratis Arc command preflight validation
description: Check Cratis Arc command input before submission with local or server validation, without running the handler or treating preflight as authorization.
---

Use Cratis Arc command preflight validation to check input before submission without running the command's `Handle()` method. It gives you feedback while someone edits a form or before you ask them to confirm an action. Choose local checks for immediate feedback, or call the server when the check needs backend rules. Neither reserves state or guarantees that a later execution will succeed.

## How it works

`command.validateClientSide()` performs local checks and returns a `CommandResult` synchronously. It runs the attached client validator and checks required non-route properties for `null` or `undefined`. Presence checks alone do not reject empty strings, unchecked checkboxes, invalid email syntax, or out-of-range numbers; those need validation rules.

`command.validate()` starts with the same local checks. A blocking local failure returns without a network request. Otherwise, it posts to the command's `/validate` endpoint and returns a `CommandResult`.

The backend resolves the handler, builds command context, and runs command filters, including authorization and validation, before filtering the results by severity. It skips handler execution and handler-argument resolution, including the normal `Provide()` path. Filters and validators still run application code: keep them free of unintended side effects, because they can access services and throw even though the handler does not run.

## Basic usage

This illustrative helper accepts an already configured and populated generated command. It works wherever you use the core runtime, without React hooks:

```typescript
import { ICommand } from '@cratis/arc/commands';

export async function inspectCommand<TContent, TResponse>(command: ICommand<TContent, TResponse>) {
    const result = await command.validate();
    if (!result.isValid) {
        console.log(result.validationResults);
    } else if (!result.isAuthorized) {
        console.log('Not authorized');
    } else if (result.hasExceptions) {
        console.log('Validation could not complete');
    }
    return result;
}
```

The helper reports the reason validation could not pass; it does not submit the command. Both validation and execution return [CommandResult](./command-result.md), but validation does not produce a handler response. Use `result.isSuccess` for the overall outcome, then check the result of `execute()` separately when you submit.

Read errors from `validationResults`, not a `validationErrors` object. Each result's severity is a numeric enum, and the command result's `correlationId` is a Fundamentals `Guid` in the client. The [validation-result shape](../validation/results.md) describes the fields you can use for feedback.

## Security considerations

Do not grant access based on a preflight status flag. A locally invalid result means server authorization did not run, and an upstream authentication challenge can return before an Arc result envelope exists. Not every denial arrives as a parseable 401/403 command result.

Skipping the handler does not make returned data safe to disclose. Review validators, filters, custom state, and exception messages for private data that could reach the caller. State can also change between validation and submission: `execute()` validates again, and its final result is authoritative.

If your workflow asks someone to confirm warnings, handle severity explicitly. Plain preflight is not guaranteed to return server warnings; follow [severity filtering](../validation/severity-filtering.md) rather than treating a successful preflight as proof that there are none.

## React usage

When calling server validation while a user types, debounce requests and discard responses for older edits. Track an edit revision or compare the current field values; `hasChanges` is not a request version. This matters whether you manage the UI yourself or use React hooks.

In React, [CommandForm](../../react/command-form/index.md) manages form lifecycle and feedback. For direct hook usage, follow [React command validation](../../react/commands/validation.md).

For endpoint details, continue with [backend command validation](../../../backend/csharp/commands/command-validation.md) and [command filters](../../../backend/csharp/commands/command-filters.md).
