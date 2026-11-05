---
title: React command hooks for editing and saving
description: Use generated Cratis Arc command hooks to initialize an editor, update React inputs through the tuple setter, and show execution results.
---

Use a generated Cratis Arc command's static `.use()` method to edit and submit command content from a React component. The hook keeps one command instance for the mounted component, applies configuration from the nearest Arc context, and registers the command with the nearest command scope.

## Return tuple

| Position | Value | Contract |
| --- | --- | --- |
| 0 | `command` | Generated instance, properties, execution, and change tracking |
| 1 | `setValues` | Updates supplied content properties and requests a React render |
| 2 | `clearValues` | Assigns undefined to all content properties and requests a render; does not reset the baseline |

Destructure the tuple before calling command methods. For an editor that does not need to clear its fields, `[command, setValues]` is enough; omit unused trailing values.

## With initial values

Pass the record's current content to `.use()` when you mount an editor. Those initial values become both the draft and the baseline used by `hasChanges`. Later renders do not reapply them, so a parent rerender does not overwrite edits. Your application must supply required values; the hook does not create identifiers or fill in other required properties.

This example assumes a generated `RenameAccount` command with string `accountId` and `name` properties. Match your generated types: a Guid-based backend concept uses the Fundamentals `Guid` type rather than the strings shown here.

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

Typing calls `setValues()` so the input displays each edit. Submitting prevents native form navigation, executes the command, and shows a message from the returned result. The separate `saving` state disables inputs and the Save button while the request is pending.

Switching the `accountId` prop alone does not initialize a new command. Key the editor by account identity to remount it, or deliberately replace its baseline after deciding what to do with unsaved edits. Follow [data binding](./data-binding.md) for query-fed editors and resets.

## React re-rendering

Use the tuple setter for input edits. Generated property setters notify change tracking, but direct assignment does not reliably rerender every edit while `hasChanges` remains true. Neither path automatically validates each assignment. Keep validation and request state explicit, as above, or use [CommandForm](../command-form/index.md) to manage the form workflow.

`hasChanges` compares the draft with its baseline; it is not an edit counter or save-result flag. Successful execution accepts the submitted snapshot as the baseline, while unsuccessful execution leaves it unchanged. See [execution baseline limitations](./data-binding.md#execution-baseline-limitations) before allowing edits during a pending request.

## Available operations

- `execute(allowedSeverity?, ignoreWarnings?)` validates and attempts execution; inspect its `CommandResult`.
- `validate()` checks the draft without executing the handler; local failures can return before server authorization.
- `validateClientSide()` checks local rules synchronously.
- `setInitialValues(content)` accepts explicit values as the baseline.
- `revertChanges()` restores baseline values.

Add [validation feedback](./validation.md) when the editor needs field messages. If several editors share a Save action, use a [command scope](./scope.md). For code outside a component, follow [imperative usage](./imperative-usage.md).
