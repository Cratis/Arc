---
title: Command validation
description: Obtain early command feedback without running the handler, while preserving server authorization and filter responsibilities.
---

Use preflight validation to give feedback before submission. It does not execute `Handle()`, reserve state, or guarantee that a later execution will succeed.

## How it works

`command.validate()` first runs the configured client validator and required-property checks. A local failure returns a `CommandResult` **without a network request**. Otherwise it calls the command's validation endpoint.

The backend preflight path resolves the handler, builds command context, and runs command filters, then filters validation results. It skips handler execution and handler-argument resolution, including the normal `Provide()` path. Filters/validators are still application code: they can access services, throw, or have side effects unless you design them not to.

`command.validateClientSide()` performs only local checks and returns synchronously. Required-property presence checks reject null/undefined, not automatically empty strings, false checkboxes, invalid email syntax, or numeric ranges. Those require actual rules.

## Basic usage

This illustrative helper accepts an already configured and populated generated command:

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

Both validation and execution return [CommandResult](./command-result.md). `correlationId` is a Fundamentals `Guid` in the client, validation severity is a numeric enum, and errors are in `validationResults`, not a `validationErrors` object. Validation does not produce a handler response. See the complete [validation-result shape](../validation/results.md).

## Security considerations

- A locally invalid result does not prove server authorization ran. Never grant access based on a preflight status flag alone.
- When reached, the server runs its configured authorization/validation filters. An upstream authentication challenge may return before the Arc result envelope exists; do not assume every denial is a parseable 401/403 result.
- Skipping the handler is not a confidentiality guarantee. Validators, filters, custom state, and exception messages must not leak private data.
- Preflight is subject to races. `execute()` validates again and its final result is authoritative.
- Warning confirmation requires explicit severity handling; plain preflight is not guaranteed to return server warnings. See [severity filtering](../validation/severity-filtering.md).

## React usage

Prefer [CommandForm](../../react/command-form/index.md) for form-managed lifecycle and feedback. For raw hooks, debounce server checks, use an edit revision/current field values rather than `hasChanges`, and discard stale responses. See [React command validation](../../react/commands/validation.md).

For endpoint details, continue with [backend command validation](../../../backend/commands/command-validation.md) and [command filters](../../../backend/commands/command-filters.md).
