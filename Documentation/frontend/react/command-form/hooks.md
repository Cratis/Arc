---
title: Working with hooks
description: Read a CommandForm's command, validation, and execution state from descendant components.
---

A custom button or preview must use the **same command instance** as the fields. CommandForm's hooks read that instance from React context; they do not construct independent commands.

## useCommandFormContext

Call this hook in a descendant of `CommandForm`, never in the component that merely returns the form. This complete component uses the generated `UpdateProfile` from the [overview checkpoint](./index.md#start-with-a-generated-command).

```tsx
import { CommandForm, InputTextField, useCommandFormContext } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

function SaveControls() {
    const { onExecute, isExecuting, commandResult } = useCommandFormContext<UpdateProfile>();

    async function save() {
        try {
            await onExecute?.();
        } catch {
            window.alert('The save could not complete. Please try again.');
        }
    }

    return (
        <div>
            <button type="button" onClick={save} disabled={isExecuting}>Save</button>
            {commandResult && !commandResult.isSuccess && <p role="status">The profile was not accepted.</p>}
        </div>
    );
}

export function ProfileForm() {
    return (
        <CommandForm command={UpdateProfile} initialValues={{ name: '', email: '' }}>
            <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
            <InputTextField<UpdateProfile> value={c => c.email} title="Email" type="email" />
            <SaveControls />
        </CommandForm>
    );
}
```

This example uses a click-only `type="button"` action; it has no form-owned submitter for implicit Enter submission. For both keyboard submission and external controls with confirmation and a duplicate guard, use [ProfilePanel](./form-lifecycle.md#reaching-the-form-from-a-parent).

`onExecute()` follows the form's transformation and result-callback path. Calling `commandInstance.execute()` directly bypasses those form callbacks, result display updates, and execution counting. Do not block a retry using the previous `commandResult.isValid`: displayed errors can describe an earlier submission.

Useful context members:

| Member | Use |
| --- | --- |
| `commandInstance` | The generated command owned by this form. |
| `commandVersion` | Revision advanced by the form's setters/baseline population; useful for edit-dependent effects. |
| `setCommandValues` | Writes command properties and rerenders; this is not baseline initialization or validation. |
| `commandResult`, `getFieldError(name)` | Displayed result and the first matching field message. |
| `isValid` | Whether the latest applied silent result contains no validation results; false until a result arrives. |
| `isAuthorized` | Client identity-role check, not proof of server authorization. |
| `isExecuting`, `onExecute` | Execution state and the form's execution operation. |
| `customFieldErrors`, `setCustomFieldError` | Presentation messages keyed by field; these alone do not block execution. |

## useIsCommandExecuting

`useIsCommandExecuting()` is a zero-argument shortcut for the context's `isExecuting`. It is suitable for a submit button rendered **inside** the form:

```tsx
import { useIsCommandExecuting } from '@cratis/arc.react/commands';

export function SubmitButton() {
    const isExecuting = useIsCommandExecuting();
    return <button type="submit" disabled={isExecuting}>{isExecuting ? 'Saving…' : 'Save'}</button>;
}
```

The form counts overlapping executions. If two submissions race, the state remains true until both settle; a rejected promise also releases its execution count. This reports activity, not duplicate-submission prevention. Disable UI actions and enforce any required idempotency on the server.

## useCommandInstance

`useCommandInstance<TCommand>()` takes **no arguments** and returns the surrounding form's instance. This complete component adds a preview below the fields:

```tsx
import { CommandForm, InputTextField, useCommandInstance } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

function ProfilePreview() {
    const command = useCommandInstance<UpdateProfile>();
    return <p>Preview: {command.name || 'Unnamed'} — {command.email || 'No email'}</p>;
}

export function ProfileForm() {
    return (
        <CommandForm command={UpdateProfile} initialValues={{ name: '', email: '' }}>
            <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
            <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
            <ProfilePreview />
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

Field edits rerender the context consumer. For programmatic edits, use the context's setter with an explicit object of property values rather than spreading a generated instance: generated properties are prototype accessors. The setter's current type is `TCommand`, so an explicit partial update may need an assertion; keep that assertion local and never pass the partial object to `onBeforeExecute` as a replacement command.

All these hooks throw outside the provider. Use [formRef/onStateChange](./form-lifecycle.md#reaching-the-form-from-a-parent) for external controls. If you truly need an independent command, use its [generated `.use()` hook](../commands/react-usage.md); it does not connect to a separate CommandForm.

## See also

- [Validation](./validation.md)
- [Data loading](./data-loading.md)
- [Form lifecycle](./form-lifecycle.md)
