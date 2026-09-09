---
title: NumberField
description: Bind a numeric command property to a native number input.
---

A numeric input with native min/max attributes and step increments. These attributes guide input; command range rules enforce validity because the form uses `noValidate`.

## Props

| Prop | Type | Default | Description |
| ------ | ------ | --------- | ------------- |
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field. |
| `placeholder` | `string` | | Placeholder text shown when empty. |
| `min` | `number` | | Native input's minimum attribute; not a command rule. |
| `max` | `number` | | Native input's maximum attribute; not a command rule. |
| `step` | `number` | | Increment/decrement step size. |
| `required` | `boolean` | | Override automatic required detection. |

The field displays `0` for an undefined value; editing the native control to empty or unparsable text also writes `0`. It cannot preserve a distinct empty numeric value. Use a custom adapter if your command needs nullable/empty semantics. See [Common props](./index.md#common-props) for styling and population.

## Example

These illustrative field fragments require imports for `NumberField` from `@cratis/arc.react/commands` and the named generated command classes. Place each fragment in its matching form; `age` and `price` are numeric properties. They are alternatives, not one complete component.

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
