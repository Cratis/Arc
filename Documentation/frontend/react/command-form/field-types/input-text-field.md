# InputTextField

A versatile text input field that supports multiple input types through HTML5 input elements.

## Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field. |
| `type` | `'text' \| 'email' \| 'password' \| 'color' \| 'date' \| 'datetime-local' \| 'time' \| 'url' \| 'tel' \| 'search'` | `'text'` | The HTML input type. |
| `placeholder` | `string` | | Placeholder text shown when empty. |
| `required` | `boolean` | | Override automatic required detection. |

## Examples

**Text Input:**

```tsx
<InputTextField<UserCommand> value={c => c.name} title="Full Name" placeholder="Enter your name" />
```

**Email Input:**

```tsx
<InputTextField<UserCommand> value={c => c.email} type="email" title="Email" placeholder="your@email.com" />
```

**Password Input:**

```tsx
<InputTextField<UserCommand> value={c => c.password} type="password" title="Password" />
```

**Date Input:**

```tsx
<InputTextField<UserCommand> value={c => c.birthDate} type="date" title="Birth Date" />
```

**Color Picker:**

```tsx
<InputTextField<UserCommand> value={c => c.favoriteColor} type="color" title="Favorite Color" />
```

**URL Input:**

```tsx
<InputTextField<UserCommand> value={c => c.website} type="url" title="Website" placeholder="https://example.com" />
```

## See Also

- [Field Types Overview](./index.md)
- [CommandForm Overview](../index.md)
