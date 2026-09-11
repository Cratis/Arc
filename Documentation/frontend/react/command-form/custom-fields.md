---
title: Creating custom fields
description: Adapt a native or library control to CommandForm while preserving change, blur, and validation behavior.
---

Use a custom field when the built-in props do not cover your control—for example, a text input with an explicit accessible label and `maxLength`. `asCommandFormField` connects the control to the surrounding form. It does not turn HTML attributes into command rules.

<a id="overview"></a>

## Basic anatomy

This is a complete `fields/MyField.tsx` module. It converts native change events to strings and forwards the form's **blur callback**, so the default blur-validation behavior still runs.

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
        const id = useId();
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

The extractor above expects a native input change event because that is the only event this control emits. If your control also emits values directly, handle both shapes explicitly. The component lets CommandForm render messages, rather than rendering the same errors a second time.

Use a **generated command class** as the constructor. This complete form uses `UpdateProfile` from the [overview](./index.md), not an interface named like a command:

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

No `title` is supplied because each control renders its own label. `maxLength` constrains normal typing but does not validate programmatically supplied values. CommandForm uses `noValidate`; keep length/format/required rules on the command.

## WrappedFieldProps

| Prop | Type | Contract |
| --- | --- | --- |
| `value` | `TValue` | Current display value; configured fallback is used only for `undefined`, not `null`. |
| `onChange` | `(valueOrEvent: TValue \| unknown) => void` | Forward the control's change event/value for extraction and binding. |
| `onBlur` | `(() => void) \| undefined` | Forward to the control's blur event to preserve form blur validation. |
| `invalid` | `boolean` | Whether this field has a displayed error. |
| `required` | `boolean` | Explicit control override or inferred descriptor flag, not a new command rule. |
| `errors` | `string[]` | Messages made available to the adapter; rendering them yourself is optional. |

## Configuration object

The configuration requires `defaultValue: TValue` and optionally accepts `extractValue: (event: unknown) => TValue`. Without an extractor the emitted value is used directly. A fallback only changes display; it does not seed the command or its baseline. Define a deliberate null/empty policy for numeric/date controls rather than silently converting every empty value to a business value.

## How a field is recognized

`asCommandFormField` marks the returned component with a static `isCommandFormField` property. Its `displayName` is `CommandFormField` as a compatibility fallback. It also binds itself when an opaque layout creates or transforms the field at render time, so custom layouts can preserve the original element type and change its presentation/binding props before registration.

Binding uses a private framework marker, not the presence of an `onValueChange` prop. A layout can wrap `onValueChange` without disabling binding; the form updates the command before invoking that consumer callback. Do not treat that callback's presence as proof of prior binding.

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
        const id = useId();
        const { label, value, onChange, invalid, required, errors, ...rest } = props;
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

`onBlur` deliberately remains in `...rest`, which is spread onto `InputText`; **blur is forwarded**. If you destructure it later, add `onBlur={onBlur}` explicitly. Do not remove that forwarding. The adapter excludes `errors` from the input and leaves messages to the form to avoid duplicate error text.

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

Forward blur as well as change. Choose one owner for error display; custom fields that render `errors` themselves must opt out of the form's display to avoid duplicates, and `showErrors={false}` does not suppress an adapter's own markup. Associate labels and messages with controls in your adapter, test keyboard navigation, and keep blocking rules on the command.

## Reusable field library

Export the adapters you actually implement from your application's field module. Keep generated commands in the configured proxy output, and import those classes when constructing forms. Do not substitute application interfaces for executable command constructors.

## See also

- [Field types](./field-types/index.md)
- [Customization](./customization.md)
- [Validation](./validation.md)
- [CommandForm overview](./index.md)
