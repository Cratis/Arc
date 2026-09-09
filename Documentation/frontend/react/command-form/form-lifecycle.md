---
title: Form lifecycle
description: Transform a command synchronously, handle results, and execute the edited form from external controls.
---

Use the form's execution path when you want its result display, callbacks, and busy state to agree. Calling a separately created command can save different values from the ones on screen.

## Command result callbacks

This complete component uses the generated `UpdateProfile` and its ordinary `{ name, email }` response from [Validation](./validation.md#backend-validation). The response type below is an application type, not a framework API.

```tsx
import { useState } from 'react';
import { CommandForm, InputTextField } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

type ProfileDetails = { name: string; email: string };

export function ProfileForm() {
    const [message, setMessage] = useState('');
    return (
        <>
            <CommandForm<UpdateProfile, ProfileDetails>
                command={UpdateProfile}
                initialValues={{ name: '', email: '' }}
                onSuccess={response => setMessage(`Accepted profile for ${response.name}`)}
                onFailed={() => setMessage('The profile was not accepted.')}
                onUnauthorized={() => setMessage('You are not authorized to update this profile.')}
                onException={() => setMessage('The server could not complete the request.')}
                onValidationFailure={results => setMessage(results.map(result => result.message).join(' '))}
            >
                <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
                <InputTextField<UpdateProfile> value={c => c.email} title="Email" type="email" />
                <button type="submit">Save</button>
            </CommandForm>
            <p role="status">{message}</p>
        </>
    );
}
```

The form first calls `onSuccess(response)` or `onFailed(result)`. It then checks, in order, `hasExceptions`, `!isAuthorized`, and `!isValid`, invoking `onException(messages, stackTrace)`, `onUnauthorized()`, and `onValidationFailure(validationResults)` as applicable. Several callbacks can run for the same result; the last message setter wins in this example.

Callbacks describe **returned command results**. An unexpectedly thrown/rejected promise, including a throwing transformation or callback, is not converted to `onException`. Catch rejections when calling the imperative handle, as below. Do not display raw stack traces to end users. Navigate only in `onSuccess`, using the response contract your handler actually returns.

## Exception diagnostics and display

CommandForm separates user-facing exception feedback from diagnostics. The default panel displays only `An unexpected error occurred. Please try again.`. Use `exceptionMessage` for safe localized text or `exceptionDisplayComponent` to replace the panel; see [safe exception feedback](./customization.md#safe-exception-feedback).

The display does not sanitize, clone, or mutate results: `onFailed` receives the original `ICommandResult<TResponse>`, `onException` receives the original diagnostic array and stack trace, and `commandResult` in the form context retains the original result. The form's execution path also returns that same result. Keep these diagnostics in trusted logging or telemetry rather than rendering them to users. `showErrors={false}` hides automatic feedback, including the custom exception renderer without invoking it, but does not disable callbacks or change result state.

The display checks `hasExceptions` **or** nonempty `exceptionMessages`. The `onException` callback remains flag-driven: messages without `hasExceptions: true` can produce safe feedback but do not invoke that callback.

A descendant can supply a result through `useSetCommandResult`. The same safe display applies, but setting a result directly does not invoke execution callbacks. A subsequent successful result with no exception flag or messages removes the previous exception feedback, whether supplied by execution or a descendant.

## Before execute hook

`onBeforeExecute` is a synchronous transformation: `(command: TCommand) => TCommand`. This alternative complete component trims the name before command validation and submission:

```tsx
import { CommandForm, InputTextField } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

export function ProfileForm() {
    return (
        <CommandForm
            command={UpdateProfile}
            initialValues={{ name: '', email: '' }}
            onBeforeExecute={command => {
                command.name = command.name.trim();
                return command;
            }}
        >
            <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
            <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

Return the executable instance. Do **not** return a boolean, a Promise, or `{ ...command }`: a spread object loses prototype methods such as `execute()` and does not preserve generated property accessors. There is no `beforeExecute` prop and no asynchronous cancellation contract. Confirm **before** invoking `formRef.current.execute()` instead.

## Reaching the form from a parent

`formRef` supplies a stable `CommandFormHandle` with `execute(): Promise<ICommandResult<unknown>>` and live `isExecuting`, `isValid`, and `isAuthorized` getters. A ref read does not rerender a parent. Pair it with `onStateChange` when an external button needs reactive state.

This complete component receives an application-owned asynchronous confirmation function. Both the footer button and native/keyboard form submission use the same gate; cancellation makes no execution call.

```tsx
import { useRef, useState } from 'react';
import {
    CommandForm, InputTextField,
    type CommandFormHandle, type CommandFormState
} from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

type ProfilePanelProps = { confirmSave: () => Promise<boolean> };

export function ProfilePanel({ confirmSave }: ProfilePanelProps) {
    const formRef = useRef<CommandFormHandle>(null);
    const busyRef = useRef(false);
    const [pending, setPending] = useState(false);
    const [message, setMessage] = useState('');
    const [state, setState] = useState<CommandFormState>({
        isExecuting: false, isValid: false, isAuthorized: true
    });

    async function save() {
        if (busyRef.current || !formRef.current) return;
        busyRef.current = true;
        setPending(true);
        try {
            if (!await confirmSave()) return;
            const result = await formRef.current?.execute();
            if (result) setMessage(result.isSuccess ? 'Profile accepted.' : 'Please review the result.');
        } catch {
            setMessage('The save could not complete. Please try again.');
        } finally {
            busyRef.current = false;
            setPending(false);
        }
    }

    return (
        <section onSubmitCapture={event => {
            event.preventDefault();
            event.stopPropagation();
            void save();
        }}>
            <CommandForm
                command={UpdateProfile}
                initialValues={{ name: '', email: '' }}
                formRef={formRef}
                onStateChange={setState}
            >
                <fieldset disabled={pending || state.isExecuting}>
                    <legend>Profile</legend>
                    <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
                    <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
                    <button type="submit" hidden>Save</button>
                </fieldset>
            </CommandForm>
            <footer>
                <button type="button" onClick={save} disabled={pending || state.isExecuting}>
                    {pending || state.isExecuting ? 'Saving…' : 'Save'}
                </button>
            </footer>
            <p role="status">{message}</p>
        </section>
    );
}
```

Supply `confirmSave` from your dialog implementation. The hidden form-owned submit button enables implicit Enter submission from the text fields; the capture handler routes it through the same confirmation as the external Save button. The disabled fieldset keeps values stable while the question is open. The ref lock also stops same-tick double clicks. The example deliberately allows invalid values to be submitted so the command can display current validation errors; it does not treat a previous `isValid` result as permanent permission.

Pass a stable ref from `useRef` or `useCallback`. An inline callback ref that sets parent state can repeatedly detach/attach and cause a render loop. `onStateChange` is different: its callback is read through a ref, runs on mount and on changes to its three state booleans, and safely accepts inline functions.

`isValid` describes the latest applied silent validation, not a validation-pending state. `isAuthorized` is a local identity-role check. Neither is a server security boundary, and `execute()` does not use them as a precondition; execution performs command validation/authorization. See [Validation](./validation.md#accessing-validation-state) for current limitations.

## Auto-save

Do not debounce on `command.hasChanges`: it remains true across successive edits. Use actual field values or `commandVersion` in a descendant to schedule work, and clear timers on edits/unmount. Execute through `onExecute` or the form handle.

A production autosave also needs serialization of saves, a pending revision, retry/error UI, and a policy for edits arriving during an in-flight save. The current command implementation snapshots its baseline after a completed server request **even on a failed result**, and that snapshot helper skips falsy values. Do not use `hasChanges` as a reliable “successfully persisted” indicator. Keep an application-owned last-successful snapshot/revision if you need that guarantee. [Advanced patterns](./advanced-patterns.md) describes these boundaries without pretending the form supplies an autosave transaction.

## See also

- [Working with hooks](./hooks.md)
- [Validation](./validation.md)
- [Data loading](./data-loading.md)
