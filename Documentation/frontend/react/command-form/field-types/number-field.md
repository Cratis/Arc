# NumberField

A specialized field for numeric input with support for min/max constraints and step increments.

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field. |
| `placeholder` | `string` | | Placeholder text shown when empty. |
| `min` | `number` | | Minimum allowed value. |
| `max` | `number` | | Maximum allowed value. |
| `step` | `number` | | Increment/decrement step size. |
| `required` | `boolean` | | Override automatic required detection. |

## Example

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

## See Also

- [Field Types Overview](./index.md)
- [CommandForm Overview](../index.md)
