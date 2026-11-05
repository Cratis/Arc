---
title: CommandForm custom field adapters
description: Adapt native or library inputs to Cratis Arc CommandForm with asCommandFormField, forwarding changes and blur events to the form-owned command.
---

Use `asCommandFormField` in Cratis Arc to connect a native or library input to the command already owned by `CommandForm`. An adapter lets you keep your input's presentation and event API while the form handles binding and validation feedback. HTML attributes such as `maxLength` still need corresponding command rules.

<a id="overview"></a>

## Basic anatomy

Create `fields/MyField.tsx` with the adapter below. `extractValue` takes the text from a native change event; forwarding `onBlur` lets the form run its default blur validation. The input renders its own accessible label and accepts `maxLength`, which the built-in field does not expose.

```tsx
import { useId, type ChangeEvent } from 'react';
import { asCommandFormField, type WrappedFieldProps } from '@cratis/arc.react/commands';

interface MyFieldProps extends WrappedFieldProps<string> {
    label: string;
    placeholder?: string;
    maxLength?: number;
}

export const MyField = asCommandFormField<MyFieldProps>(
    function TextInput(props: MyFieldProps) {
        const generatedId = useId();
        const id = props.id ?? generatedId;
        return (
            <div>
                <label htmlFor={id}>{props.label}</label>
                <input
                    id={id}
                    value={props.value ?? ''}
                    onChange={props.onChange}
                    onBlur={props.onBlur}
                    required={props.required}
                    aria-invalid={props.invalid}
                    placeholder={props.placeholder}
                    maxLength={props.maxLength}
                />
            </div>
        );
    },
    {
        defaultValue: '',
        extractValue: (event: unknown) => (event as ChangeEvent<HTMLInputElement>).target.value
    }
);
```

Typing calls the extractor with the native input event, then updates the bound command property. A control that emits both events and values needs an extractor that handles both shapes. This adapter leaves messages to CommandForm so each error appears once.

Mount it inside a form using the generated `UpdateProfile` from the [setup example](./index.md):

```tsx
import { CommandForm } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';
import { MyField } from './fields/MyField';

export function ProfileForm() {
    return (
        <CommandForm command={UpdateProfile} initialValues={{ name: '', email: '' }}>
            <MyField<UpdateProfile> value={c => c.name} label="Name" maxLength={100} />
            <MyField<UpdateProfile> value={c => c.email} label="Email" />
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

No `title` is supplied because each control renders its own label. If you use a form-rendered `title` instead, forward `props.id` to the control; do not render a second label with the same text. `maxLength` constrains normal typing but does not validate programmatically supplied values. CommandForm uses `noValidate`; keep length/format/required rules on the command.

## WrappedFieldProps

| Prop | Type | Contract |
| --- | --- | --- |
| `value` | `TValue` | Current display value; configured fallback is used only for `undefined`, not `null`. |
| `id` | `string \| undefined` | Forward to the rendered input for a single-input field, or to the group container for a grouped field. A caller-supplied id takes precedence over the generated one. |
| `onChange` | `(valueOrEvent: TValue \| unknown) => void` | Forward the control's change event/value for extraction and binding. |
| `onBlur` | `(() => void) \| undefined` | Forward to the control's blur event to preserve form blur validation. |
| `invalid` | `boolean` | Whether this field has a displayed error. |
| `required` | `boolean` | Explicit control override or inferred descriptor flag, not a new command rule. |
| `errors` | `string[]` | Messages made available to the adapter; rendering them yourself is optional. |

## Configuration object

Set `defaultValue: TValue` for the display fallback and, when the control emits an event, supply `extractValue: (event: unknown) => TValue`. Without an extractor the emitted value is used directly. The fallback only changes what the control displays; it does not initialize the command or its change-tracking baseline. For numeric and date controls, decide whether an empty input means `null`, `undefined`, or another value before implementing the conversion.

For a field with multiple inputs, set `groupRole: 'group'` (or `'radiogroup'` for radio options) in the `asCommandFormField` configuration alongside `defaultValue` and any `extractValue`. CommandForm then renders the `title` as the group's accessible name, not as a label for the first option. Forward `props.id` to the group container if it needs an id; never put it on one of the options. Label each option individually. The form-rendered title is omitted when `showTitles={false}`; in that case provide an accessible group name yourself. Existing single-input adapters need no configuration change.

## How a field is recognized

`asCommandFormField` marks the returned component with a static `isCommandFormField` property and sets `displayName` to `CommandFormField` as a compatibility fallback. This lets the form recognize the adapter among its children. The adapter can also bind itself when a custom layout creates or transforms it during rendering, where the form cannot inspect the field in advance.

Keep the original field type when a layout changes presentation or binding props. A private framework marker tracks binding; an `onValueChange` prop does not. You can wrap that callback for your own notification, and the form updates the command before calling it.

:::caution[Keep command-form field markers intact]
Do not strip field static markers. If both recognition markers disappear, CommandForm no longer discovers the field. In development, an unresolved accessor produces a warning; production remains non-throwing and unbound. Use a simple accessor such as `c => c.name` rather than a computed expression.
:::

For a hand-written adapter, prefer `withCommandFormFieldBinding` over marker-only registration. This complete alternative module includes the injected blur contract:

```tsx
import {
    withCommandFormFieldBinding,
    type CommandFormFieldProps,
    type InjectedCommandFormFieldProps
} from '@cratis/arc.react/commands';

