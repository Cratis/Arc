---
title: Customization
description: Customize CommandForm titles, messages, field containers, decorators, and tooltips using their actual prop contracts.
---

Change presentation without creating another command instance. CommandForm exposes rendering components for individual fields and context for form-level summaries. The examples below use the generated `UpdateProfile` from the [overview](./index.md).

## Custom titles

`showTitles={false}` hides the default titles. Supply accessible labels in your own controls rather than substituting headings and assuming they label an input. The built-in title renderer does not supply an `htmlFor`/input-ID association, and the built-in text field does not accept every native ARIA/ID prop. For explicit label associations, use a [custom field](./custom-fields.md).

## Custom error rendering

Use `showErrors={false}` to hide automatic field messages and form-level exception feedback, including any custom `exceptionDisplayComponent`, without invoking that replacement. Validation, result state, and execution callbacks still work. This complete component adds a descendant summary that reads the same form's displayed validation results, including non-field failures:

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

There is no `instance.errors` dictionary. Read `commandResult.validationResults` or use `getFieldError('email')` for the first field message. Hooks must be **below** the form provider. `showErrors` does not clear validation, change `isValid`, or suppress messages that a custom field or summary chooses to render itself. This summary handles validation results only; if you hide automatic feedback, provide your own safe exception feedback as well. Keep rule messages suitable for end users and raw diagnostics in trusted handling.

## Custom field container

A container receives exactly these props:

| Prop | Type | Meaning |
| --- | --- | --- |
| `title` | `string \| undefined` | Field title, even when `showTitles` is false. |
| `errorMessage` | `string \| undefined` | First field message; undefined when `showErrors` is false. |
| `children` | `React.ReactNode` | The field content, including the default title if `showTitles` is true. |

This complete renderer keeps the title supplied in `children` and adds a border/message:

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

Set `fieldContainerComponent={BorderedField}` on your form. If your container renders `title` itself, also set `showTitles={false}` to avoid duplicate titles. Extra props such as `helpText` are **not** automatically passed simply because you add them to a custom interface.

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

Without a custom container, `errorDisplayComponent` replaces the default field message element. This complete renderer uses the actual interface:

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

Choose the ownership boundary deliberately:

1. Keep default titles or render them yourself, not both.
2. Let either the form's error renderer or a custom container own field messages.
3. Let a custom decorator own icon/description/tooltip composition, or use the default decorator plus `tooltipComponent`.
4. Keep blocking rules in command validation. Changing presentation does not change execution semantics.

## See also

- [Creating custom fields](./custom-fields.md)
- [Field types](./field-types/index.md)
- [Validation](./validation.md)
- [Advanced patterns](./advanced-patterns.md)
