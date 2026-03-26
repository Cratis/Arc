# Form Lifecycle

Control form behavior throughout its lifecycle with hooks, state management, callbacks, and auto-save functionality.

## Command Result Callbacks

Handle command execution results with dedicated callbacks. These callbacks are invoked automatically after command execution based on the result state:

```tsx
import { CommandForm } from '@cratis/arc/commands';
import { ValidationResult } from '@cratis/arc/validation';

interface CreateUserResponse {
    userId: string;
    message: string;
}

function UserForm() {
    const handleSuccess = (response: CreateUserResponse) => {
        console.log('User created with ID:', response.userId);
        // Navigate to user profile, show success message, etc.
    };
    
    const handleFailed = (result: CommandResult<CreateUserResponse>) => {
        console.error('Command failed:', result);
        // Handle general failure
    };
    
    const handleException = (messages: string[], stackTrace: string) => {
        console.error('Exception occurred:', messages);
        // Log exception, show error dialog, etc.
    };
    
    const handleUnauthorized = () => {
        console.warn('User is not authorized');
        // Redirect to login, show authorization message, etc.
    };
    
    const handleValidationFailure = (validationResults: ValidationResult[]) => {
        console.warn('Validation failed:', validationResults);
        // Additional validation failure handling beyond automatic field errors
    };
    
    return (
        <CommandForm<CreateUser, CreateUserResponse>
            command={CreateUser}
            onSuccess={handleSuccess}
            onFailed={handleFailed}
            onException={handleException}
            onUnauthorized={handleUnauthorized}
            onValidationFailure={handleValidationFailure}
        >
            <InputTextField<CreateUser> value={c => c.name} title="Name" required />
            <InputTextField<CreateUser> value={c => c.email} type="email" title="Email" required />
            <button type="submit">Create User</button>
        </CommandForm>
    );
}
```

### Available Callbacks

| Callback | Parameters | When Invoked |
|----------|------------|--------------|
| `onSuccess` | `(response: TResponse) => void` | Command executed successfully |
| `onFailed` | `(commandResult: CommandResult<TResponse>) => void` | Command execution failed (any failure type) |
| `onException` | `(messages: string[], stackTrace: string) => void` | Command threw an exception |
| `onUnauthorized` | `() => void` | User is not authorized to execute the command |
| `onValidationFailure` | `(validationResults: ValidationResult[]) => void` | Command failed validation |

### Callback Invocation Order

When a command fails, multiple callbacks may be invoked:
1. `onFailed` - Always called when `isSuccess` is `false`
2. One or more specific callbacks based on failure type:
   - `onException` if `hasExceptions` is `true`
   - `onUnauthorized` if `isAuthorized` is `false`
   - `onValidationFailure` if `isValid` is `false`

### Type-Safe Response Handling

CommandForm supports generic type parameters for type-safe responses:

```tsx
// Define response type
interface OrderResponse {
    orderId: string;
    orderNumber: string;
    totalAmount: number;
}

// Use generic type parameters
<CommandForm<CreateOrder, OrderResponse>
    command={CreateOrder}
    onSuccess={(response) => {
        // response is strongly typed as OrderResponse
        console.log(`Order ${response.orderNumber} created with ID ${response.orderId}`);
    }}
>
    {/* Form fields */}
</CommandForm>
```

### Integration with Navigation

Common pattern for navigating after successful command execution:

```tsx
import { useNavigate } from 'react-router-dom';

function CreateProjectForm() {
    const navigate = useNavigate();
    
    const handleSuccess = (response: ProjectResponse) => {
        // Navigate to the newly created project
        navigate(`/projects/${response.projectId}`);
    };
    
    return (
        <CommandForm<CreateProject, ProjectResponse>
            command={CreateProject}
            onSuccess={handleSuccess}
        >
            <InputTextField<CreateProject> value={c => c.name} title="Name" required />
            <TextAreaField<CreateProject> value={c => c.description} title="Description" />
            <button type="submit">Create Project</button>
        </CommandForm>
    );
}
```

## Before Execute Hook

Execute code before the command is submitted:

```tsx
function OrderForm() {
    const handleBeforeExecute = async (command: CreateOrder): Promise<boolean> => {
        // Confirm before submitting
        const confirmed = window.confirm('Submit this order?');
        
        if (!confirmed) {
            return false; // Cancel submission
        }
        
        // Add calculated fields
        command.totalAmount = calculateTotal(command.items);
        command.submittedAt = new Date();
        
        return true; // Proceed with submission
    };
    
    return (
        <CommandForm
            command={CreateOrder}
            beforeExecute={handleBeforeExecute}
        >
            <InputTextField<CreateOrder> value={c => c.customerName} title="Customer" required />
            {/* More fields... */}
        </CommandForm>
    );
}
```

The `beforeExecute` callback:
- Receives the command instance
- Returns `true` to proceed, `false` to cancel
- Can modify the command before submission
- Can perform async operations

## Form State Management

Track form state for enhanced UX:

```tsx
function SmartForm() {
    const command = useCommandInstance(UpdateProfile);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [lastSaved, setLastSaved] = useState<Date | null>(null);
    
    const handleExecute = async () => {
        setIsSubmitting(true);
        try {
            const result = await command.execute();
            if (result.isSuccess) {
                setLastSaved(new Date());
            }
        } finally {
            setIsSubmitting(false);
        }
    };
    
    return (
        <div>
            <CommandForm command={UpdateProfile}>
                <InputTextField<UpdateProfile> value={c => c.name} title="Name" />
                <InputTextField<UpdateProfile> value={c => c.email} type="email" title="Email" />
            </CommandForm>
            
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1rem' }}>
                <button 
                    onClick={handleExecute}
                    disabled={!command.hasChanges || isSubmitting}
                >
                    {isSubmitting ? 'Saving...' : 'Save'}
                </button>
                
                {lastSaved && (
                    <span style={{ fontSize: '0.875rem', color: '#6b7280' }}>
                        Last saved: {lastSaved.toLocaleTimeString()}
                    </span>
                )}
                
                {command.hasChanges && (
                    <span style={{ fontSize: '0.875rem', color: '#f59e0b' }}>
                        Unsaved changes
                    </span>
                )}
            </div>
        </div>
    );
}
```

## Auto-Save

Implement auto-save functionality:

```tsx
function AutoSaveForm() {
    const command = useCommandInstance(UpdateDraft);
    const timeoutRef = useRef<NodeJS.Timeout>();
    
    useEffect(() => {
        if (command.hasChanges) {
            // Clear previous timeout
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }
            
            // Set new timeout to auto-save after 2 seconds
            timeoutRef.current = setTimeout(async () => {
                await command.execute();
                console.log('Auto-saved');
            }, 2000);
        }
        
        return () => {
            if (timeoutRef.current) {
                clearTimeout(timeoutRef.current);
            }
        };
    }, [command.hasChanges]);
    
    return (
        <CommandForm command={UpdateDraft}>
            <InputTextField<UpdateDraft> value={c => c.title} title="Title" />
            <TextAreaField<UpdateDraft> value={c => c.content} title="Content" rows={10} />
            <div style={{ fontSize: '0.875rem', color: '#6b7280', marginTop: '0.5rem' }}>
                Changes are saved automatically
            </div>
        </CommandForm>
    );
}
```

## See Also

- [CommandForm Overview](./index.md)
- [Working with Hooks](./hooks.md)
- [Validation](./validation.md)
