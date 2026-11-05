---
title: CommandForm labels, errors, and presentation
description: Customize Cratis Arc CommandForm labels, error messages, containers, and tooltips without changing the command's values or validation rules.
---

Customize Cratis Arc `CommandForm` through its rendering props when you need different labels, error messages, or field layout without changing how the command executes. Use form context for summaries that span fields. The examples use the generated `UpdateProfile` from the [setup page](./index.md); they keep the existing form-owned command.

## Custom titles

`showTitles={false}` hides the default titles. Supply accessible labels in your own controls rather than substituting headings and assuming they label an input. For single-input fields, the built-in title renderer associates its label with the control using a generated id, or a caller-provided `id` when supplied. Custom single-input fields must forward that id to their input. For radio groups and custom grouped fields, the title names the group without activating an option; [custom fields](./custom-fields.md#configuration-object) explains how to declare a group.

## Custom error rendering

To collect validation messages in one place, hide automatic feedback with `showErrors={false}` and render a summary inside the form. This also hides form-level exception feedback and prevents a custom `exceptionDisplayComponent` from being invoked; it does not disable validation or execution callbacks. The summary below reads the same form's displayed validation results, including failures not attached to a field:

```tsx
import { CommandForm, InputTextField, useCommandFormContext } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

function ErrorSummary() {
    const { commandResult } = useCommandFormContext<UpdateProfile>();
    const results = commandResult?.validationResults ?? [];
    if (results.length === 0) return null;
    return (
        <div role="alert">
            <h3>Please review the profile</h3>
            <ul>
                {results.map((result, index) => (
                    <li key={index}>
                        {result.reason === 'rule' ? result.message : 'The request could not be validated. Please try again.'}
                    </li>
                ))}
            </ul>
        </div>
    );
}

export function ProfileForm() {
    return (
        <CommandForm command={UpdateProfile} initialValues={{ name: '', email: '' }} showErrors={false}>
            <ErrorSummary />
            <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
            <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

Read messages from `commandResult.validationResults`, or call `getFieldError('email')` for the first field message; there is no `instance.errors` dictionary. Keep the component using these hooks beneath the form provider. `showErrors={false}` neither clears validation nor hides markup rendered by a custom field or summary.

This summary covers validation, not exceptions. If you hide automatic feedback, also provide [safe exception feedback](#safe-exception-feedback). Rule messages should be suitable for users; send raw diagnostics only to trusted handling.

## Custom field container

A container receives exactly these props:

| Prop | Type | Meaning |
| --- | --- | --- |
| `title` | `string \| undefined` | Field title, even when `showTitles` is false. |
| `errorMessage` | `string \| undefined` | First field message; undefined when `showErrors` is false. |
| `children` | `React.ReactNode` | The field content, including the default title if `showTitles` is true. |

The container below adds a border and error message while retaining the title already supplied in `children`:

```tsx
import { type FieldContainerProps } from '@cratis/arc.react/commands';

export function BorderedField({ errorMessage, children }: FieldContainerProps) {
    return (
        <div style={{ border: `1px solid ${errorMessage ? '#c00' : '#888'}`, padding: '1rem', marginBottom: '1rem' }}>
            {children}
            {errorMessage && <p role="alert">{errorMessage}</p>}
        </div>
    );
}
```

Set `fieldContainerComponent={BorderedField}` on your form to use it. If you change the container to render `title` itself, also set `showTitles={false}` so the title appears only once. Adding a property such as `helpText` to a custom interface does not make the form pass it; the container receives the props listed above.

A custom container owns field error rendering: the form does not also render `errorDisplayComponent` outside it. Choose either a container-owned message or the normal error display, rather than expecting both to compose.

## Custom field decorator

A decorator runs for fields with an `icon` or `description`. This complete renderer uses native CSS and a browser tooltip:

```tsx
import { type FieldDecoratorProps } from '@cratis/arc.react/commands';

export function InlineDecorator({ icon, description, children }: FieldDecoratorProps) {
    return (
        <div title={description} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            {icon && <span aria-hidden="true">{icon}</span>}
            {children}
        </div>
    );
}
```

| Prop | Type |
| --- | --- |
| `icon` | `React.ReactElement \| undefined` |
| `description` | `string \| undefined` |
| `children` | `React.ReactNode` |

Set `fieldDecoratorComponent={InlineDecorator}` on the form. A custom decorator replaces default icon/description decoration, including the default `tooltipComponent` path. Render its description or tooltip yourself. Do not put essential instructions only in a hover tooltip.

## Custom error display

If you want to replace only the field message, use `errorDisplayComponent` without a custom container. The renderer receives the field name and selected errors:

```tsx
import { type ErrorDisplayProps } from '@cratis/arc.react/commands';

export function FieldErrors({ errors, fieldName }: ErrorDisplayProps) {
    return (
        <div role="alert" data-field={fieldName}>
            {errors.map((error, index) => <p key={index} style={{ color: '#c00' }}>{error}</p>)}
        </div>
    );
}
```

`errors` is `string[]`; `fieldName` is optional. The current form passes its selected first field message as a one-element array, not all backend messages for that field. Use a context summary for the full result.

## Safe exception feedback

Unexpected failures should tell users what to do next, not expose server diagnostics. By default, CommandForm shows an accessible alert containing only:

> An unexpected error occurred. Please try again.

Set `exceptionMessage` to safe application-specific or localized text. An explicitly empty string stays empty rather than falling back to the default. This **configuration fragment** assumes the `CommandForm` and generated `UpdateProfile` imports from the overview:

```tsx
<CommandForm
    command={UpdateProfile}
    exceptionMessage="We couldn't save your changes. Please try again."
/>
```

To replace the entire default panel, provide `exceptionDisplayComponent`. It receives only `{ message: string }` through the exported `ExceptionDisplayProps` interface, never the command result, diagnostic messages, or stack trace. This complete component supplies accessible feedback in its replacement:

```tsx
import { CommandForm, InputTextField, type ExceptionDisplayProps } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

function ExceptionNotice({ message }: ExceptionDisplayProps) {
    return <div className="exception-notice" role="alert">{message}</div>;
}

export function ProfileForm() {
    return (
        <CommandForm
            command={UpdateProfile}
            initialValues={{ name: '', email: '' }}
            exceptionMessage="We couldn't save your changes. Please try again."
            exceptionDisplayComponent={ExceptionNotice}
        >
            <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
            <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

`errorDisplayComponent` remains exclusively for field validation errors; CommandForm never calls it for a form-level exception. `showErrors={false}` hides both the default exception panel and the custom replacement without invoking the replacement.

Treat `exceptionMessage` as user-facing content: do not populate it from raw exception diagnostics. Original diagnostics remain available through [execution callbacks and result state](./form-lifecycle.md#exception-diagnostics-and-display) for trusted handling, not automatic display.

## Custom tooltip component

`tooltipComponent` receives `{ description: string; children: React.ReactNode }` and wraps the default decorated field. The following **optional PrimeReact renderer** requires a compatible installed PrimeReact package and its theme setup; it is not an Arc dependency:

```tsx
import { useId } from 'react';
import { Tooltip } from 'primereact/tooltip';
import { type TooltipWrapperProps } from '@cratis/arc.react/commands';

export function PrimeTooltip({ description, children }: TooltipWrapperProps) {
    const id = `field-tooltip-${useId().replace(/[^a-zA-Z0-9_-]/g, '')}`;
    return (
        <>
            <div id={id} data-pr-tooltip={description}>{children}</div>
            <Tooltip target={`#${id}`} />
        </>
    );
}
```

The ID remains stable across renders. Use this via `tooltipComponent={PrimeTooltip}` when no custom decorator replaces that path.

## Custom CSS classes

`errorClassName` defaults to `p-error`; `iconAddonClassName` defaults to `p-inputgroup-addon`. Class names alone do not install CSS. This **configuration fragment** assumes `CommandForm`, `InputTextField`, and `UpdateProfile` imports and application CSS for the two classes:

```tsx
<CommandForm command={UpdateProfile} errorClassName="profile-error" iconAddonClassName="profile-icon">
    <InputTextField<UpdateProfile> value={c => c.email} title="Email" icon={<span>@</span>} />
</CommandForm>
```

The default error element also has inline styles, including color, so a class declaration may not override every property. Use `errorDisplayComponent` for complete styling control.

## Framework integration examples

PrimeReact and Tailwind are optional presentation choices, not CommandForm requirements. For PrimeReact inputs, use the [custom InputText adapter](./custom-fields.md#example-primereact-inputtext) and install the CSS required by your chosen PrimeReact version. PrimeFlex utility classes require PrimeFlex (or equivalent rules), separately from a PrimeReact theme.

For a Tailwind application, supply typed `FieldDecoratorProps`/`ErrorDisplayProps` components containing your utility classes and ensure the application scans those files. Do not assume Tailwind recognizes PrimeFlex class names such as `flex-column` or `md:flex-row` with identical semantics.

## Combining customizations

Start with the smallest replacement that meets the design. An error renderer changes messages without taking over field layout; a custom container owns both layout and field error rendering. A custom decorator takes over the icon, description, and tooltip path, so it replaces rather than wraps the default tooltip integration.

Keep one owner for each title and message to avoid duplicate output. These choices change presentation, not blocking rules or execution behavior. If the input itself needs a different value or event API, build a [custom field adapter](./custom-fields.md) instead.

## See also

- [Creating custom fields](./custom-fields.md)
- [Field types](./field-types/index.md)
- [Validation](./validation.mdx)
- [Advanced patterns](./advanced-patterns.md)
