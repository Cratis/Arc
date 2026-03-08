# CheckboxField

A checkbox input for boolean (true/false) values.

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field (shown when `showTitles` is enabled). |
| `label` | `string` | | Label text displayed next to the checkbox. |
| `required` | `boolean` | | Override automatic required detection. |

## Example

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

## See Also

- [Field Types Overview](./index.md)
- [CommandForm Overview](../index.md)
