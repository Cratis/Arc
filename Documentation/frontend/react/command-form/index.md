---
title: CommandForm setup in Cratis Arc
description: CommandForm binds React fields to one generated Cratis Arc command instance, so submission executes the values the user edited.
---

`CommandForm` in Cratis Arc binds React fields to one generated command instance and executes that instance on submission. Use it to build an editor without keeping a second copy of the command's values in React state.

<a id="overview"></a>
<a id="basic-usage"></a>

## Start with a generated command

First configure [Cratis Arc in React](../arc.md) and [proxy generation](../../../backend/csharp/proxy-generation/getting-started.md), which creates TypeScript classes for your backend contracts. Import the generated command class: the form needs its route, property descriptors, and validation metadata to bind fields and execute a request. An interface or empty subclass of `Command` does not supply that information.

This example edits `name` and `email` on a generated `UpdateProfile`. Adjust its import to your generated output directory. The [backend validation example](./validation.mdx#backend-validation) supplies the endpoint and rules; it returns profile details without persisting them and does not require Cratis Chronicle.

```tsx
import { CommandForm, InputTextField, useIsCommandExecuting } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

function SaveButton() {
    const isExecuting = useIsCommandExecuting();
    return <button type="submit" disabled={isExecuting}>{isExecuting ? 'Saving…' : 'Save'}</button>;
}

export function ProfileForm() {
    return (
        <CommandForm
            command={UpdateProfile}
            initialValues={{ name: '', email: '' }}
            onSuccess={() => window.alert('Profile accepted')}
        >
            <InputTextField<UpdateProfile> value={c => c.name} title="Name" placeholder="Your name" />
            <InputTextField<UpdateProfile> value={c => c.email} title="Email" type="email" />
            <SaveButton />
        </CommandForm>
    );
}
```

Edit a field, leave it to see rule failures, then submit valid values. `SaveButton` reads the surrounding form's execution state because it renders **inside** the form. Hooks called in `ProfileForm` itself would be outside that provider. For a toolbar or dialog footer, use [formRef and onStateChange](./form-lifecycle.md#reaching-the-form-from-a-parent).

`type="email"` chooses an HTML control; it does not define an Arc email rule. The form uses `noValidate`, so browser constraint validation does not block submission. See [required fields and validation rules](./validation.mdx#required-fields).

<a id="commandformprops"></a>

## Props reference

`command` is required; `children` accepts React nodes. The callbacks below use the generated instance type `TCommand` and the response type `TResponse` (default `object`).

| Prop | Default | Contract |
| --- | --- | --- |
| `command` | Required | `Constructor<TCommand>`; use the generated command class. |
| `initialValues` | Unset | `Partial<TCommand>` seed and change-tracking baseline. Undefined values supply nothing; changing this prop alone after mount does not repopulate the command. |
| `currentValues` | Unset | Reactive `Partial<TCommand>` overlay. Present keys are written, including explicit `null`/`undefined` where the property type permits them; absent keys are left alone. |
| `populateFromQuery` | Unset | Single-instance query constructor used to populate field baselines. |
| `populateFromObservableQuery` | Unset | Observable counterpart; choose one population source. |
| `populateFromQueryArgs` | Unset | Argument object for either population source. |
| `validateOn` | `'blur'` | `'blur'`, `'change'`, or `'both'`; controls when interaction errors appear, not whether silent validation runs. |
| `validateAllFieldsOnChange` | `false` | Display the full validation result rather than merging only the interacted field's errors. |
| `validateOnInit` | `false` | Display initialization/population validation errors. Silent validation still runs when false. |
| `autoServerValidate` | `false` | Opt into pre-submit server validation; submission still validates on the server. |
| `autoServerValidateThrottle` | `500` | Delay in milliseconds for the typing-triggered server-validation timer, not a global request limiter. |
| `onFieldValidate` | Unset | Synchronous `(command, fieldName, oldValue, newValue) => string \| undefined`; supplies a custom field message. |
| `onFieldChange` | Unset | `(command, fieldName, oldValue, newValue, validationInfo?) => void`; interaction notification, not a fresh asynchronous validation verdict. |
| `onBeforeExecute` | Unset | Synchronous `(command: TCommand) => TCommand`; return the executable command instance. |
| `onSuccess` | Unset | `(response: TResponse) => void`. |
| `onFailed` | Unset | `(result: ICommandResult<TResponse>) => void`; receives the original result when execution fails. |
| `onException` | Unset | `(messages: string[], stackTrace: string) => void`; receives original diagnostics when `hasExceptions` is true. |
| `onUnauthorized` | Unset | `() => void`. |
| `onValidationFailure` | Unset | `(validationResults: ValidationResult[]) => void`. |
| `formRef` | Unset | `React.Ref<CommandFormHandle>` for parent execution and live state reads. |
| `onStateChange` | Unset | `(state: CommandFormState) => void` for reactive parent state. |
| `showTitles` | `true` | Enable built-in field titles. Custom renderers must honor the intended presentation themselves. |
| `showErrors` | `true` | Show automatic field errors and form-level exception feedback, including the custom exception renderer. Does not suppress messages independently rendered by custom fields or summaries. |
| `exceptionMessage` | `'An unexpected error occurred. Please try again.'` | Safe user-facing `string`; supports localization and preserves an explicitly empty string. |
| `exceptionDisplayComponent` | Unset | `React.ComponentType<ExceptionDisplayProps>` replacing the default exception panel; receives only `{ message: string }` and is not invoked when `showErrors` is false. |
| `fieldContainerComponent` | Unset | Component receiving `FieldContainerProps`. |
| `fieldDecoratorComponent` | Unset | Component receiving `FieldDecoratorProps`. |
| `errorDisplayComponent` | Unset | Component receiving `ErrorDisplayProps` for field validation errors, not form-level exceptions. |
| `tooltipComponent` | Unset | Component receiving `TooltipWrapperProps`. |
| `errorClassName` | `'p-error'` | Default error element's CSS class. |
| `iconAddonClassName` | `'p-inputgroup-addon'` | Default icon wrapper's CSS class. |

Exception feedback uses safe text rather than raw server diagnostics. See [safe exception customization](./customization.md#safe-exception-feedback) for localization and replacement renderers, and [exception diagnostics and display](./form-lifecycle.md#exception-diagnostics-and-display) for callback and context behavior.

## Initial values

Use `initialValues` for the initial seed and baseline, as in the example above; use `currentValues` for a reactive overlay. Changing `initialValues` alone after mount does not repopulate the command.

For an existing record, continue with [data loading](./data-loading.md) to choose when loaded values become the baseline for change tracking. Before disabling Save based on form state, read [validation](./validation.mdx): `isValid` reflects the latest applied check, not a promise that the next submission will succeed.

## Children

Place fields inside the form, using fragments, HTML elements, or custom layout components as needed. A field's `value={c => c.name}` identifies the command property it edits; it is not a callback for computing a display value. Use [custom fields](./custom-fields.md) to adapt an existing input and [customization](./customization.md) to change its surrounding presentation.

`CommandForm.Column` groups columns; its responsive styling needs the CSS described in [Layouts](./layouts.md). Descendants that need values or execution state should use the [form hooks](./hooks.md), which reach the same command instance.

<a id="see-also"></a>

## Next steps

- [Field types](./field-types/index.md): choose a control and its supported props.
- [Validation](./validation.mdx): distinguish client rules, server rules, and error display.
- [Working with hooks](./hooks.md): read and edit the same form instance.
- [Form lifecycle](./form-lifecycle.md): handle results and external execution.
- [Advanced patterns](./advanced-patterns.md): computed previews, dialogs, and autosave boundaries.
