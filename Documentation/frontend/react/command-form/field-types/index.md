# Field Types

CommandForm provides a comprehensive set of built-in field components for common form inputs. All field components automatically integrate with the command's state management and validation system.

## Type Safety

Most field components use an explicit type parameter to ensure type-safe accessor functions:

```tsx
// ✅ Correct: Full type safety with IntelliSense
<InputTextField<UserCommand> value={c => c.name} title="Name" />

// ❌ Incorrect: Missing type parameter - 'c' will be 'unknown'
<InputTextField value={c => c.name} title="Name" />
```

The type parameter ensures the `value` accessor function parameter `c` is properly typed as your command class.

`RadioButtonField` and `RadioGroupField` also infer the selected value type from the accessor return type. When you do not provide a JSX type argument, annotate the accessor parameter inline:

```tsx
<RadioGroupField
    value={(c: UserCommand) => c.role}
    options={[
        { value: 'reader', label: 'Reader' },
        { value: 'admin', label: 'Administrator' }
    ]}
/>
```

## Common Props

All field components share these base props:

| Prop | Type | Description |
|------|------|-------------|
| `value` | `(instance: TCommand) => unknown` | **Required.** Accessor function that returns the property value from the command instance. |
| `title` | `string` | The label for the field (shown when `showTitles` is enabled). |
| `required` | `boolean` | Marks the field as required for validation. **Automatically determined** from the command property's `PropertyDescriptor.isOptional` (required if not optional). Only specify explicitly to override the automatic behavior. |

> **Note**: Fields are automatically marked as required based on the command property's nullability. In C#, non-nullable properties generate `PropertyDescriptor` with `isOptional: false`, making those fields required by default. You only need to specify `required` explicitly when you want to override this behavior.

## Field State

All field components automatically handle:

- **Value Management**: Values are synchronized with the command instance
- **Validation**: Required fields and type validation are enforced
- **Error Display**: Invalid fields show error styling (red border)
- **Change Tracking**: Changes are detected and the command's `hasChanges` property is updated

## Available Fields

- [InputTextField](./input-text-field.md) - Text, email, password, date, color, and other text-based inputs
- [NumberField](./number-field.md) - Numeric inputs with min, max, and step handling
- [TextAreaField](./text-area-field.md) - Multi-line text input
- [CheckboxField](./checkbox-field.md) - Boolean toggle input
- [RadioButtonField](./radio-button-field.md) - Single radio option that assigns a specific value
- [RadioGroupField](./radio-group-field.md) - Radio option list for one-of-many selections
- [RangeField](./range-field.md) - Slider input for numeric ranges
- [SelectField](./select-field.md) - Dropdown selection input

## Styling

Field components use a consistent styling approach:

- Full width by default (`width: 100%`)
- Standard padding (`padding: 0.75rem`)
- Rounded corners (`border-radius: 0.375rem`)
- Border color adapts to validation state (gray for valid, red for invalid)
- Responsive to theme (light/dark mode support)

## See Also

- [CommandForm Overview](../index.md)
- [Validation](../validation.md)
- [Creating Custom Fields](../custom-fields.md)
