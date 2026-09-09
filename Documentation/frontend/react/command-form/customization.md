---
title: Customization
description: Customize CommandForm titles, messages, field containers, decorators, and tooltips using their actual prop contracts.
---

Change presentation without creating another command instance. CommandForm exposes rendering components for individual fields and context for form-level summaries. The examples below use the generated `UpdateProfile` from the [overview](./index.md).

## Custom titles

`showTitles={false}` hides the default titles. Supply accessible labels in your own controls rather than substituting headings and assuming they label an input. The built-in title renderer does not supply an `htmlFor`/input-ID association, and the built-in text field does not accept every native ARIA/ID prop. For explicit label associations, use a [custom field](./custom-fields.md).

## Custom error rendering

Use `showErrors={false}` to hide the form's automatic field messages and add a descendant summary. This complete component reads the same form's displayed result, including non-field failures:

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

There is no `instance.errors` dictionary. Read `commandResult.validationResults` or use `getFieldError('email')` for the first field message. Hooks must be **below** the form provider. `showErrors` does not clear validation, change `isValid`, or suppress messages that a custom field chooses to render itself. It also does not suppress the form's automatic `exceptionMessages` block. A friendly summary only changes its own text; ensure server exception messages are safe for end users rather than treating this summary as global sanitization.

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
