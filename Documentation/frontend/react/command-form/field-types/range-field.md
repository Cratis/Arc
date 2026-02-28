# RangeField

A slider input for selecting a numeric value within a range. The current value is displayed next to the slider for immediate feedback.

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field. |
| `min` | `number` | `0` | Minimum value of the range. |
| `max` | `number` | `100` | Maximum value of the range. |
| `step` | `number` | `1` | Increment step size. |
| `required` | `boolean` | | Override automatic required detection. |

## Example

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

## See Also

- [Field Types Overview](./index.md)
- [CommandForm Overview](../index.md)
