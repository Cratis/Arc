# RadioGroupField

A grouped radio input for selecting one value from a predefined set of options.

`RadioGroupField` infers each option value type from the bound accessor. That keeps every option aligned with the command property it updates.

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `value` | `(instance: TCommand) => TValue` | | **Required.** Accessor function returning the property value from the command instance. |
| `options` | `RadioGroupFieldOption<TValue>[]` | | **Required.** Options rendered by the group. Each `value` must match the accessor return type. |
| `direction` | `'horizontal' \| 'vertical'` | `'vertical'` | Layout direction for the options. |
| `title` | `string` | | The label for the field (shown when `showTitles` is enabled). |
| `required` | `boolean` | | Override automatic required detection. |

## Example

```tsx
<RadioGroupField
    value={(c: UserSettingsCommand) => c.role}
    title="Role"
    direction="horizontal"
    options={[
        { value: 'reader', label: 'Reader' },
        { value: 'admin', label: 'Administrator' },
        { value: 'owner', label: 'Owner' }
    ]}
/>
```

## See Also

- [RadioButtonField](./radio-button-field.md)
- [Field Types Overview](./index.md)
- [CommandForm Overview](../index.md)
