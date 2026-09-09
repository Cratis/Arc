---
title: InputTextField
description: Bind a string command property to a native text-based input.
---

A text input supporting the native input types listed below. Values emitted by this field are strings, including date/time and color inputs—not JavaScript `Date` objects.

## Props

| Prop | Type | Default | Description |
| ------ | ------ | --------- | ------------- |
| `value` | `(instance: TCommand) => unknown` | | **Required.** Accessor function returning the property value from the command instance. |
| `title` | `string` | | The label for the field. |
| `type` | `'text' \| 'email' \| 'password' \| 'color' \| 'date' \| 'datetime-local' \| 'time' \| 'url' \| 'tel' \| 'search'` | `'text'` | The HTML input type. |
| `placeholder` | `string` | | Placeholder text shown when empty. |
| `required` | `boolean` | | Override automatic required detection. |

The shared `className`, `style`, decoration, and population props are described in [Common props](./index.md#common-props). This built-in field does not accept `minLength`, `maxLength`, `pattern`, arbitrary input IDs, or arbitrary native attributes; use a [custom adapter](../custom-fields.md) when needed. `type="email"` does not enforce an email rule because CommandForm uses `noValidate`; see [Validation](../validation.md#required-fields).

## Examples

These are illustrative field fragments, not complete forms. Import `InputTextField` from `@cratis/arc.react/commands` and your generated `UserCommand` class, then place each fragment inside `CommandForm command={UserCommand}`. The selected properties must be strings. See the [complete form checkpoint](../index.md#start-with-a-generated-command).

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
