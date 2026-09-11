---
title: RangeField
description: Bind a numeric command property to a slider with a value display.
---

A native slider with its current value displayed beside it. Input range attributes constrain the control, not programmatically supplied command values; enforce range rules on the command.

## Props

| Prop | Type | Default | Description |
| ------ | ------ | --------- | ------------- |
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field. |
| `min` | `number` | `0` | Minimum value of the range. |
| `max` | `number` | `100` | Maximum value of the range. |
| `step` | `number` | `1` | Increment step size. |
| `required` | `boolean` | | Override automatic required detection. |

An undefined value uses a display fallback of zero. Seed a value within your chosen range rather than relying on the browser to reconcile an out-of-range command value. The form displays validation messages, but this control currently does not change its border for invalid state. See [Common props](./index.md#common-props) for styling and population.

## Example

These illustrative field fragments require `RangeField` from `@cratis/arc.react/commands` and the named generated command classes. Place each inside its matching CommandForm, with numeric `volume` or `experience` properties. They are alternatives, not one complete component.

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
