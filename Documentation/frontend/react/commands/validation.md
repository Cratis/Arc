---
title: Command validation in React
description: Show preflight feedback without stale submit permission, native form navigation, or false security guarantees.
---

A field can be valid when you start checking it and different by the time the answer arrives. For raw command hooks, treat validation as feedback tied to an edit revision, not as a lasting permission to submit. [CommandForm](../command-form/index.md) is the preferred form-managed alternative.

## How it works

`validate()` first checks client rules and required values. A failure can return locally without checking server authorization. Otherwise the backend runs filters but skips handler execution and handler-argument resolution. Filter/validator code must itself be read-only and avoid sensitive messages; “the handler did not run” is not a blanket side-effect or confidentiality guarantee.

`execute()` validates again. Inspect its final `CommandResult` even after a successful preflight. See [core validation](../../core/commands/validation.md).

## Progressive and debounced validation

This illustrative component assumes a generated `UpdateContact` command with string `email` and `phone` properties and no other required input. It validates the displayed draft after a short delay, discards stale answers, clears old messages on edits, and prevents native form navigation:

```tsx
import { useEffect, useRef, useState } from 'react';
import { ValidationResult } from '@cratis/arc/validation';
import { UpdateContact } from './generated/UpdateContact';

export function ContactEditor() {
    const [command, setValues] = UpdateContact.use({ email: '', phone: '' });
    const [revision, setRevision] = useState(0);
    const currentRevision = useRef(0);
    const [errors, setErrors] = useState<ValidationResult[]>([]);
    const [saving, setSaving] = useState(false);
    const [status, setStatus] = useState('');

    function edit(values: { email?: string; phone?: string }) {
        currentRevision.current++;
        setValues(values);
        setErrors([]);
        setStatus('');
        setRevision(currentRevision.current);
    }

    useEffect(() => {
        let active = true;
        const timer = setTimeout(async () => {
            const result = await command.validate();
            if (active && currentRevision.current === revision) {
                setErrors(result.validationResults);
            }
        }, 300);
        return () => { active = false; clearTimeout(timer); };
    }, [command, revision]);

    async function submit() {
        currentRevision.current++;
        setSaving(true);
        try {
            const result = await command.execute();
            setErrors(result.validationResults);
            setStatus(result.isSuccess ? 'Saved.' : 'Save failed. Check your inputs and permissions.');
        } finally {
            setSaving(false);
        }
    }

    const errorFor = (member: string) => errors.find(error => error.members.includes(member))?.message;
    return (
        <form noValidate onSubmit={event => { event.preventDefault(); void submit(); }}>
            <label>Email<input value={command.email ?? ''} disabled={saving}
                onChange={event => edit({ email: event.target.value })} /></label>
            <p>{errorFor('email')}</p>
            <label>Phone<input value={command.phone ?? ''} disabled={saving}
                onChange={event => edit({ phone: event.target.value })} /></label>
            <p>{errorFor('phone')}</p>
            <button disabled={saving}>Save</button>
            <p role="status">{status}</p>
        </form>
    );
}
```

The submit button does not rely on cached preflight permission. Required rules remain on the backend; the sample's HTML does not add Arc validation rules. Map framework-generated rejection reasons to application copy before showing them in production; see [validation results](../../core/validation/results.md).

## Validation on blur

For blur-only feedback, call `validate()` from the field's blur handler and use the same revision guard. Find messages by `result.validationResults.filter(item => item.members.includes(fieldName))`. Replace that field's previous messages with the new list, **including an empty list**, even when a different field still fails. There are no `CommandResult.hasErrors` or `getErrorsFor` helpers.

## Common mistakes

- `[command.hasChanges]` is not a per-edit dependency: the flag can remain true across many edits.
- Raw property assignment does not guarantee every React render or automatic validation. Use the tuple setter for edits.
- Preflight success can go stale. Do not reuse it after a draft or identity change.
- `setValues()` edits content; it does not reset the baseline. A completed rejected server execution can change that baseline today. See [data binding](./data-binding.md#execution-baseline-limitations).
- Default execution allows warnings. For confirmation-before-warning-override, use the explicit [severity workflow](../../core/validation/severity-filtering.md), not `execute()` followed by a question.

Continue with [command results](../../core/commands/command-result.md) and [server validation](../../../backend/commands/command-validation.md).
