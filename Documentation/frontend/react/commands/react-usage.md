---
title: React command hooks
description: Use the generated command tuple for explicit input updates, baseline initialization, and execution feedback.
---

A generated command's static `.use()` method creates a command instance for a mounted component, configures it from the nearest Arc context, and registers it with the nearest command scope.

## Return tuple

| Position | Value | Contract |
| --- | --- | --- |
| 0 | `command` | Generated instance, properties, execution, and change tracking |
| 1 | `setValues` | Updates supplied content properties and requests a React render |
| 2 | `clearValues` | Assigns undefined to all content properties and requests a render; does not reset the baseline |

Omit unused trailing tuple values when destructuring. Do not treat the tuple itself as a command.

## With initial values

Pass an explicit content object to seed values and baseline when the hook creates its instance. Initial values are not reapplied on later renders. Required generated properties are not automatically initialized; an identity, quantity, or other required value must come from your application.

This illustrative one-operation editor assumes a generated `RenameAccount` command with string `accountId` and `name` properties. If your backend uses a Guid-based concept, use the generated Fundamentals `Guid` type instead of a string:

```tsx
import { useState } from 'react';
import { RenameAccount } from './generated/RenameAccount';

export function RenameAccountForm({ accountId, name }: { accountId: string; name: string }) {
    const [command, setValues] = RenameAccount.use({ accountId, name });
    const [saving, setSaving] = useState(false);
    const [message, setMessage] = useState('');

    async function save() {
        setSaving(true);
        try {
            const result = await command.execute();
            setMessage(result.isSuccess ? 'Saved.' : 'Could not save. Check your inputs and permissions.');
        } finally {
            setSaving(false);
        }
    }

    return (
        <form onSubmit={event => { event.preventDefault(); void save(); }}>
            <label>Name<input value={command.name ?? ''} disabled={saving}
                onChange={event => setValues({ name: event.target.value })} /></label>
            <button disabled={saving}>Save</button>
            <p role="status">{message}</p>
        </form>
    );
}
```

Key/remount the editor when switching accounts, or deliberately load a new baseline after deciding what to do with edits. See [data binding](./data-binding.md) for query-fed forms and reset semantics.

## React re-rendering

Use the tuple setter for reliable per-edit rendering. Generated setters notify property tracking, but raw assignment does not reliably rerender every edit while `hasChanges` remains true. The hook does not automatically validate each assignment or provide an execution-loading state; use explicit state as above or [CommandForm](../command-form/index.md).

`hasChanges` compares draft content with the baseline. It is not an edit revision, a server-save status, or a guaranteed retry gate after failure. Current execution snapshots truthy values after completed server requests even when unsuccessful; see [baseline limitations](./data-binding.md#execution-baseline-limitations).

## Available operations

- `execute(allowedSeverity?, ignoreWarnings?)` validates and attempts execution; inspect its `CommandResult`.
- `validate()` provides preflight feedback, potentially returning locally before server authorization.
- `validateClientSide()` checks local rules synchronously.
- `setInitialValues(content)` accepts explicit values as the baseline.
- `revertChanges()` restores baseline values.

Continue with [validation](./validation.md), [command scopes](./scope.md), or [imperative usage](./imperative-usage.md).
