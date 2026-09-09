---
title: RadioButtonField
description: Assign a typed option value to a command property with one radio button.
---

A single radio input for assigning a specific value to a command property.

`RadioButtonField` infers the allowed `setValue` type from the bound accessor. That keeps each radio option aligned with the command property it updates.

## Props

| Prop | Type | Default | Description |
| ------ | ------ | --------- | ------------- |
| `value` | `(instance: TCommand) => TValue` | | **Required.** Accessor function returning the property value from the command instance. |
| `setValue` | `TValue` | | **Required.** Value assigned when the radio button is selected. The type is inferred from `value`. |
| `label` | `React.ReactNode` | | Content displayed next to the radio button. |
| `title` | `string` | | The label for the field (shown when `showTitles` is enabled). |
| `required` | `boolean` | | Override automatic required detection. |
| `disabled` | `boolean` | `false` | Disables the radio button. |

Selection assigns `setValue` directly, and checked state uses `Object.is`. Prefer primitive option values; freshly constructed objects do not compare structurally. Radios sharing a bound property use its field name for grouping. See [Common props](./index.md#common-props); `required` is a control flag, not a replacement for command validation.

## Example

These illustrative field fragments require `RadioButtonField` from `@cratis/arc.react/commands` and a generated `NotificationPreferences` whose `contactMethod` accepts `'email'` and `'sms'`. Place both inside its CommandForm. Annotating the accessor parameter enables option type inference; do not pass the command type as this component's accessor generic.

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
