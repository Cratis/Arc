# Customization

CommandForm provides several customization options to adapt the form rendering to your specific needs while maintaining integration with the command system.

## Custom Titles

By default, CommandForm displays field titles above each field when they are defined. You can disable automatic title rendering and provide your own custom title rendering.

### Disabling Automatic Titles

Set the `showTitles` prop to `false`:

```tsx
<CommandForm<MyCommand> command={MyCommand} showTitles={false}>
    <InputTextField<MyCommand> value={c => c.name} title="Full Name" />
    <InputTextField<MyCommand> value={c => c.email} type="email" title="Email Address" />
</CommandForm>
```

### Custom Title Rendering

Combine `showTitles={false}` with custom heading or label elements:

```tsx
<CommandForm<MyCommand> command={MyCommand} showTitles={false}>
    <h3 style={{ fontSize: '1.25rem', fontWeight: 'bold', marginBottom: '0.5rem' }}>
        Full Name
    </h3>
    <InputTextField<MyCommand> value={c => c.name} title="Full Name" />
    
    <h3 style={{ fontSize: '1.25rem', fontWeight: 'bold', marginBottom: '0.5rem', marginTop: '1rem' }}>
        Email Address
    </h3>
    <InputTextField<MyCommand> value={c => c.email} type="email" title="Email Address" />
</CommandForm>
```

### Use Case

Custom titles are useful when you want:

- Different visual styling than the default
- To include additional information (icons, tooltips, required indicators)
- More control over layout and spacing
- To use a custom design system

## Custom Error Rendering

CommandForm automatically displays error messages below fields when validation fails. You can take control of error rendering to match your design requirements.

### Disabling Automatic Error Messages

Set the `showErrors` prop to `false`:

```tsx
<CommandForm<MyCommand> command={MyCommand} showErrors={false}>
    <InputTextField<MyCommand> value={c => c.email} type="email" title="Email" required />
    <InputTextField<MyCommand> value={c => c.password} type="password" title="Password" required />
</CommandForm>
```

### Custom Error Display

Use the `useCommandFormContext` hook to access validation state and render errors yourself:

```tsx
import { useCommandFormContext } from '@cratis/applications-react/commands';

function MyForm() {
    const { instance } = useCommandFormContext();
    
    return (
        <CommandForm<MyCommand> command={MyCommand} showErrors={false}>
            <InputTextField<MyCommand> value={c => c.email} type="email" title="Email" required />
            {instance.hasErrors('email') && (
                <div className="error-message" style={{
                    backgroundColor: '#fee',
                    color: '#c00',
                    padding: '0.5rem',
                    borderRadius: '0.25rem',
                    marginTop: '0.25rem',
                    fontSize: '0.875rem'
                }}>
                    ⚠️ {instance.getErrorsFor('email').join(', ')}
                </div>
            )}
            
            <InputTextField<MyCommand> value={c => c.password} type="password" title="Password" required />
            {instance.hasErrors('password') && (
                <div className="error-message">
                    ⚠️ {instance.getErrorsFor('password').join(', ')}
                </div>
            )}
        </CommandForm>
    );
}
```

### Error Summary

Display all errors at once at the top or bottom of the form:

```tsx
function MyForm() {
    const { instance } = useCommandFormContext();
    
    return (
        <CommandForm<MyCommand> command={MyCommand} showErrors={false}>
            {instance.hasErrors() && (
                <div className="error-summary" style={{
                    backgroundColor: '#fee',
                    border: '2px solid #c00',
                    padding: '1rem',
                    borderRadius: '0.5rem',
                    marginBottom: '1rem'
                }}>
                    <h4 style={{ margin: '0 0 0.5rem 0', color: '#c00' }}>
                        Please correct the following errors:
                    </h4>
                    <ul style={{ margin: 0, paddingLeft: '1.5rem' }}>
                        {Object.entries(instance.errors).map(([field, errors]) => (
                            <li key={field} style={{ color: '#c00' }}>
                                <strong>{field}:</strong> {errors.join(', ')}
                            </li>
                        ))}
                    </ul>
                </div>
            )}
            
            <InputTextField<MyCommand> value={c => c.email} type="email" title="Email" required />
            <InputTextField<MyCommand> value={c => c.password} type="password" title="Password" required />
        </CommandForm>
    );
}
```

### Use Case

Custom error rendering is useful when you want:

- Error summaries at the form level
- Integration with a specific UI framework or design system
- Inline errors with custom icons or animations
- Accessible error messages with specific ARIA attributes

## Custom Field Container

For complete control over how each field is rendered, including its title, error message, and container, use the `fieldContainerComponent` prop.

### Using Custom Field Container

