# TextAreaField

A multi-line text input field for longer content.

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field. |
| `placeholder` | `string` | | Placeholder text shown when empty. |
| `rows` | `number` | `5` | Number of visible text rows. |
| `cols` | `number` | | Number of visible text columns. |
| `required` | `boolean` | | Override automatic required detection. |

## Example

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
