# Creating Custom Fields

While CommandForm provides built-in field components for common scenarios, you can easily create your own custom fields to integrate with any UI library or implement specialized input controls.

## Overview

Custom fields are created using the `asCommandFormField` higher-order component (HOC), which handles all the integration with CommandForm automatically, including:

- Value synchronization with the command instance
- Change event handling
- Validation state management
- Error message display
- Required field handling

## Basic Anatomy

A custom field consists of two parts:

1. **Your component** - Receives `WrappedFieldProps<TValue>` and renders the UI
2. **Field configuration** - Specifies default value and how to extract values from change events

```tsx
import { asCommandFormField, WrappedFieldProps } from '@cratis/applications-react/commands';

// 1. Define your component props (extends WrappedFieldProps)
interface MyFieldProps extends WrappedFieldProps<string> {
    placeholder?: string;
    // Add any custom props here
}

// 2. Create the field using asCommandFormField
export const MyField = asCommandFormField<MyFieldProps>(
    // Your component implementation
    (props) => (
        <input
            value={props.value}
            onChange={props.onChange}
            placeholder={props.placeholder}
            required={props.required}
            className={props.invalid ? 'invalid' : ''}
        />
    ),
    // Configuration
    {
        defaultValue: '',
        extractValue: (e: unknown) => {
            const event = e as React.ChangeEvent<HTMLInputElement>;
            return event.target.value;
        }
    }
);
```

## WrappedFieldProps

Your component receives these props automatically from CommandForm:

| Prop | Type | Description |
|------|------|-------------|
| `value` | `TValue` | The current field value from the command instance |
| `onChange` | `(valueOrEvent: TValue \| unknown) => void` | Callback to update the value |
| `invalid` | `boolean` | Whether the field has validation errors |
| `required` | `boolean` | Whether the field is required |
| `errors` | `string[]` | Array of error messages for this field |

## Configuration Object

The second parameter to `asCommandFormField` is a configuration object:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `defaultValue` | `TValue` | Yes | Default value when the field is empty/undefined |
| `extractValue` | `(event: unknown) => TValue` | No | Function to extract the value from change events. If omitted, the event itself is used as the value. |

## Example: PrimeReact InputText

Here's a complete example of creating a custom field using PrimeReact's `InputText` component:

```tsx
import React from 'react';
import { InputText, InputTextProps } from 'primereact/inputtext';
import { asCommandFormField, WrappedFieldProps } from '@cratis/applications-react/commands';

// Define the props your field accepts, combining WrappedFieldProps with PrimeReact's InputTextProps
interface PrimeInputTextFieldProps extends WrappedFieldProps<string> {
    placeholder?: InputTextProps['placeholder'];
    maxLength?: InputTextProps['maxLength'];
    keyfilter?: InputTextProps['keyfilter'];
    size?: InputTextProps['size'];
    variant?: InputTextProps['variant'];
}

// Create the field component
export const PrimeInputTextField = asCommandFormField<PrimeInputTextFieldProps>(
    (props) => {
        const { value, onChange, invalid, required, errors, placeholder, maxLength, keyfilter, size, variant, ...rest } = props;
        
        return (
            <div className="field">
                <InputText
                    value={value}
                    onChange={onChange}
                    placeholder={placeholder}
                    maxLength={maxLength}
                    keyfilter={keyfilter}
                    size={size}
                    variant={variant}
                    required={required}
                    invalid={invalid}
                    className="w-full"
                    {...rest}
                />
                {errors.length > 0 && (
                    <div className="p-error mt-1">
                        {errors.map((error, idx) => (
                            <small key={idx} className="block">{error}</small>
                        ))}
                    </div>
                )}
            </div>
        );
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => {
            if (e && typeof e === 'object' && 'target' in e) {
                const event = e as React.ChangeEvent<HTMLInputElement>;
                return event.target.value;
            }
            return String(e || '');
        }
    }
);
```

### Usage

