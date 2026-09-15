---
title: Validation
description: Distinguish command rules from HTML attributes, and display current validation results in CommandForm.
---

A form can look complete while its command is invalid. Define the rules on the backend, generate supported client rules, and let CommandForm display their results. HTML input attributes and presentation callbacks are not substitutes for command validation.

## Required fields

`required` defaults to the inverse of the generated property's `isOptional` descriptor. An explicit field prop overrides the **control's required flag**, not the command's metadata or backend rules.

CommandForm renders `<form noValidate>`. Native browser constraint validation therefore does not block submission, including submission through `formRef`. In particular:

| Setting | What it does | What enforces the rule |
| --- | --- | --- |
| `required` | Marks the rendered control as required. | Command payload presence checks reject `null`/`undefined` for non-optional payload properties; they do not reject `''`, whitespace, `0`, or `false`. |
| `type="email"` / `type="url"` | Selects the browser input type and input affordances. | An explicit email/URL command rule. |
| `NumberField` `min`, `max`, `step` | Configures the numeric control. | Command range/precision rules; typed or programmatically supplied values still need validation. |
| A required checkbox | Marks the control, but `false` is a present boolean value. | A rule requiring `true` for consent. |

Use FluentValidation `NotEmpty()` (or an appropriate `[Required]` rule) to reject empty strings. Put business invariants on the server even when a matching generated client rule exists. `InputTextField` does **not** expose `minLength`, `maxLength`, or `pattern`; use generated rules or a [custom field](./custom-fields.md) that explicitly supports those attributes. Such attributes still do not turn off the form's `noValidate` behavior.

A field's display fallback (for example `''` for an undefined text value) is not a command value or baseline. Supply intentional defaults with `initialValues` when needed.

## Validation timing

Silent validation runs on initialization and every field edit. The form starts with `isValid: false` until that asynchronous path applies its first result. It is not already validated on the first render.

The following are **configuration fragments** inside a component that imports `CommandForm` and the generated `UpdateProfile` from the [overview](./index.md):

```tsx
<CommandForm command={UpdateProfile} validateOn="blur" />
<CommandForm command={UpdateProfile} validateOn="change" />
<CommandForm command={UpdateProfile} validateOn="both" validateAllFieldsOnChange />
<CommandForm command={UpdateProfile} validateOnInit />
```

| Prop | Default | Effect |
| --- | --- | --- |
| `validateOn` | `'blur'` | Determines when interaction error messages are updated: blur, change, or both. It does not disable silent edit validation. |
| `validateAllFieldsOnChange` | `false` | False merges only the interacted field's displayed messages; true displays the full result. Both paths validate the command, not an isolated field rule. |
| `validateOnInit` | `false` | Shows initialization/population errors. False hides those initial messages, not the validation work. |
| `autoServerValidate` | `false` | Enables automatic server preflight; see [Auto server validation](./auto-server-validation.md) for request paths and timing. |

Use full-result display for cross-field rules when changing one field should update another field's message. With automatic server validation disabled, silent/interaction validation is client-side. Submission still runs command validation and, when local checks pass, the server pipeline.

## Backend validation

This standalone Arc endpoint is a complete command-and-validator example for an already configured Arc host. It returns the accepted values; it deliberately does **not** persist a profile. A real update also needs an explicit backend write. No event store or Chronicle integration is involved.

:::tip[Persist on the backend, not in a form callback]
This example only returns accepted values. For a real immediate inline update, prefer a backend [command operation](../../../backend/commands/operations/index.md) describing the service write; direct service calls remain supported. Keep validation in validators and UI feedback in form callbacks. Operations execute on the server and do not add operation payloads or recovery details to the TypeScript command result.
:::

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace MyApp.Profiles;

public record ProfileName(string Value) : ConceptAs<string>(Value)
{
    public static readonly ProfileName NotSet = new(string.Empty);
}

