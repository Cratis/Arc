# Built-in Field Types

CommandForm provides a comprehensive set of built-in field components for common form inputs. All field components automatically integrate with the command's state management and validation system.

## Type Safety

All field components are generic and require an explicit type parameter to ensure type-safe accessor functions:

```tsx
// ✅ Correct: Full type safety with IntelliSense
<InputTextField<UserCommand> value={c => c.name} title="Name" />

// ❌ Incorrect: Missing type parameter - 'c' will be 'unknown'
<InputTextField value={c => c.name} title="Name" />
```

The type parameter ensures the `value` accessor function parameter `c` is properly typed as your command class.

## Common Props

All field components share these base props:

| Prop | Type | Description |
|------|------|-------------|
| `value` | `(instance: TCommand) => unknown` | **Required.** Accessor function that returns the property value from the command instance. |
| `title` | `string` | The label for the field (shown when `showTitles` is enabled). |
| `required` | `boolean` | Marks the field as required for validation. **Automatically determined** from the command property's `PropertyDescriptor.isOptional` (required if not optional). Only specify explicitly to override the automatic behavior. |

> **Note**: Fields are automatically marked as required based on the command property's nullability. In C#, non-nullable properties generate `PropertyDescriptor` with `isOptional: false`, making those fields required by default. You only need to specify `required` explicitly when you want to override this behavior.

## InputTextField

A versatile text input field that supports multiple input types through HTML5 input elements.

### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `type` | `'text' \| 'email' \| 'password' \| 'color' \| 'date' \| 'datetime-local' \| 'time' \| 'url' \| 'tel' \| 'search'` | `'text'` | The HTML input type. |
| `placeholder` | `string` | - | Placeholder text shown when empty. |

### Examples

**Text Input:**

```tsx
<InputTextField<UserCommand> value={c => c.name} title="Full Name" placeholder="Enter your name" />
```

**Email Input:**

```tsx
<InputTextField<UserCommand> value={c => c.email} type="email" title="Email" placeholder="your@email.com" />
```

**Password Input:**

```tsx
<InputTextField<UserCommand> value={c => c.password} type="password" title="Password" />
```

**Date Input:**

```tsx
<InputTextField<UserCommand> value={c => c.birthDate} type="date" title="Birth Date" />
```

**Color Picker:**

```tsx
<InputTextField<UserCommand> value={c => c.favoriteColor} type="color" title="Favorite Color" />
```

**URL Input:**

```tsx
<InputTextField<UserCommand> value={c => c.website} type="url" title="Website" placeholder="https://example.com" />
```

## NumberField

A specialized field for numeric input with support for min/max constraints and step increments.

### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `placeholder` | `string` | - | Placeholder text shown when empty. |
| `min` | `number` | - | Minimum allowed value. |
| `max` | `number` | - | Maximum allowed value. |
| `step` | `number` | - | Increment/decrement step size. |

### Example

```tsx
<NumberField<UserCommand>
    value={c => c.age} 
    title="Age" 
    min={0} 
    max={120} 
    step={1}
/>

<NumberField<ProductCommand>
    value={c => c.price} 
    title="Price" 
    min={0} 
    step={0.01}
    placeholder="0.00"
/>
```

## TextAreaField

A multi-line text input field for longer content.

### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `placeholder` | `string` | - | Placeholder text shown when empty. |
| `rows` | `number` | `5` | Number of visible text rows. |
| `cols` | `number` | - | Number of visible text columns. |

### Example

```tsx
<TextAreaField<UserCommand>
    value={c => c.bio} 
    title="Biography" 
    placeholder="Tell us about yourself..."
    rows={8}
/>

<TextAreaField<NoteCommand>
    value={c => c.notes} 
    title="Additional Notes" 
    rows={3}
/>
```

## CheckboxField

A checkbox input for boolean (true/false) values.

### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `label` | `string` | - | Label text displayed next to the checkbox. |

### Example

```tsx
<CheckboxField<UserCommand>
    value={c => c.agreeToTerms} 
    title="Terms of Service"
    label="I agree to the terms and conditions" 
/>

<CheckboxField<UserCommand>
    value={c => c.newsletter} 
    title="Newsletter"
    label="Send me newsletter updates" 
/>
```

## RangeField

A slider input for selecting a numeric value within a range.

### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `min` | `number` | `0` | Minimum value of the range. |
| `max` | `number` | `100` | Maximum value of the range. |
| `step` | `number` | `1` | Increment step size. |

### Example

```tsx
<RangeField<AudioCommand>
    value={c => c.volume} 
    title="Volume" 
    min={0} 
    max={100} 
    step={1}
/>

<RangeField<UserCommand>
    value={c => c.experience} 
    title="Years of Experience" 
    min={0} 
    max={50} 
    step={1}
/>
```

The RangeField displays the current value next to the slider for immediate feedback.

## SelectField

A dropdown select field for choosing from a list of options.

### Props

| Prop | Type | Description |
|------|------|-------------|
| `options` | `Array<{ [key: string]: unknown }>` | **Required.** Array of option objects. |
| `optionIdField` | `string` | **Required.** Name of the property to use as the option value. |
| `optionLabelField` | `string` | **Required.** Name of the property to display as the option label. |
| `placeholder` | `string` | Placeholder text shown for empty selection. |

### Example

```tsx
const countries = [
    { id: 'us', name: 'United States' },
    { id: 'uk', name: 'United Kingdom' },
    { id: 'ca', name: 'Canada' },
    { id: 'no', name: 'Norway' }
];

<SelectField<UserCommand>
    value={c => c.country} 
    title="Country"
    options={countries}
    optionIdField="id"
    optionLabelField="name"
    placeholder="Select a country..."
/>
```

With custom data:

```tsx
const roles = [
    { value: 'admin', display: 'Administrator' },
    { value: 'user', display: 'Standard User' },
    { value: 'guest', display: 'Guest' }
];

<SelectField<UserCommand>
    value={c => c.role} 
    title="User Role"
    options={roles}
    optionIdField="value"
    optionLabelField="display"
/>
```

## Field State

All field components automatically handle:

- **Value Management**: Values are synchronized with the command instance
- **Validation**: Required fields and type validation are enforced
- **Error Display**: Invalid fields show error styling (red border)
- **Change Tracking**: Changes are detected and the command's `hasChanges` property is updated

## Styling

Field components use a consistent styling approach:

- Full width by default (`width: 100%`)
- Standard padding (`padding: 0.75rem`)
- Rounded corners (`border-radius: 0.375rem`)
- Border color adapts to validation state (gray for valid, red for invalid)
- Responsive to theme (light/dark mode support)

## Creating Custom Fields

To create a custom field component that integrates with CommandForm, use the `asCommandFormField` wrapper:

```tsx
import { asCommandFormField, WrappedFieldProps } from '@cratis/applications-react/commands';

interface MyCustomFieldProps extends WrappedFieldProps<string> {
    customProp?: string;
}

export const MyCustomField = asCommandFormField<MyCustomFieldProps>(
    (props) => (
        <input
            value={props.value}
            onChange={props.onChange}
            required={props.required}
            className={props.invalid ? 'error' : ''}
        />
    ),
    {
        defaultValue: '',
        extractValue: (e: unknown) => {
            // Extract value from event or handle direct value
            if (e && typeof e === 'object' && 'target' in e) {
                return (e.target as HTMLInputElement).value;
            }
            return String(e || '');
        }
    }
);
```

See [Customization](./customization.md) for more details on creating custom field components.

## See Also

- [CommandForm Overview](./index.md)
- [Validation](./validation.md)
- [Customization](./customization.md)
