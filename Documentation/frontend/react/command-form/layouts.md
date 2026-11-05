---
title: Arrange React CommandForm fields
description: Arrange React CommandForm fields with responsive columns or CSS Grid, group related inputs, and show conditional fields without changing command rules.
---

Cratis Arc's React CommandForm lets you arrange bound fields inside HTML elements or custom layout components while preserving child order. Use `CommandForm.Column` when your application supplies its utility CSS, or CSS Grid when you want to control the layout yourself. Fields do not all need to be direct children of the form.

## Multi-column layout with CommandForm.Column

Load PrimeFlex or equivalent application CSS before using responsive columns. CommandForm groups direct `CommandForm.Column` children using utility classes such as `flex`, `flex-column`, `md:flex-row`, `gap-3`, and `flex-1`. Arc does not install the stylesheet, and a PrimeReact theme alone does not supply these utilities.

With those styles loaded, use the generated `UpdateProfile` from the [overview](./index.md) in this complete component:

```tsx
import { CommandForm, InputTextField } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

export function ProfileColumns() {
    return (
        <CommandForm command={UpdateProfile} initialValues={{ name: '', email: '' }}>
            <h2>Profile</h2>
            <CommandForm.Column>
                <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
            </CommandForm.Column>
            <CommandForm.Column>
                <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
            </CommandForm.Column>
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

The utility CSS stacks the name and email columns below its medium breakpoint and places them side by side above it. The heading and Save button occupy full-width rows. Without the matching CSS, the responsive arrangement and gaps will be missing.

Keep `CommandForm.Column` at the form's top level for this grouping. A column inside a custom component still binds its fields, but does not trigger the direct-child layout.

## Multi-column layout with CSS Grid (alternative)

If your application does not use those utilities, put a CSS Grid container inside the form. This complete component uses `auto-fit` to fit as many columns as the available width allows:

```tsx
import { CommandForm, InputTextField } from '@cratis/arc.react/commands';
import { UpdateProfile } from './commands/UpdateProfile';

export function ProfileGrid() {
    return (
        <CommandForm command={UpdateProfile} initialValues={{ name: '', email: '' }}>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 16rem), 1fr))', gap: '1rem' }}>
                <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
                <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
            </div>
            <button type="submit">Save</button>
        </CommandForm>
    );
}
```

The grid controls column widths while CommandForm binds each input to the command and displays its errors. Use explicit grid areas or spans when some fields need more space than others.

## Grouped fields

Use a `fieldset` and `legend` to give related inputs a shared label. This layout fragment replaces the fields inside either preceding form and uses the same imports:

```tsx
<fieldset>
    <legend>Contact details</legend>
    <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
    <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
</fieldset>
```

Grouping the inputs does not change the command's properties or rules. Keep the fieldset inside the existing CommandForm; do not nest HTML forms.

## Conditional fields

To show fields based on another input, read the command with `useCommandInstance` in a child of CommandForm. The hook needs the form's context, so it cannot read that context from the component that creates the form.

This complete component expects a generated `RegisterUser` with string properties `email`, `accountType`, `companyName`, and `taxId`:

```tsx
import { CommandForm, InputTextField, SelectField, useCommandInstance } from '@cratis/arc.react/commands';
import { RegisterUser } from './commands/RegisterUser';

function BusinessFields() {
    const command = useCommandInstance<RegisterUser>();
    if (command.accountType !== 'business') return null;
    return (
        <fieldset>
            <legend>Business details</legend>
            <InputTextField<RegisterUser> value={c => c.companyName} title="Company name" />
            <InputTextField<RegisterUser> value={c => c.taxId} title="Tax ID" />
        </fieldset>
    );
}

export function RegistrationForm() {
    return (
        <CommandForm command={RegisterUser} initialValues={{ email: '', accountType: 'personal', companyName: '', taxId: '' }}>
            <InputTextField<RegisterUser> value={c => c.email} title="Email" type="email" />
            <SelectField<RegisterUser>
                value={c => c.accountType} title="Account type"
                options={[{ id: 'personal', name: 'Personal' }, { id: 'business', name: 'Business' }]}
                optionIdField="id" optionLabelField="name"
            />
            <BusinessFields />
            <button type="submit">Register</button>
        </CommandForm>
    );
}
```

Selecting Business mounts the extra fields; selecting Personal unmounts them. Unmounting does not clear `companyName` or `taxId`, and command validation still runs. Decide whether to clear values that no longer apply, and make the backend rules depend on the account type where appropriate.

For C# backends, proxy extraction omits rules guarded by FluentValidation `When`/`Unless`, including asynchronous, block, and chained conditions. These rules run [only on the server](./validation.mdx#backend-validation). Nullable concept members without a direct `[Required]` annotation omit inferred presence rules (`notEmpty` and `notNull`). Other inferred rules, explicit rules, and annotations still apply according to the [validation extraction precedence](../../../backend/csharp/proxy-generation/validation.md#prerequisite-and-extraction-flow). Inspect the generated rules before relying on client feedback for hidden fields.

:::caution[A hidden field is not a security boundary]
Hiding or unmounting a field does not remove its command value or replace server-side validation and authorization.
:::

## Layout and population

Rerendering a layout does not by itself repopulate its mounted fields. Each field keeps a stable registration, and equivalent inline accessors do not trigger repeated population. Changing the resolved property, `currentValue`, or `noInitialValue` updates that registration.

Use `populationKey` when an `initialValue` callback depends on other changing values. Only callbacks from committed renders are used, including when the population source and callback change together. Continue with [per-field data loading](./data-loading.md#per-field-control) to decide which loaded values become the baseline for change tracking.

## See also

- [CommandForm overview](./index.md)
- [Validation: separate input attributes from command rules](./validation.mdx)
- [Customization: change field containers and feedback](./customization.md)
