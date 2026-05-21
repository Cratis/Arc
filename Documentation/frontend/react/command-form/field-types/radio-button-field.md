# RadioButtonField

A single radio input for assigning a specific value to a command property.

`RadioButtonField` infers the allowed `setValue` type from the bound accessor. That keeps each radio option aligned with the command property it updates.

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `value` | `(instance: TCommand) => TValue` | | **Required.** Accessor function returning the property value from the command instance. |
| `setValue` | `TValue` | | **Required.** Value assigned when the radio button is selected. The type is inferred from `value`. |
| `label` | `React.ReactNode` | | Content displayed next to the radio button. |
| `title` | `string` | | The label for the field (shown when `showTitles` is enabled). |
| `required` | `boolean` | | Override automatic required detection. |
| `disabled` | `boolean` | `false` | Disables the radio button. |

## Example

```tsx
<RadioButtonField
    value={(c: NotificationPreferences) => c.contactMethod}
    setValue="email"
    label="Email"
    title="Preferred Contact Method"
/>

<RadioButtonField
    value={(c: NotificationPreferences) => c.contactMethod}
    setValue="sms"
    label="SMS"
/>
```

## See Also

- [RadioGroupField](./radio-group-field.md)
- [Field Types Overview](./index.md)
- [CommandForm Overview](../index.md)
