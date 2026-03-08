# Layouts

CommandForm renders children in the order they are defined. You can create custom layouts using standard HTML and CSS, or use the built-in `CommandForm.Column` component for multi-column layouts.

## Multi-Column Layout with CommandForm.Column

CommandForm provides a built-in `CommandForm.Column` component for creating responsive multi-column layouts:

```tsx
<CommandForm command={UpdateProfile}>
    <h3>Personal Information</h3>
    
    <CommandForm.Column>
        <InputTextField<UpdateProfile> value={c => c.firstName} title="First Name" />
        <InputTextField<UpdateProfile> value={c => c.lastName} title="Last Name" />
        <InputTextField<UpdateProfile> value={c => c.phone} title="Phone" />
    </CommandForm.Column>
    
    <CommandForm.Column>
        <InputTextField<UpdateProfile> value={c => c.email} type="email" title="Email" />
        <InputTextField<UpdateProfile> value={c => c.city} title="City" />
        <InputTextField<UpdateProfile> value={c => c.country} title="Country" />
    </CommandForm.Column>
    
    <h3>Additional Details</h3>
    <TextAreaField<UpdateProfile> value={c => c.bio} title="Biography" rows={5} />
</CommandForm>
```

The `CommandForm.Column` component:
- Automatically arranges columns in a responsive layout
- Displays side-by-side on wider screens
- Stacks vertically on mobile devices
- Maintains consistent spacing between fields
- Works seamlessly with other CommandForm features (validation, errors, etc.)

**Benefits**:
- Cleaner syntax than manual CSS Grid/Flexbox
- Responsive by default
- Consistent styling with the rest of CommandForm
- Fields within columns behave identically to regular fields

## Multi-Column Layout with CSS Grid (Alternative)

For more control over layout, you can use CSS Grid directly:

```tsx
<CommandForm command={UpdateProfile}>
    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        <InputTextField<UpdateProfile> value={c => c.firstName} title="First Name" />
        <InputTextField<UpdateProfile> value={c => c.lastName} title="Last Name" />
    </div>
    
    <InputTextField<UpdateProfile> value={c => c.email} type="email" title="Email" />
    <TextAreaField<UpdateProfile> value={c => c.bio} title="Biography" rows={5} />
</CommandForm>
```

**When to use CSS Grid**:
- You need precise control over column widths
- You have complex, asymmetric layouts
- You want to span fields across multiple columns

**When to use CommandForm.Column**:
- Standard multi-column layouts
- You want responsive behavior out of the box
- You prefer cleaner, more semantic code

## Grouped Fields

```tsx
<CommandForm command={RegisterUser}>
    <fieldset style={{ border: '1px solid #e5e7eb', borderRadius: '0.5rem', padding: '1rem', marginBottom: '1rem' }}>
        <legend style={{ fontWeight: 'bold', padding: '0 0.5rem' }}>Account Information</legend>
        <InputTextField<RegisterUser> value={c => c.username} title="Username" />
        <InputTextField<RegisterUser> value={c => c.email} type="email" title="Email" />
        <InputTextField<RegisterUser> value={c => c.password} type="password" title="Password" />
    </fieldset>
    
    <fieldset style={{ border: '1px solid #e5e7eb', borderRadius: '0.5rem', padding: '1rem' }}>
        <legend style={{ fontWeight: 'bold', padding: '0 0.5rem' }}>Personal Information</legend>
        <InputTextField<RegisterUser> value={c => c.firstName} title="First Name" />
        <InputTextField<RegisterUser> value={c => c.lastName} title="Last Name" />
        <TextAreaField<RegisterUser> value={c => c.bio} title="Bio" rows={4} />
    </fieldset>
</CommandForm>
```

## Conditional Fields

Show fields based on other field values:

```tsx
function RegistrationForm() {
    const command = useCommandInstance(RegisterUser);
    
    return (
        <CommandForm command={RegisterUser}>
            <InputTextField<RegisterUser> value={c => c.email} type="email" title="Email" />
            
            <SelectField<RegisterUser>
                value={c => c.accountType}
                title="Account Type"
                options={[
                    { id: 'personal', name: 'Personal' },
                    { id: 'business', name: 'Business' }
                ]}
                optionIdField="id"
                optionLabelField="name"
            />
            
            {command.accountType === 'business' && (
                <>
                    <InputTextField<RegisterUser> value={c => c.companyName} title="Company Name" />
                    <InputTextField<RegisterUser> value={c => c.taxId} title="Tax ID" />
                </>
            )}
        </CommandForm>
    );
}
```

## See Also

- [CommandForm Overview](./index.md)
- [Validation](./validation.md)
- [Customization](./customization.md)
