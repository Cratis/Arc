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
    const { getFieldError } = useCommandFormContext();
    const emailError = getFieldError('email');
    const passwordError = getFieldError('password');
    
    return (
        <CommandForm<MyCommand> command={MyCommand} showErrors={false}>
            <InputTextField<MyCommand> value={c => c.email} type="email" title="Email" required />
            {emailError && (
                <div className="error-message" style={{
                    backgroundColor: '#fee',
                    color: '#c00',
                    padding: '0.5rem',
                    borderRadius: '0.25rem',
                    marginTop: '0.25rem',
                    fontSize: '0.875rem'
                }}>
                    ⚠️ {emailError}
                </div>
            )}
            
            <InputTextField<MyCommand> value={c => c.password} type="password" title="Password" required />
            {passwordError && (
                <div className="error-message">
                    ⚠️ {passwordError}
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
    const { commandResult } = useCommandFormContext();
    const hasErrors = commandResult?.validationResults && commandResult.validationResults.length > 0;
    
    return (
        <CommandForm<MyCommand> command={MyCommand} showErrors={false}>
            {hasErrors && (
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

## Custom Field Decorator

The `fieldDecoratorComponent` allows you to customize how fields with icons and descriptions are wrapped and decorated. This is particularly useful for integrating with UI frameworks like PrimeReact, Material-UI, or custom design systems.

### Using Custom Field Decorator

Define a custom decorator component:

```tsx
import { FieldDecoratorProps } from '@cratis/applications-react/commands';

const PrimeReactFieldDecorator = ({ icon, description, children }: FieldDecoratorProps) => {
    if (!icon && !description) {
        return <>{children}</>;
    }

    return (
        <div className="p-inputgroup">
            {icon && (
                <span className="p-inputgroup-addon">
                    {icon}
                </span>
            )}
            <div className="p-field" data-pr-tooltip={description}>
                {children}
            </div>
        </div>
    );
};
```

Use it with CommandForm:

```tsx
<CommandForm<MyCommand>
    command={MyCommand}
    fieldDecoratorComponent={PrimeReactFieldDecorator}
>
    <InputTextField<MyCommand> 
        value={c => c.email} 
        title="Email"
        icon={<i className="pi pi-envelope" />}
        description="We'll never share your email"
    />
</CommandForm>
```

### FieldDecoratorProps Interface

| Prop | Type | Description |
|------|------|-------------|
| `icon` | `React.ReactElement \| undefined` | Icon element to display alongside the field. |
| `description` | `string \| undefined` | Description text to show as tooltip or help text. |
| `children` | `React.ReactNode` | The field component to decorate. |

## Custom Error Display

The `errorDisplayComponent` allows complete control over how validation errors are rendered for each field.

### Using Custom Error Display

Define a custom error display component:

```tsx
import { ErrorDisplayProps } from '@cratis/applications-react/commands';

const CustomErrorDisplay = ({ errors, fieldName }: ErrorDisplayProps) => (
    <div className="custom-errors" role="alert" aria-live="polite">
        {errors.map((error, idx) => (
            <div key={idx} className="error-item" style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
                padding: '0.5rem',
                backgroundColor: '#fef2f2',
                border: '1px solid #fecaca',
                borderRadius: '0.25rem',
                marginTop: '0.25rem',
                fontSize: '0.875rem',
                color: '#dc2626'
            }}>
                <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                    <path fillRule="evenodd" d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zM7 5a1 1 0 1 1 2 0v4a1 1 0 1 1-2 0V5zm1 7a1 1 0 1 0 0-2 1 1 0 0 0 0 2z" />
                </svg>
                <span>{error}</span>
            </div>
        ))}
    </div>
);
```

Use it with CommandForm:

```tsx
<CommandForm<MyCommand>
    command={MyCommand}
    errorDisplayComponent={CustomErrorDisplay}
>
    <InputTextField<MyCommand> value={c => c.email} title="Email" required />
    <InputTextField<MyCommand> value={c => c.password} title="Password" required />
</CommandForm>
```

### ErrorDisplayProps Interface

| Prop | Type | Description |
|------|------|-------------|
| `errors` | `string[]` | Array of error messages for the field. |
| `fieldName` | `string \| undefined` | Name of the field with errors (useful for custom error handling). |

## Custom Tooltip Component

The `tooltipComponent` allows you to integrate custom tooltip libraries or components for field descriptions.

### Using Custom Tooltip

Define a custom tooltip wrapper:

```tsx
import { TooltipWrapperProps } from '@cratis/applications-react/commands';
import { Tooltip } from 'primereact/tooltip';

const PrimeReactTooltip = ({ description, children }: TooltipWrapperProps) => {
    const tooltipId = `tooltip-${Math.random().toString(36).substr(2, 9)}`;
    
    return (
        <>
            <div data-pr-tooltip={description} data-pr-position="top" id={tooltipId}>
                {children}
            </div>
            <Tooltip target={`#${tooltipId}`} />
        </>
    );
};
```

Use it with CommandForm:

```tsx
<CommandForm<MyCommand>
    command={MyCommand}
    tooltipComponent={PrimeReactTooltip}
>
    <InputTextField<MyCommand> 
        value={c => c.username} 
        title="Username"
        description="Choose a unique username. Letters, numbers, and underscores only."
    />
</CommandForm>
```

### TooltipWrapperProps Interface

| Prop | Type | Description |
|------|------|-------------|
| `description` | `string` | The tooltip text to display. |
| `children` | `React.ReactNode` | The content to attach the tooltip to. |

## Custom CSS Classes

For lightweight customization without creating custom components, use the CSS class customization options.

### Using Custom CSS Classes

```tsx
<CommandForm<MyCommand>
    command={MyCommand}
    errorClassName="my-error-message"
    iconAddonClassName="my-icon-addon"
>
    <InputTextField<MyCommand> 
        value={c => c.email} 
        title="Email"
        icon={<span>📧</span>}
        required
    />
</CommandForm>
```

Then define your CSS:

```css
.my-error-message {
    display: block;
    margin-top: 0.5rem;
    padding: 0.5rem;
    background-color: #fee;
    border-left: 3px solid #c00;
    color: #c00;
    font-size: 0.875rem;
    font-weight: 500;
}

.my-icon-addon {
    display: flex;
    align-items: center;
    padding: 0.75rem;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border: none;
    border-radius: 0.5rem 0 0 0.5rem;
}
```

### CSS Class Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `errorClassName` | `string` | `'p-error'` | CSS class applied to error message elements. |
| `iconAddonClassName` | `string` | `'p-inputgroup-addon'` | CSS class applied to icon addon containers. |

## Framework Integration Examples

### PrimeReact Integration

```tsx
import { CommandForm } from '@cratis/applications-react/commands';
import { Tooltip } from 'primereact/tooltip';
import 'primereact/resources/themes/lara-light-blue/theme.css';
import 'primereact/resources/primereact.min.css';

const PrimeFieldDecorator = ({ icon, description, children }) => (
    <div className="p-inputgroup">
        {icon && <span className="p-inputgroup-addon">{icon}</span>}
        {children}
    </div>
);

const PrimeErrorDisplay = ({ errors }) => (
    <div>
        {errors.map((error, idx) => (
            <small key={idx} className="p-error block mt-1">{error}</small>
        ))}
    </div>
);

function MyForm() {
    return (
        <CommandForm<MyCommand>
            command={MyCommand}
            fieldDecoratorComponent={PrimeFieldDecorator}
            errorDisplayComponent={PrimeErrorDisplay}
            errorClassName="p-error"
            iconAddonClassName="p-inputgroup-addon"
        >
            {/* Your fields */}
        </CommandForm>
    );
}
```

### Tailwind CSS Integration

```tsx
const TailwindFieldDecorator = ({ icon, description, children }) => (
    <div className="relative" title={description}>
        {icon && (
            <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
                {icon}
            </div>
        )}
        <div className={icon ? 'pl-10' : ''}>
            {children}
        </div>
    </div>
);

const TailwindErrorDisplay = ({ errors }) => (
    <div className="mt-1 space-y-1">
        {errors.map((error, idx) => (
            <p key={idx} className="text-sm text-red-600 flex items-center gap-1">
                <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
                <span>{error}</span>
            </p>
        ))}
    </div>
);

function MyForm() {
    return (
        <CommandForm<MyCommand>
            command={MyCommand}
            fieldDecoratorComponent={TailwindFieldDecorator}
            errorDisplayComponent={TailwindErrorDisplay}
            errorClassName="text-sm text-red-600 mt-1"
        >
            {/* Your fields */}
        </CommandForm>
    );
}
```

## Combining Customizations

You can combine multiple customization options for complete control:

```tsx
<CommandForm<MyCommand>
    command={MyCommand} 
    showTitles={false}                              // Disable auto titles
    showErrors={true}                               // Keep auto errors
    fieldContainerComponent={CustomFieldContainer}  // Custom container
    fieldDecoratorComponent={CustomFieldDecorator}  // Custom field decoration
    errorDisplayComponent={CustomErrorDisplay}      // Custom error rendering
    tooltipComponent={CustomTooltip}                // Custom tooltips
    errorClassName="my-error"                       // Custom error CSS class
    iconAddonClassName="my-icon"                    // Custom icon CSS class
>
    <h2>Account Information</h2>
    <InputTextField<MyCommand> 
        value={c => c.username} 
        title="Username" 
        icon={<span>👤</span>}
        description="Choose a unique username"
        required 
    />
    <InputTextField<MyCommand> 
        value={c => c.email} 
        type="email" 
        title="Email"
        icon={<span>📧</span>}
        description="We'll never share your email"
        required 
    />
    
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
