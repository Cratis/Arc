---
title: Field types
description: Choose a built-in CommandForm control and distinguish binding props from command validation rules.
---

Built-in fields synchronize values with the surrounding form and display its validation messages. Import them from `@cratis/arc.react/commands` and render them inside `CommandForm`.

## Type safety

Use an explicit command type for fields created by `asCommandFormField`. This **field fragment** assumes imports for `InputTextField` and your generated `UpdateProfile` class with a string `name` property:

```tsx
<InputTextField<UpdateProfile> value={c => c.name} title="Name" />
```

Omitting both the generic and an accessor parameter annotation leaves `c` as `unknown`. The generic checks property names; most field accessors return `unknown`, so it does not guarantee the selected property's value type matches the control. Use simple property accessors, not computed or nested expressions.

Radio fields infer the selected value type from the accessor itself. This **field fragment** expects a generated `UserSettingsCommand` whose `role` accepts `'reader'` or `'admin'`:

```tsx
<RadioGroupField
    value={(c: UserSettingsCommand) => c.role}
    title="Role"
    options={[
        { value: 'reader', label: 'Reader' },
        { value: 'admin', label: 'Administrator' }
    ]}
/>
```

## Common props

| Prop | Type | Contract |
| --- | --- | --- |
| `value` | Property accessor | Required; selects the command property. |
| `title` | `string` | Field title shown by the default title renderer. |
| `required` | `boolean` | Overrides the control's inferred required flag, not the command's validation rules. |
| `icon` | `React.ReactElement` | Optional icon decoration. |
| `description` | `string` | Optional description/tooltip decoration. |
| `className`, `style` | `string`, `React.CSSProperties` | Control or wrapper styling, depending on field type. |
| `currentValue` | `unknown` (typed for radios) | Explicit field seed; undefined supplies nothing. Binding supplies the live value internally. |
| `initialValue`, `noInitialValue`, `populationKey` | Population metadata | See [Data loading](../data-loading.md#per-field-control). |

Fields default to required when their generated `PropertyDescriptor.isOptional` is false (and also when no descriptor is resolved). This sets an input attribute. CommandForm has `noValidate`, so it does **not** enforce browser required/type/range constraints on submit. Payload presence validation checks null/undefined, not empty strings or false booleans. Use explicit [command validation rules](../validation.md#required-fields).

`onValueChange`, `onBlur`, descriptor, and field-name props participate in internal binding. Custom adapters must forward the injected blur handler. Do not attach a public `onBlur` and assume it overrides or composes with the form's injected validation callback; use the form's interaction callbacks or an adapter that preserves forwarding.

## Field state

- Edits update the command through the form setter and advance its revision.
- Silent validation updates form validity; `validateOn` controls when displayed errors are refreshed.
- Most controls add an invalid border; `RangeField` currently does not vary its border with invalid state, though the form still displays its message.
- Display defaults do not seed payload values. Define intentional initial values on the form.

## Available fields

| Field | Value and control |
| --- | --- |
| [InputTextField](./input-text-field.md) | Strings; text, email, password, date/time text, color, URL, telephone, and search. |
| [NumberField](./number-field.md) | Numbers; empty/invalid native text input converts to zero. |
| [TextAreaField](./text-area-field.md) | Multi-line strings. |
| [CheckboxField](./checkbox-field.md) | Boolean checked state. |
| [RadioButtonField](./radio-button-field.md) | One typed option for a property. |
| [RadioGroupField](./radio-group-field.md) | A list of typed options for a property. |
| [RangeField](./range-field.md) | Numeric slider with value display. |
| [SelectField](./select-field.md) | String values from a native select; option IDs are stringified. |

## Styling

Most fields use full-width inline layout, `0.75rem` padding, and `0.375rem` rounded borders. Theme colors use CSS variables with some fallbacks; application CSS must define any variables or utility classes you rely on. There is no automatic UI-library installation or comprehensive accessible label/error association. For explicit control IDs, ARIA props, or unsupported native attributes, use [custom fields](../custom-fields.md).

## See also

- [CommandForm overview](../index.md)
- [Validation](../validation.md)
- [Creating custom fields](../custom-fields.md)
