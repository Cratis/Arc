# Command Validation

Command validation enables pre-flight validation of commands without executing them. This provides early feedback to users in React applications before performing potentially expensive or state-changing operations.

## Purpose

The validation mechanism allows you to check authorization and validation rules without executing the command handler. This is essential for:

- **Early User Feedback**: Show validation errors before the user submits a form
- **UX Improvements**: Enable/disable submit buttons based on validation state
- **Authorization Checks**: Verify user permissions without side effects
- **Progressive Validation**: Validate fields as users interact with forms

## How It Works

When you validate a command, the request is sent to the backend validation endpoint where:

1. All command filters run (authorization, validation)
2. The command handler is **not** executed
3. A `CommandResult` is returned with validation and authorization status
4. No side effects occur on the system

For details on:

- The TypeScript command validation API, see [Core Command Validation](../../core/command-validation.md)
- The backend validation pipeline, see [Backend Command Validation](../../../backend/commands/command-validation.md)

## React Hook Usage

React commands created with `.use()` include the `validate()` method:

```typescript
import { CreateOrder } from './generated/commands';

function OrderForm() {
    const [command, setValues] = CreateOrder.use();
    const [validationErrors, setValidationErrors] = useState<string[]>([]);

    const handleFieldBlur = async () => {
        // Validate on field blur for early feedback
        const result = await command.validate();
        
        if (!result.isValid) {
            setValidationErrors(result.validationResults.map(v => v.message));
        } else {
            setValidationErrors([]);
        }
    };

    const handleSubmit = async () => {
        // Execute the command
        const result = await command.execute();
        
        if (result.isSuccess) {
            // Handle success
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <input 
                value={command.orderNumber}
                onChange={e => command.orderNumber = e.target.value}
                onBlur={handleFieldBlur}
            />
            {validationErrors.map(error => (
                <div key={error} className="error">{error}</div>
            ))}
            <button type="submit">Create Order</button>
        </form>
    );
}
```

## Progressive Validation

You can validate commands reactively as properties change:

```typescript
function ProductOrderForm() {
    const [command, setValues] = CreateOrder.use();
    const [canSubmit, setCanSubmit] = useState(false);

    useEffect(() => {
        // Validate whenever command properties change
        const validateCommand = async () => {
            const result = await command.validate();
            setCanSubmit(result.isSuccess);
        };

        validateCommand();
    }, [command.hasChanges]);

    return (
        <form>
            <input 
                value={command.productId}
                onChange={e => command.productId = e.target.value}
            />
            <input 
                value={command.quantity}
                onChange={e => command.quantity = parseInt(e.target.value)}
            />
            <button 
                type="submit" 
                disabled={!canSubmit}
                onClick={() => command.execute()}
            >
                Create Order
            </button>
        </form>
    );
}
```

## Debounced Validation

To avoid excessive server calls during typing, debounce your validation:

```typescript
import { useMemo, useEffect } from 'react';
import { debounce } from 'lodash';

function OrderForm() {
    const [command] = CreateOrder.use();
    const [errors, setErrors] = useState<string[]>([]);

    const debouncedValidate = useMemo(
        () => debounce(async () => {
            const result = await command.validate();
            
            if (!result.isSuccess) {
                setErrors(result.validationResults.map(v => v.message));
            } else {
                setErrors([]);
            }
        }, 500),
        [command]
    );

    useEffect(() => {
        if (command.hasChanges) {
            debouncedValidate();
        }
    }, [command.orderNumber, command.quantity, debouncedValidate]);

    return (
        <form>
            <input 
                value={command.orderNumber}
                onChange={e => command.orderNumber = e.target.value}
            />
            {errors.map(error => (
                <div key={error} className="error">{error}</div>
            ))}
            <button onClick={() => command.execute()}>
                Create Order
            </button>
        </form>
    );
}
```

## Validation on Blur

A common pattern is to validate when a field loses focus:

```typescript
function CustomerForm() {
    const [command] = CreateCustomer.use();
    const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

    const validateField = async (fieldName: string) => {
        const result = await command.validate();
        
        if (!result.isValid) {
            const fieldError = result.validationResults.find(
                v => v.members.includes(fieldName)
            );
            
            if (fieldError) {
                setFieldErrors(prev => ({
                    ...prev,
                    [fieldName]: fieldError.message
                }));
            }
        } else {
            setFieldErrors(prev => {
                const { [fieldName]: _, ...rest } = prev;
                return rest;
            });
        }
    };

    return (
        <form>
            <div>
                <input
                    value={command.email}
                    onChange={e => command.email = e.target.value}
                    onBlur={() => validateField('email')}
                />
                {fieldErrors.email && (
                    <span className="error">{fieldErrors.email}</span>
                )}
            </div>
            <div>
                <input
                    value={command.phone}
                    onChange={e => command.phone = e.target.value}
                    onBlur={() => validateField('phone')}
                />
                {fieldErrors.phone && (
                    <span className="error">{fieldErrors.phone}</span>
                )}
            </div>
            <button onClick={() => command.execute()}>
                Create Customer
            </button>
        </form>
    );
}
```

## CommandResult Structure

Both `execute()` and `validate()` return the same `CommandResult` structure:

```typescript
interface CommandResult<TResponse> {
    correlationId: string;
    isSuccess: boolean;        // Overall success (authorized + valid + no exceptions)
    isAuthorized: boolean;     // Authorization status
    isValid: boolean;          // Validation status
    hasExceptions: boolean;    // Whether exceptions occurred
    validationResults: ValidationResult[];
    exceptionMessages: string[];
    exceptionStackTrace: string;
    response?: TResponse;      // Only populated on execute()
}
```

**Note**: The `response` property will be `null` or `undefined` when using `validate()` since the handler is not executed.

## Best Practices

### When to Use Validate

✅ **Good Use Cases:**

- Form validation as users type or blur fields
- Enabling/disabling submit buttons based on validation state
- Showing validation messages before submission
- Checking authorization before showing UI elements

❌ **Avoid:**

- Calling validate() immediately before execute() (execute already validates)
- Over-validating (don't validate on every keystroke without debouncing)
- Using validate() as a substitute for client-side validation

### Performance Considerations

- Validation makes a server round-trip, so use judiciously
- Always debounce validation calls for real-time feedback
- Client-side validation is still important for immediate feedback
- Server validation ensures security and data integrity

## Security Considerations

- Validation endpoints run the same authorization filters as execute endpoints
- Unauthorized users receive 401/403 responses from validation endpoints
- Validation does not expose sensitive data since handlers aren't executed
- Validation results may reveal authorization policies (by design)

## Troubleshooting

### Validation is slow

**Cause**: Complex validation logic or database queries in validators.

**Solution**:

- Debounce validation calls
- Optimize validator implementations on the backend
- Consider client-side validation for immediate feedback

### Validation passes but execute fails

**Cause**: State may have changed between validate and execute calls, or the handler encountered an error.

**Solution**: This is expected behavior. Always check the result of `execute()` for the authoritative status.
