# SelectField

A dropdown select field for choosing from a list of options.

## Props

| Prop | Type | Description |
|------|------|-------------|
| `value` | `(instance: TCommand) => unknown` | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | The label for the field. |
| `options` | `Array<{ [key: string]: unknown }>` | **Required.** Array of option objects. |
| `optionIdField` | `string` | **Required.** Name of the property to use as the option value. |
| `optionLabelField` | `string` | **Required.** Name of the property to display as the option label. |
| `placeholder` | `string` | Placeholder text shown for empty selection. |
| `required` | `boolean` | Override automatic required detection. |

## Example

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
