---
title: SelectField
description: Bind a string command property to a native select with explicit option key and label fields.
---

A native dropdown for choosing from option objects. Both option IDs and labels are stringified; selection emits a string, not the original object or numeric ID.

## Props

| Prop | Type | Description |
| ------ | ------ | ------------- |
| `value` | `(instance: TCommand) => unknown` | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | The label for the field. |
| `options` | `Array<{ [key: string]: unknown }>` | **Required.** Array of option objects. |
| `optionIdField` | `string` | **Required.** Name of the property to use as the option value. |
| `optionLabelField` | `string` | **Required.** Name of the property to display as the option label. |
| `placeholder` | `string` | Placeholder text shown for empty selection. |
| `required` | `boolean` | Override automatic required detection. |

All three option props are required: `options`, `optionIdField`, and `optionLabelField`. The optional placeholder has value `''`; command rules must reject it when a selection is mandatory. The native control can visually select an option while the command remains unset if you omit a placeholder/initial value, so seed intentionally. See [Common props](./index.md#common-props) and [Data loading](../data-loading.md) for asynchronous options.

## Example

These illustrative configuration/field fragments require `SelectField` from `@cratis/arc.react/commands` and a generated `UserCommand` with string `country` and `role` properties. Place each selector inside that command's form and keep its option array in component or module scope.

```tsx
const countries = [
    { id: 'us', name: 'United States' },
    { id: 'uk', name: 'United Kingdom' },
    { id: 'ca', name: 'Canada' },
    { id: 'no', name: 'Norway' }
];

<SelectField<UserCommand>
    value={c => c.country} 
    title="Country"
    options={countries}
    optionIdField="id"
    optionLabelField="name"
    placeholder="Select a country..."
/>
```

With custom data:

```tsx
const roles = [
    { value: 'admin', display: 'Administrator' },
    { value: 'user', display: 'Standard User' },
    { value: 'guest', display: 'Guest' }
];

<SelectField<UserCommand>
    value={c => c.role} 
    title="User Role"
    options={roles}
    optionIdField="value"
    optionLabelField="display"
/>
```

## See Also

- [Field Types Overview](./index.md)
- [CommandForm Overview](../index.md)
