---
title: TextAreaField
description: Bind a string command property to a multi-line text area.
---

A multi-line string input. An undefined value displays as an empty string without seeding the command.

## Props

| Prop | Type | Default | Description |
| ------ | ------ | --------- | ------------- |
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field. |
| `placeholder` | `string` | | Placeholder text shown when empty. |
| `rows` | `number` | `5` | Number of visible text rows. |
| `cols` | `number` | | Number of visible text columns. |
| `required` | `boolean` | | Override automatic required detection. |

See [Common props](./index.md#common-props) for styling, decoration, and population. This field does not accept every native textarea attribute (for example `maxLength`); use a [custom adapter](../custom-fields.md) for additional attributes. Required and length rules belong in [command validation](../validation.md#required-fields).

## Example

These illustrative field fragments require `TextAreaField` from `@cratis/arc.react/commands` and the named generated command classes. Place each inside its matching CommandForm. `bio` and `notes` are string properties; these are alternatives, not a complete component.

```tsx
<TextAreaField<UserCommand>
    value={c => c.bio} 
    title="Biography" 
    placeholder="Tell us about yourself..."
    rows={8}
/>

<TextAreaField<NoteCommand>
    value={c => c.notes} 
    title="Additional Notes" 
    rows={3}
/>
```

## See Also

- [Field Types Overview](./index.md)
- [CommandForm Overview](../index.md)
