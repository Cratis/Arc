# Built-in Field Types

CommandForm provides a comprehensive set of built-in field components for common form inputs. All field components automatically integrate with the command's state management and validation system.

## Common Props

All field components share these base props:

| Prop | Type | Description |
|------|------|-------------|
| `property` | `string` | **Required.** The property name on the command. |
| `title` | `string` | The label for the field (shown when `showTitles` is enabled). |
| `required` | `boolean` | Marks the field as required for validation. |

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
<InputTextField property="name" title="Full Name" placeholder="Enter your name" />
```

**Email Input:**

```tsx
<InputTextField property="email" type="email" title="Email" placeholder="your@email.com" required />
```

**Password Input:**

```tsx
<InputTextField property="password" type="password" title="Password" required />
```

**Date Input:**

```tsx
<InputTextField property="birthDate" type="date" title="Birth Date" />
```

**Color Picker:**

```tsx
<InputTextField property="favoriteColor" type="color" title="Favorite Color" />
```

**URL Input:**

```tsx
<InputTextField property="website" type="url" title="Website" placeholder="https://example.com" />
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
<NumberField 
    property="age" 
    title="Age" 
    min={0} 
    max={120} 
    step={1}
    required 
/>

<NumberField 
    property="price" 
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
<TextAreaField 
    property="bio" 
    title="Biography" 
    placeholder="Tell us about yourself..."
    rows={8}
/>

<TextAreaField 
    property="notes" 
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
<CheckboxField 
    property="agreeToTerms" 
    title="Terms of Service"
    label="I agree to the terms and conditions" 
    required 
/>

<CheckboxField 
    property="newsletter" 
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
<RangeField 
    property="volume" 
    title="Volume" 
    min={0} 
    max={100} 
    step={1}
/>

<RangeField 
    property="experience" 
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

<SelectField 
    property="country" 
    title="Country"
    options={countries}
    optionIdField="id"
    optionLabelField="name"
    placeholder="Select a country..."
    required
/>
```

With custom data:

```tsx
const roles = [
    { value: 'admin', display: 'Administrator' },
    { value: 'user', display: 'Standard User' },
    { value: 'guest', display: 'Guest' }
];

<SelectField 
    property="role" 
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
