---
title: CommandForm submission and result handling
description: Submit the form-owned Cratis Arc command, handle returned results, and connect external Save controls or confirmation through the CommandForm handle.
---

Submit through Cratis Arc `CommandForm` to execute the values being edited and keep result display, callbacks, and busy state connected to that attempt. For Save buttons outside the form, use its `formRef` handle rather than creating another command instance. A separate command can submit different values from those on screen.

## Command result callbacks

Use result callbacks to decide what the user sees after submission. This example uses the generated `UpdateProfile` from [backend validation](./validation.mdx#backend-validation), whose handler returns `{ name, email }`. `ProfileDetails` describes that application response, not a framework API.

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

These callbacks handle returned command results. A thrown exception or rejected promise, including one from a transformation or callback, is not converted to `onException`; catch it when calling the form handle, as in the parent-control example below. Navigate from `onSuccess`, using the response your handler actually returns, and keep raw stack traces out of user-facing messages.

Nonempty custom field errors block submission, `onExecute()` and `formRef.execute()` before transformation or command execution. A blocked attempt publishes the same validation-failure result to `commandResult`, the caller and `onFailed`, then invokes `onValidationFailure`. It makes no HTTP request and does not enter the executing state. Errors set during `onBeforeExecute` are checked again before command execution.

Custom errors and the command's own validation results are kept separately. Editing a custom error updates its displayed failure; clearing the last one restores the retained command result, or `undefined` if there is none. An earlier success can therefore reappear without another execution or callback. Clearing custom errors does not discard native field errors or put a custom failure into silent validity. A subsequent displayed command result replaces the custom failure.

Calling `commandInstance.execute()` directly bypasses this form-local gate. Use submission, `onExecute()`, or `formRef.execute()` when those custom errors must block the attempt.

## Exception diagnostics and display

CommandForm separates user-facing exception feedback from diagnostics. The default panel displays only `An unexpected error occurred. Please try again.`. Use `exceptionMessage` for safe localized text or `exceptionDisplayComponent` to replace the panel; see [safe exception feedback](./customization.md#safe-exception-feedback).

The display does not sanitize, clone, or mutate results: `onFailed` receives the original `ICommandResult<TResponse>`, `onException` receives the original diagnostic array and stack trace, and `commandResult` in the form context retains the original result. The form's execution path also returns that same result. Keep these diagnostics in trusted logging or telemetry rather than rendering them to users. `showErrors={false}` hides automatic feedback, including the custom exception renderer without invoking it, but does not disable callbacks or change result state.

The display checks `hasExceptions` **or** nonempty `exceptionMessages`. The `onException` callback remains flag-driven: messages without `hasExceptions: true` can produce safe feedback but do not invoke that callback.

A descendant can supply a result through `useSetCommandResult`. The same safe display applies, but setting a result directly does not invoke execution callbacks. A subsequent successful result with no exception flag or messages removes the previous exception feedback, whether supplied by execution or a descendant.

## Before execute hook

Normalize values in `onBeforeExecute` when they need a synchronous adjustment before command validation and submission. Its contract is `(command: TCommand) => TCommand`. This example trims the name and returns the same executable instance:

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

Return the executable instance, not a boolean, Promise, or `{ ...command }`. A spread object loses prototype methods such as `execute()` and does not preserve generated property accessors. This is not a cancellation hook, and there is no `beforeExecute` prop. If confirmation requires waiting for a dialog, ask before calling `formRef.current.execute()`.

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

`isValid` combines the latest applied silent validation with the absence of nonempty custom field errors; it is not a validation-pending state. `isAuthorized` is a local identity-role check. Neither is a server security boundary. `execute()` gates on custom errors, not these flags; once that gate passes, execution performs command validation/authorization. See [Validation](./validation.mdx#accessing-validation-state) for current limitations.

## Auto-save

Do not debounce on `command.hasChanges`: it remains true across successive edits. Use actual field values or `commandVersion` in a descendant to schedule work, and clear timers on edits/unmount. Execute through `onExecute` or the form handle.

Serialize saves and track pending revisions so a response can be matched to the edit it submitted. Provide retry/error feedback and decide what happens to edits made during a request. The command captures submitted values, including falsy ones, and advances its baseline only after a successful server result. That supports change tracking; it does not prove durable storage. Tie any “saved” message to the last-successful revision and your backend's persistence contract. Continue with [autosave boundaries](./advanced-patterns.md#autosave-boundaries) for the workflow outline.

## See also

- [Working with hooks](./hooks.md)
- [Validation](./validation.mdx)
- [Data loading](./data-loading.md)