Define a custom container component that receives field metadata:

```tsx
import { FieldContainerProps } from '@cratis/applications-react/commands';

const CustomFieldContainer = ({ title, errorMessage, children }: FieldContainerProps) => (
    <div className="custom-field-container" style={{
        backgroundColor: errorMessage ? '#fff5f5' : '#f9fafb',
        border: `2px solid ${errorMessage ? '#f56565' : '#e5e7eb'}`,
        borderRadius: '0.5rem',
        padding: '1rem',
        marginBottom: '1rem'
    }}>
        {title && (
            <label style={{
                display: 'block',
                fontWeight: 600,
                fontSize: '0.875rem',
                marginBottom: '0.5rem',
                color: errorMessage ? '#c53030' : '#374151'
            }}>
                {title}
                {errorMessage && ' *'}
            </label>
        )}
        
        {children}
        
        {errorMessage && (
            <div style={{
                color: '#c53030',
                fontSize: '0.875rem',
                marginTop: '0.5rem',
                fontWeight: 500
            }}>
                🚫 {errorMessage}
            </div>
        )}
    </div>
);
```

Then use it with CommandForm:

```tsx
<CommandForm<MyCommand>
    command={MyCommand} 
    fieldContainerComponent={CustomFieldContainer}
>
    <InputTextField<MyCommand> value={c => c.name} title="Full Name" required />
    <InputTextField<MyCommand> value={c => c.email} type="email" title="Email Address" required />
    <TextAreaField<MyCommand> value={c => c.bio} title="Biography" rows={5} />
</CommandForm>
```

### FieldContainerProps Interface

The custom field container component receives these props:

| Prop | Type | Description |
|------|------|-------------|
| `title` | `string \| undefined` | The field title (from the field's `title` prop). |
| `errorMessage` | `string \| undefined` | The error message for the field, if any. |
| `children` | `React.ReactNode` | The actual field component. |

### Advanced Field Container

Add additional features like help text, icons, or required indicators:

```tsx
interface ExtendedFieldContainerProps extends FieldContainerProps {
    helpText?: string;
    icon?: React.ReactNode;
}

const AdvancedFieldContainer = ({ 
    title, 
    errorMessage, 
    children, 
    helpText, 
    icon 
}: ExtendedFieldContainerProps) => (
    <div className="field-wrapper" style={{
        marginBottom: '1.5rem'
    }}>
        {title && (
            <div style={{ display: 'flex', alignItems: 'center', marginBottom: '0.5rem' }}>
                {icon && <span style={{ marginRight: '0.5rem' }}>{icon}</span>}
                <label style={{ fontWeight: 600 }}>
                    {title}
                </label>
            </div>
        )}
        
        {children}
        
        {helpText && !errorMessage && (
            <div style={{
                fontSize: '0.875rem',
                color: '#6b7280',
                marginTop: '0.25rem'
            }}>
                ℹ️ {helpText}
            </div>
        )}
        
        {errorMessage && (
            <div style={{
                fontSize: '0.875rem',
                color: '#dc2626',
                marginTop: '0.25rem',
                fontWeight: 500
            }}>
                ❌ {errorMessage}
            </div>
        )}
    </div>
);
```

### Use Case

Custom field containers are useful when you want:

- Consistent field styling across your application
- Integration with an existing component library
- Complex layouts with icons, help text, and labels
- Conditional styling based on validation state
- Accessibility enhancements (ARIA attributes, screen reader support)

## Combining Customizations

You can combine multiple customization options:

```tsx
<CommandForm<MyCommand>
    command={MyCommand} 
    showTitles={false}           // Disable auto titles
    showErrors={false}           // Disable auto errors
    fieldContainerComponent={CustomFieldContainer}  // Use custom container
>
    <h2>Account Information</h2>
    <InputTextField<MyCommand> value={c => c.username} title="Username" required />
    <InputTextField<MyCommand> value={c => c.email} type="email" title="Email" required />
    
    <h2>Profile</h2>
    <TextAreaField<MyCommand> value={c => c.bio} title="Bio" />
</CommandForm>
```

## Best Practices

1. **Consistency**: Use the same customization approach across all forms in your application
2. **Accessibility**: Ensure custom rendering maintains proper accessibility (labels, ARIA attributes)
3. **Validation**: Keep error messages visible and clear regardless of customization
4. **Performance**: Avoid creating new component instances on each render (define outside or use `useMemo`)
5. **Testing**: Test custom components with various validation states and error messages

## See Also

- [CommandForm Overview](./index.md)
- [Built-in Field Types](./field-types.md)
- [Validation](./validation.md)
- [Advanced Usage](./advanced.md)