```tsx
import { CommandForm } from '@cratis/applications-react/commands';
import { PrimeInputTextField } from './fields/PrimeInputTextField';

interface UserCommand {
    name: string;
    email: string;
    phone: string;
}

function UserForm() {
    return (
        <CommandForm command={UserCommand}>
            <PrimeInputTextField<UserCommand>
                value={c => c.name}
                title="Full Name"
                placeholder="Enter your name"
                required
            />
            
            <PrimeInputTextField<UserCommand>
                value={c => c.email}
                title="Email Address"
                placeholder="you@example.com"
                keyfilter="email"
                required
            />
            
            <PrimeInputTextField<UserCommand>
                value={c => c.phone}
                title="Phone Number"
                placeholder="+1 (555) 123-4567"
                keyfilter="int"
            />
        </CommandForm>
    );
}
```

## Advanced Examples

### Complex Component with Multiple Elements

```tsx
interface RichTextFieldProps extends WrappedFieldProps<string> {
    maxLength?: number;
    showCharCount?: boolean;
}

export const RichTextField = asCommandFormField<RichTextFieldProps>(
    (props) => {
        const { value, onChange, invalid, required, errors, maxLength, showCharCount } = props;
        const charCount = value.length;
        
        return (
            <div className="rich-text-field">
                <div className={`input-wrapper ${invalid ? 'error' : ''}`}>
                    <textarea
                        value={value}
                        onChange={onChange}
                        maxLength={maxLength}
                        required={required}
                        className="rich-textarea"
                        rows={5}
                    />
                </div>
                
                {showCharCount && maxLength && (
                    <div className="char-count">
                        {charCount} / {maxLength}
                    </div>
                )}
                
                {errors.length > 0 && (
                    <ul className="error-list">
                        {errors.map((error, idx) => (
                            <li key={idx}>{error}</li>
                        ))}
                    </ul>
                )}
            </div>
        );
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => {
            if (e && typeof e === 'object' && 'target' in e) {
                const event = e as React.ChangeEvent<HTMLTextAreaElement>;
                return event.target.value;
            }
            return String(e || '');
        }
    }
);
```

### Non-String Values (Number Example)

```tsx
import { InputNumber, InputNumberProps } from 'primereact/inputnumber';

interface PrimeNumberFieldProps extends WrappedFieldProps<number> {
    min?: InputNumberProps['min'];
    max?: InputNumberProps['max'];
    step?: InputNumberProps['step'];
    showButtons?: InputNumberProps['showButtons'];
    currency?: InputNumberProps['currency'];
    locale?: InputNumberProps['locale'];
    mode?: InputNumberProps['mode'];
    minFractionDigits?: InputNumberProps['minFractionDigits'];
    maxFractionDigits?: InputNumberProps['maxFractionDigits'];
}

export const PrimeNumberField = asCommandFormField<PrimeNumberFieldProps>(
    (props) => {
        const { value, onChange, invalid, required, errors, min, max, step, showButtons, currency, locale, mode, minFractionDigits, maxFractionDigits } = props;
        
        return (
            <div className="field">
                <InputNumber
                    value={value}
                    onValueChange={onChange}
                    min={min}
                    max={max}
                    step={step}
                    showButtons={showButtons}
                    mode={mode}
                    currency={currency}
                    locale={locale || 'en-US'}
                    minFractionDigits={minFractionDigits}
                    maxFractionDigits={maxFractionDigits}
                    invalid={invalid}
                    className="w-full"
                />
                {errors.length > 0 && (
                    <small className="p-error">{errors.join(', ')}</small>
                )}
            </div>
        );
    },
    {
        defaultValue: 0,
        extractValue: (e: unknown) => {
            // PrimeReact InputNumber passes an InputNumberChangeEvent
            if (e && typeof e === 'object' && 'value' in e) {
                const event = e as { value: number | null };
                return event.value ?? 0;
            }
            return Number(e) || 0;
        }
    }
);
```

### Boolean Values (Toggle/Switch)

