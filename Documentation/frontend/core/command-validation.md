# Command Validation

Command validation enables pre-flight validation of commands without executing them. This provides early feedback to users before performing potentially expensive or state-changing operations.

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

For details on the backend validation pipeline, see [Backend Command Validation](../../backend/commands/command-validation.md).

## Command.validate() Method

All generated TypeScript command proxies include a `validate()` method alongside the `execute()` method:

```typescript
interface ICommand<TCommandContent, TCommandResponse> {
    /**
     * Validate the command without executing it.
     * Returns validation and authorization status.
     */
    validate(): Promise<CommandResult<TCommandResponse>>;
    
    /**
     * Execute the command.
     */
    execute(): Promise<CommandResult<TCommandResponse>>;
}
```

## Basic Usage

```typescript
import { CreateOrder } from './generated/commands';

async function validateOrder() {
    const command = new CreateOrder();
    command.orderNumber = 'ORD-12345';
    command.customerId = '550e8400-e29b-41d4-a716-446655440000';
    
    // Validate without executing
    const result = await command.validate();
    
    if (result.isSuccess) {
        console.log('Command is valid and authorized');
    } else {
        if (!result.isAuthorized) {
            console.log('User not authorized');
        }
        if (!result.isValid) {
            console.log('Validation errors:', result.validationResults);
        }
    }
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

interface ValidationResult {
    message: string;
    members: string[];
    severity: 'Error' | 'Warning' | 'Info';
}
```

**Important**: The `response` property will be `null` or `undefined` when using `validate()` since the handler is not executed.

## Validation Filters

The `validate()` method runs all registered command filters on the backend:

- **AuthorizationFilter**: Checks user permissions
- **DataAnnotationValidationFilter**: Validates data annotations
- **FluentValidationFilter**: Runs FluentValidation validators

For more information, see [Backend Command Filters](../../backend/commands/command-filters.md).

## Best Practices

### When to Use Validate

✅ **Good Use Cases:**

- Form validation as users type or blur fields
- Enabling/disabling submit buttons based on validation state
- Showing validation messages before submission
- Checking authorization before showing UI elements

❌ **Avoid:**

- Calling validate() immediately before execute() (execute already validates)
- Over-validating (don't validate on every keystroke for performance)
- Using validate() as a substitute for client-side validation

### Performance Considerations

- Validation makes a server round-trip, so use judiciously
- Consider debouncing validation calls for real-time feedback
- Client-side validation is still important for immediate feedback
- Server validation ensures security and data integrity

### Example: Debounced Validation

```typescript
let validationTimeout: NodeJS.Timeout;

function debounceValidation(command: ICommand<any, any>, onResult: (result: CommandResult<any>) => void) {
    clearTimeout(validationTimeout);
    
    validationTimeout = setTimeout(async () => {
        const result = await command.validate();
        onResult(result);
    }, 500);
}

// Usage
const command = new CreateOrder();
command.orderNumber = 'ORD-12345';

debounceValidation(command, (result) => {
    if (!result.isSuccess) {
        console.log('Validation errors:', result.validationResults);
    }
});
```

## Security Considerations

- Validation endpoints run the same authorization filters as execute endpoints
- Unauthorized users receive 401/403 responses from validation endpoints
- Validation does not expose sensitive data since handlers aren't executed
- Validation results may reveal authorization policies (by design)

## Framework-Specific Usage

For React-specific patterns and hooks, see [React Command Validation](../react/commands/validation.md).
