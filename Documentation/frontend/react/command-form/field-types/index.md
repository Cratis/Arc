---
title: CommandForm field types and binding
description: Choose a React CommandForm field for your command property, bind it with a typed accessor, and distinguish display defaults from payload values.
---

Cratis Arc's React `CommandForm` fields bind user input to command properties and show validation feedback from the form. Choose a control that emits the property's value type, import it from `@cratis/arc.react/commands`, and render it inside `CommandForm`.

## Available fields

Start with the value your command expects. Use a text input for a short string, a text area for longer content, and a checkbox for a boolean. For a fixed set of choices, use a select for string IDs or radio fields when you need to retain typed option values.

| Field | Choose it for |
| --- | --- |
| [InputTextField](./input-text-field.md) | Strings; text, email, password, date/time text, color, URL, telephone, and search. Date/time inputs still emit strings. |
| [NumberField](./number-field.md) | Numeric entry; empty or invalid native input converts to zero. |
| [TextAreaField](./text-area-field.md) | Multi-line strings. |
| [CheckboxField](./checkbox-field.md) | Boolean checked state. |
| [RadioButtonField](./radio-button-field.md) | One typed option, placed individually alongside other radio buttons bound to the same property. |
| [RadioGroupField](./radio-group-field.md) | A list of typed options for one property. |
| [RangeField](./range-field.md) | A numeric slider with a value display. |
| [SelectField](./select-field.md) | String values from a native select; option IDs are stringified. |

If an empty numeric field must mean “no value” rather than zero, use a [custom field adapter](../custom-fields.md) with an explicit conversion. A built-in `NumberField` cannot preserve that distinction.

## Type safety

The `value` prop is a function that identifies a command property, not the input's current text. For fields created by `asCommandFormField`, supply the command type so TypeScript can check the property name. This field fragment assumes imports for `InputTextField` and your generated `UpdateProfile` class with a string `name` property:

```tsx
<InputTextField<UpdateProfile> value={c => c.name} title="Name" />
```

Here, the form reads and updates `name` on its command instance. Omitting both the generic and an accessor parameter annotation leaves `c` as `unknown`. Most field accessors return `unknown`, so checking the property name does not guarantee its value type matches the control. Match the control to the property using the table above.

Use a simple accessor such as `c => c.name`. The form infers a top-level property name from the accessor; it does not traverse nested objects. For a computed accessor, supply `fieldName` explicitly to identify the top-level command property.

Radio fields infer the selected value type from the accessor itself, so their option values are checked against that type. This field fragment expects a generated `UserSettingsCommand` whose `role` accepts `'reader'` or `'admin'`:

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
| `fieldName` | `string` | Optional explicit property name; takes precedence over accessor inference. |
| `id` | `string` | Optional control or group-container ID; otherwise the form generates one. |
| `title` | `string` | Optional field title; the form renders it when `showTitles` is enabled. |
| `required` | `boolean` | Overrides the control's inferred required flag, not the command's validation rules. |
| `icon` | `React.ReactElement` | Optional icon decoration. |
| `description` | `string` | Optional description/tooltip decoration. |
| `className`, `style` | `string`, `React.CSSProperties` | Control or wrapper styling, depending on field type. |
| `currentValue` | `unknown` (typed for radios) | Explicit field seed; undefined supplies nothing. Binding supplies the live value internally. |
| `initialValue`, `noInitialValue`, `populationKey` | Population metadata | See [Data loading](../data-loading.md#per-field-control). |

The control's `required` flag comes from the generated `PropertyDescriptor.isOptional`: a non-optional property makes the control required. With no resolved descriptor, the flag defaults to true. Setting `required={false}` only changes that control flag; it does not make the command property optional.

CommandForm uses `noValidate`, so browser required, type, and range constraints do not enforce validation on submit. The command's payload presence check rejects null or undefined for non-optional payload properties, but not empty strings or false booleans. To require nonempty text or an explicitly checked box, define [command validation rules](../validation.mdx#required-fields).

The form supplies the live value, property descriptor, and change and blur handlers internally. A public `onBlur` does not override or compose with the injected validation handler. Use the form's [interaction callbacks](../index.md#props-reference) for application reactions. If you build a custom adapter, forward the injected `onBlur` to preserve blur validation.

## Field state

An edit updates the command through the form setter and advances the form's revision so bound fields render the new value. Silent validation updates form validity without necessarily showing messages. The form's `validateOn` setting controls when displayed errors are refreshed; see [validation timing](../validation.mdx).

A control's display fallback does not initialize the command. For example, an uninitialized checkbox can look unchecked while the command property remains undefined. Fields created by `asCommandFormField` use their fallback only for undefined, not null. Define intentional payload values with the form's `initialValues`, or use a field's `currentValue` as an explicit seed. An undefined seed supplies nothing. See [data loading](../data-loading.md) for seed precedence and reactive updates.

Most controls add an invalid border when they have a displayed error. `RangeField` does not change its border for invalid state; the form can still show its validation message.

## Styling

Built-in fields accept `className` and `style`, applied to the control or its wrapper depending on the field. Most use full-width inline layout, `0.75rem` padding, and `0.375rem` rounded borders. Theme colors use CSS variables with some fallbacks; define the variables and utility classes your application relies on. These controls do not require a UI library.

For single-input fields, the form associates `title` with the control's ID. For `RadioGroupField`, it uses the title as the group's accessible name, and each option has its own label. If you hide titles, provide an accessible name yourself. An explicit `id` is supported, but additional ARIA props, error-message associations, or unsupported native attributes may need a [custom field adapter](../custom-fields.md).

## See also

To place these fields in a complete form, continue with the [CommandForm overview](../index.md). For rules and message timing, use the [validation guide](../validation.mdx). If the built-in controls cannot represent your value or input behavior, follow [Creating custom fields](../custom-fields.md).