```tsx
import { InputSwitch, InputSwitchProps } from 'primereact/inputswitch';

interface PrimeSwitchFieldProps extends WrappedFieldProps<boolean> {
    trueLabel?: string;
    falseLabel?: string;
    trueValue?: InputSwitchProps['trueValue'];
    falseValue?: InputSwitchProps['falseValue'];
}

export const PrimeSwitchField = asCommandFormField<PrimeSwitchFieldProps>(
    (props) => {
        const { value, onChange, invalid, errors, trueLabel, falseLabel, trueValue, falseValue } = props;
        
        return (
            <div className="field">
                <div className="flex align-items-center gap-2">
                    <InputSwitch
                        checked={value}
                        onChange={onChange}
                        trueValue={trueValue}
                        falseValue={falseValue}
                        invalid={invalid}
                    />
                    <span className="ml-2">
                        {value ? (trueLabel || 'Yes') : (falseLabel || 'No')}
                    </span>
                </div>
                {errors.length > 0 && (
                    <small className="p-error">{errors.join(', ')}</small>
                )}
            </div>
        );
    },
    {
        defaultValue: false,
        extractValue: (e: unknown) => {
            // PrimeReact InputSwitch passes an InputSwitchChangeEvent
            if (e && typeof e === 'object' && 'value' in e) {
                const event = e as { value: boolean };
                return event.value;
            }
            return Boolean(e);
        }
    }
);
```

## Type Safety

CommandForm fields are fully type-safe when you provide the command type:

```tsx
interface ProductCommand {
    name: string;
    price: number;
    inStock: boolean;
}

// ✅ Type-safe: TypeScript knows c is ProductCommand
<PrimeInputTextField<ProductCommand> 
    value={c => c.name}  // ✅ c.name is valid
    title="Product Name" 
/>

// ✅ Type-safe: TypeScript knows c is ProductCommand
<PrimeNumberField<ProductCommand> 
    value={c => c.price}  // ✅ c.price is valid
    title="Price"
    currency="USD"
/>

// ❌ Compile error: Property 'invalid' does not exist on ProductCommand
<PrimeInputTextField<ProductCommand> 
    value={c => c.invalid}  // ❌ TypeScript error
    title="Invalid Field" 
/>
```

## Best Practices

### 1. Handle Null/Undefined Values

Always provide a sensible default value and handle null/undefined in your `extractValue`:

```tsx
{
    defaultValue: '',
    extractValue: (e: unknown) => {
        if (!e) return '';
        // ... extract logic
    }
}
```

### 2. Preserve Original Events

Some libraries need the original event object. Pass it through when possible:

```tsx
onChange={(e) => {
    // Library might need the original event
    props.onChange(e);
}}
```

### 3. Show Validation Errors

Always display the `errors` array to users:

```tsx
{errors.length > 0 && (
    <div className="error-message">
        {errors.map((error, idx) => (
            <small key={idx}>{error}</small>
        ))}
    </div>
)}
```

### 4. Apply Invalid State Styling

Use the `invalid` prop to style fields with errors:

```tsx
className={invalid ? 'p-invalid' : ''}
```

### 5. Respect the Required Flag

Pass the `required` prop to your underlying component:

```tsx
<input required={required} />
```

## Reusable Field Library

Create a library of custom fields for your organization:

```tsx
// src/components/fields/index.ts
export { PrimeInputTextField } from './PrimeInputTextField';
export { PrimeNumberField } from './PrimeNumberField';
export { PrimeSwitchField } from './PrimeSwitchField';
export { PrimeDropdownField } from './PrimeDropdownField';
export { PrimeDateField } from './PrimeDateField';
export { PrimeTextAreaField } from './PrimeTextAreaField';
```

Then use them consistently across your application:

```tsx
import { CommandForm } from '@cratis/applications-react/commands';
import {
    PrimeInputTextField,
    PrimeNumberField,
    PrimeSwitchField,
    PrimeDateField
} from '@/components/fields';

function MyForm() {
    return (
        <CommandForm command={MyCommand}>
            <PrimeInputTextField<MyCommand> value={c => c.name} title="Name" />
            <PrimeNumberField<MyCommand> value={c => c.age} title="Age" min={0} max={120} />
            <PrimeSwitchField<MyCommand> value={c => c.active} title="Active" />
            <PrimeDateField<MyCommand> value={c => c.birthDate} title="Birth Date" />
        </CommandForm>
    );
}
```

## See Also

- [CommandForm Overview](./index.md)
- [Built-in Field Types](./field-types.md)
- [Customization](./customization.md)
- [Validation](./validation.md)