type HandRolledProps = CommandFormFieldProps & InjectedCommandFormFieldProps;

export const HandRolledField = withCommandFormFieldBinding((props: HandRolledProps) => (
    <input
        id={props.id}
        aria-label={props.title}
        value={String(props.currentValue ?? '')}
        onChange={event => props.onValueChange?.(event.target.value)}
        onBlur={props.onBlur}
        required={props.required}
    />
));
```

Supply a `title` when using that adapter to provide its accessible name. `markAsCommandFormField` remains supported for compatibility through visible child trees; `withCommandFormFieldBinding` is the choice for fields crossing custom component boundaries. Neither replaces error rendering by the surrounding form.

## Example: PrimeReact InputText

This **optional integration module**, `fields/PrimeInputTextField.tsx`, needs a compatible installed PrimeReact package and its theme setup. It does not require Chronicle or make PrimeReact an Arc dependency.

```tsx
import { useId, type ChangeEvent } from 'react';
import { InputText, type InputTextProps } from 'primereact/inputtext';
import { asCommandFormField, type WrappedFieldProps } from '@cratis/arc.react/commands';

interface PrimeInputTextFieldProps extends WrappedFieldProps<string> {
    label: string;
    placeholder?: InputTextProps['placeholder'];
    maxLength?: InputTextProps['maxLength'];
    type?: InputTextProps['type'];
}

export const PrimeInputTextField = asCommandFormField<PrimeInputTextFieldProps>(
    function PrimeTextInput(props: PrimeInputTextFieldProps) {
        const generatedId = useId();
        const { label, value, onChange, invalid, required, errors, id: providedId, ...rest } = props;
        const id = providedId ?? generatedId;
        void errors;
        return (
            <div>
                <label htmlFor={id}>{label}</label>
                <InputText
                    id={id}
                    value={value ?? ''}
                    onChange={onChange}
                    invalid={invalid}
                    required={required}
                    style={{ width: '100%' }}
                    {...rest}
                />
            </div>
        );
    },
    {
        defaultValue: '',
        extractValue: (event: unknown) => (event as ChangeEvent<HTMLInputElement>).target.value
    }
);
```

Here `onBlur` reaches `InputText` through `...rest`, preserving blur validation. If you later destructure it, forward it explicitly with `onBlur={onBlur}`. The adapter removes `errors` before spreading props onto the input because CommandForm owns the error text.

### Usage

This complete usage component supplies the real command constructor and all adapter-required props:

```tsx
import { CommandForm } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';
import { PrimeInputTextField } from './fields/PrimeInputTextField';

export function ProfileForm() {
    return (
        <CommandForm command={UpdateProfile} initialValues={{ name: '', email: '' }}>
            <PrimeInputTextField<UpdateProfile> value={c => c.name} label="Name" maxLength={100} />
            <PrimeInputTextField<UpdateProfile> value={c => c.email} label="Email" type="email" />
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

## Advanced examples

Apply the same adapter contract to rich text, numeric inputs, and switches:

- **Text area:** forward `onChange` and `onBlur`; extract `ChangeEvent<HTMLTextAreaElement>.target.value`. Add `rows`/`maxLength` only if your adapter forwards them.
- **PrimeReact InputNumber:** use its `onValueChange` event and extract its `value`, deciding explicitly what `null` means. Forward `onBlur` and any required/invalid attributes supported by your installed version.
- **Switch:** bind `checked`, extract the library's boolean value, and forward `onBlur`. A required flag does not require the value to be true; consent needs a command rule.

These are adapter design notes, not drop-in components. Check the installed control's prop/event definitions before implementing an adapter; a wrapper cannot safely assume all libraries emit native events.

## Type safety

Supply the command type in JSX (`<MyField<UpdateProfile> ... />`) or annotate the accessor parameter. This checks property names. Most generic field accessors return `unknown`, so a valid property name alone does not prove its value type matches the control; do not bind a `Date` or number to a string adapter without an explicit conversion policy.

## Best practices

Check the adapter with both typing and keyboard focus changes: omitting `onBlur` can leave the value bound while silently losing blur validation. Associate labels and messages with the rendered controls. If the adapter displays `errors` itself, turn off the form's competing display; `showErrors={false}` does not hide markup your adapter renders. Keep blocking rules on the command so they also apply to programmatically supplied values.

## Reusable field library

Export reusable adapters from your application's field module and keep generated command classes in the configured proxy output. Reuse the adapter across forms by supplying each form's command type and property accessor. For shared labels, containers, and error presentation, continue with [CommandForm customization](./customization.md).

## See also

- [Field types](./field-types/index.md)
- [Customization](./customization.md)
- [Validation](./validation.mdx)
- [CommandForm overview](./index.md)
