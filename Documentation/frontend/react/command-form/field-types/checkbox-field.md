---
title: CheckboxField
description: Bind a boolean command property to a checkbox.
---

A checkbox input for boolean values. An undefined value displays as unchecked; clicking writes the control's boolean checked state.

## Props

| Prop | Type | Default | Description |
| ------ | ------ | --------- | ------------- |
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field (shown when `showTitles` is enabled). |
| `label` | `string` | | Label text displayed next to the checkbox. |
| `required` | `boolean` | | Override automatic required detection. |

A `required` flag does not require this command property to be true: `false` is a present boolean and CommandForm uses `noValidate`. A consent checkbox needs an explicit backend rule requiring true. The built-in adjacent label is not linked with an input ID; use a [custom field](../custom-fields.md) for explicit accessible label association. See [Common props](./index.md#common-props) for styling and population.

## Example

These illustrative field fragments require imports for `CheckboxField` from `@cratis/arc.react/commands` and your generated `UserCommand` with boolean `agreeToTerms` and `newsletter` properties. Place them inside that command's form, initialized with intentional boolean defaults.

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
