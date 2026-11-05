---
title: React command validation for editable forms
description: Validate Cratis Arc command drafts in React, discard feedback for older edits, and check the final execution result before reporting success.
---

Validate a Cratis Arc command draft to show feedback before submission, then inspect the final execution result when saving. With raw React command hooks, track which edit a validation response belongs to: a field can change while the check is pending. Use [CommandForm](../command-form/index.md) if you want the form to manage this workflow.

## How it works

`validate()` checks client rules and required values first. A blocking failure can return locally without checking server authorization. If the local checks allow it, validation calls the backend, where filters run without executing the command handler or resolving its arguments.

Keep filter and validator code read-only, and avoid sensitive information in its messages. Skipping the handler does not prevent a custom filter from causing side effects or disclosing data.

A successful check describes the draft at that moment, not permission to submit a later draft. `execute()` validates again, so inspect its final `CommandResult` even after validation succeeded. See [core validation](../../core/commands/validation.md) for the request flow.

## Progressive and debounced validation

For feedback while typing, wait briefly after each edit before calling `validate()`. Give each edit a revision number, and display a response only if it still belongs to the current revision. Debouncing reduces requests; the revision check prevents a slower, older response from replacing newer feedback.

This example assumes a generated `UpdateContact` command with string `email` and `phone` properties and no other required input:

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

Each change updates the command through `setValues()`, clears the old feedback, and schedules validation after 300 milliseconds. Effect cleanup prevents an obsolete check from updating the displayed errors, while the revision counter also invalidates pending feedback when submission starts. This discards stale answers; it does not cancel requests already sent to the server.

Submitting prevents native form navigation and calls `execute()` rather than relying on cached validation success. Inputs stay disabled while saving, and the final result supplies the status and validation messages. The sample's HTML does not define Arc rules: define the required rules on the backend and use the generated proxy's client rules where available. Map framework-generated rejection reasons to application copy before displaying them to users; see [validation results](../../core/validation/results.md).

## Validation on blur

To check a field when the user leaves it, call `validate()` from its blur handler and keep the same revision guard. The result can contain messages for several fields, so select the field's messages with `result.validationResults.filter(item => item.members.includes(fieldName))`.

Replace that field's previous messages even when the new list is empty. Otherwise an old message can linger after the field becomes valid, especially when another field still fails. Read `validationResults` directly; `CommandResult` has no `hasErrors` or `getErrorsFor` helpers.

## Common mistakes

- `[command.hasChanges]` is not a per-edit dependency: the flag can remain true across many edits.
- Raw property assignment does not guarantee every React render or automatic validation. Use the tuple setter for edits.
- Preflight success can go stale. Do not reuse it after a draft or identity change.
- `setValues()` edits content without resetting the baseline. Successful execution accepts the submitted values; unsuccessful execution leaves the baseline unchanged. See [data binding](./data-binding.md#execution-baseline-limitations).
- Warnings do not block execution by default, though a command's declared severity policy can make them blocking. For confirmation before overriding a warning, follow the explicit [severity workflow](../../core/validation/severity-filtering.md) rather than asking after `execute()`.

Use [command results](../../core/commands/command-result.md) to distinguish validation, authorization, and execution failures in the UI. To define the rules behind those messages, follow [server validation](../../../backend/csharp/commands/command-validation.md).
