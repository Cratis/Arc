---
title: Layouts
description: Arrange bound fields with CSS, columns, groups, and conditional descendant components.
---

CommandForm preserves child order and binds fields through fragments, HTML elements, and custom layout components. Keep the layout where it reads naturally; you do not need to hoist every field to a direct child.

## Multi-column layout with CommandForm.Column

Direct `CommandForm.Column` children are grouped with classes such as `flex`, `flex-column`, `md:flex-row`, `gap-3`, and `flex-1`. Responsive columns require **PrimeFlex or equivalent application CSS defining these utilities**. Arc does not install that stylesheet for you. A PrimeReact theme alone is not a PrimeFlex utility stylesheet.

This complete component assumes those utility styles are already loaded and uses the generated `UpdateProfile` from the [overview](./index.md):

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

With compatible utilities, columns stack below the configured medium breakpoint and sit side by side above it; non-column children occupy full-width rows. Without those rules, do not expect the responsive arrangement or gaps. Nested columns still bind their fields, but do not assume a column hidden inside an opaque component will trigger the same **direct-child** column grouping.

## Multi-column layout with CSS Grid (alternative)

Use native CSS when you want no utility-library prerequisite. This complete component responds to available width through `auto-fit`:

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

The grid decides column widths; CommandForm still owns field binding and error display. Use explicit grid areas or spans for asymmetric layouts.

## Grouped fields

This **layout fragment** replaces the fields inside either preceding form and uses the same imports:

```tsx
<fieldset>
    <legend>Contact details</legend>
    <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
    <InputTextField<UpdateProfile> value={c => c.email} title="Email" />
</fieldset>
```

A `fieldset`/`legend` expresses a group without changing the command. Do not nest HTML forms.

## Conditional fields

Read the command in a descendant, not the component that creates its provider. This complete component expects a generated `RegisterUser` with string properties `email`, `accountType`, `companyName`, and `taxId`:

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

Selecting Business mounts the extra fields; selecting Personal unmounts them. Unmounting a field does **not** clear its command property or remove command validation rules. Define conditional backend rules and decide whether to clear now-inapplicable values. Proxy extraction does **not** preserve FluentValidation conditions such as `When`/`Unless`: a supported validator under a condition can become an unconditional client rule and reject a hidden field. Inspect the generated rules; keep conditional rules [server-only](./validation.md#backend-validation) when their condition cannot be represented on the client. A hidden field is not a security boundary.

## Layout and population

A mounted field keeps a stable registration. Equivalent inline accessors do not cause repeated population. Changes to the resolved property, `currentValue`, or `noInitialValue` update registration metadata; use `populationKey` when an `initialValue` callback captures other changing semantics. Only committed callbacks are published, including when the source and callback change together. See [Data loading](./data-loading.md#per-field-control).

## See also

- [CommandForm overview](./index.md)
- [Validation](./validation.md)
- [Customization](./customization.md)