public record EmailAddress(string Value) : ConceptAs<string>(Value)
{
    public static readonly EmailAddress NotSet = new(string.Empty);
}

public class EmailAddressValidator : ConceptValidator<EmailAddress>
{
    public EmailAddressValidator() => RuleFor(email => email.Value).NotEmpty().EmailAddress();
}

[Command]
public record UpdateProfile(ProfileName Name, EmailAddress Email)
{
    public ProfileDetails Handle() => new(Name, Email);
}

public record ProfileDetails(ProfileName Name, EmailAddress Email);

public class UpdateProfileValidator : CommandValidator<UpdateProfile>
{
    public UpdateProfileValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MinimumLength(3).MaximumLength(100);
    }
}
```

The named values distinguish profile names from email addresses in backend signatures. `EmailAddressValidator` carries the email invariant wherever that concept appears. Arc's concept-aware `RuleFor` unwraps `ProfileName`, so the command's string rules still apply. The generated `name` and `email` properties are strings, with the concept's supported rules attached to `email`; your React inputs need no concept-object conversion. These declarations are grouped for copying; keep each concept in its own application file.

Build in Debug with [proxy generation configured](../../../backend/proxy-generation/getting-started.md), then use the generated class in the [overview form](./index.md#start-with-a-generated-command). Entering a short name or malformed email should display its rule message on blur. A valid submission returns `{ name, email }` to `onSuccess`. Only supported rules are generated; asynchronous service/database checks remain server-side.

Preflight `validate()` skips handler execution and handler-argument resolution, but still runs pipeline filters/validators when it reaches the server. Keep those operations free of unintended side effects and sensitive diagnostic output. A client-side short-circuit does not establish server authorization. See [Backend command validation](../../../backend/commands/command-validation.md).

## Accessing validation state

Read context in a **child** of the form. This complete component shows all displayed validation messages, including results with no field member. `showErrors={false}` hides automatic field errors and form-level exception feedback, including any custom `exceptionDisplayComponent` without invoking it; validation and result state remain unchanged:

```tsx
import { CommandForm, InputTextField, useCommandFormContext } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

function ErrorSummary() {
    const { commandResult } = useCommandFormContext<UpdateProfile>();
    const results = commandResult?.validationResults ?? [];
    if (results.length === 0) return null;
    return (
        <ul role="alert">
            {results.map((result, index) => (
                <li key={index}>
                    {result.reason === 'rule' ? result.message : 'The request could not be validated. Please try again.'}
                </li>
            ))}
        </ul>
    );
}

export function ProfileForm() {
    return (
        <CommandForm command={UpdateProfile} initialValues={{ name: '', email: '' }} showErrors={false}>
            <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
            <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
            <ErrorSummary />
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

`getFieldError('email')` returns the first matching message, giving custom errors precedence. Built-in field display also shows that first message; the summary can show the full array. Results without members require form-level display. Translating summary text changes only that summary; it does not sanitize messages rendered independently by custom fields. Keep rule messages safe for end users and translate framework-generated reasons to application wording rather than exposing diagnostics; [validation results](../../core/validation/results.md) describes reason metadata. This summary handles validation results only: when hiding automatic feedback, provide your own safe exception feedback too.

Current state boundaries:

- `isValid` comes from the latest applied **silent** result having an empty `validationResults` array, not from displayed `commandResult.isValid` or custom field errors. Even a warning makes that form flag false; command execution has its own severity policy.
- `isAuthorized` checks the client identity's roles. It does not certify server authorization.
- The form has no public validation-pending state. A prior verdict can remain while newer validation is in flight. Internal ordering rejects a result overtaken by a later **applied** result, not every result for an older edit.
- `onFieldChange` fires immediately on edits with previously displayed validation information; it can also fire after blur. Do not use its `validationInfo` as a fresh asynchronous permission to save.
- `onFieldValidate` and `setCustomFieldError` supply presentation errors only; reproduce blocking rules in command validation.

These flags are UI hints, not reservations or security boundaries. Always let execution validate again.

## Validation failures and exceptions

Field validation messages remain associated with their fields and continue to use `errorDisplayComponent` when supplied without a custom field container. A validation-only result does not produce form-level exception feedback. With `showErrors` enabled, a result containing both validation failures and exceptions displays the field messages alongside the safe exception message.

CommandForm shows exception feedback when `hasExceptions` is true **or** `exceptionMessages` is nonempty, even if those values are inconsistent. It never displays those diagnostic messages or the stack trace automatically. The default text is `An unexpected error occurred. Please try again.`; use [safe exception customization](./customization.md#safe-exception-feedback) to localize it or replace its panel. The `onException` callback still depends on the flag, not the messages; see [exception diagnostics and display](./form-lifecycle.md#exception-diagnostics-and-display).

## Progressive validation

Prefer built-in validation for ordinary feedback. If you own an asynchronous preflight UI, tie the verdict to the **edit revision**, clear permission immediately on a new revision, and ignore superseded responses. Depending on the boolean `hasChanges` misses all edits after it first becomes true.

This complete child component is an optional replacement for the submit button in the overview. It debounces its own preflight and must be rendered inside CommandForm. Leave `autoServerValidate` off to avoid two competing preflight mechanisms.

```tsx
import { useEffect, useState } from 'react';
import { useCommandFormContext } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

type Check = { revision: number; valid: boolean };

export function CheckedSubmitButton() {
    const { commandInstance, commandVersion, isExecuting } = useCommandFormContext<UpdateProfile>();
    const [check, setCheck] = useState<Check>();

    useEffect(() => {
        let active = true;
        const revision = commandVersion;
        const timer = setTimeout(async () => {
            try {
                const result = await commandInstance.validate();
                if (active) setCheck({
                    revision,
                    valid: result.isValid && result.isAuthorized && !result.hasExceptions
                });
            } catch {
                if (active) setCheck({ revision, valid: false });
            }
        }, 500);
        return () => {
            active = false;
            clearTimeout(timer);
        };
    }, [commandInstance, commandVersion]);

    const pending = check?.revision !== commandVersion;
    return (
        <div>
            <button type="submit" disabled={pending || !check?.valid || isExecuting}>Save</button>
            <p role="status">{pending ? 'Checking…' : check?.valid ? 'Ready to submit.' : 'Review the values or try editing again.'}</p>
        </div>
    );
}
```

A new form revision immediately invalidates the old button verdict even before its effect runs. Cleanup prevents an older request from overwriting a newer check and prevents updates after unmount. It does not cancel an HTTP request already sent. This example observes edits made through the form; arbitrary direct property assignments are not guaranteed to advance its revision. Preflight can still short-circuit locally and is never an authorization grant. Keyboard or programmatic submission can bypass a disabled button, so execution remains the enforcement point.

## Validation results

`validate()` returns a `CommandResult<TResponse>` implementing `ICommandResult<TResponse>`. Use these actual members; there is no `errors` dictionary, `hasErrors()` method, or `getErrorsFor()` method.

| Member | Meaning |
| --- | --- |
| `isSuccess`, `isValid`, `isAuthorized`, `hasExceptions` | Outcome flags. |
| `validationResults` | Array of `{ severity, message, members, state, reason, reasonDetail? }`. |
| `exceptionMessages`, `exceptionStackTrace` | Exception diagnostics. |
| `authorizationFailureReason` | Authorization failure detail. |
| `correlationId`, `response` | Correlation identifier and optional typed response. |

For exact field matching outside CommandForm, filter `validationResults` by member name and map to messages. Account for casing/nested paths in your own renderer; within the form prefer `getFieldError()` for its field-matching behavior.

## See also

- [Auto server validation](./auto-server-validation.md)
- [Customization](./customization.md)
- [Core command validation](../../core/commands/validation.md)
- [Form lifecycle](./form-lifecycle.md)
