# Validation Severity Filtering

Validation severity filtering allows you to control which validation results block command execution based on their severity level. This enables user-friendly workflows where warnings and informational messages can be shown to users for confirmation before allowing execution to proceed.

## Overview

When validating commands, validation results can have different severity levels:

- **Error** (`ValidationResultSeverity.Error = 3`): Critical issues that must be fixed
- **Warning** (`ValidationResultSeverity.Warning = 2`): Potential issues that should be reviewed
- **Information** (`ValidationResultSeverity.Information = 1`): Informational messages that don't prevent execution
- **Unknown** (`ValidationResultSeverity.Unknown = 0`): Unclassified validation results

By default, only **Error** severity results block command execution. Warnings and Information results are filtered out and don't prevent the command from executing.

## Purpose

Severity filtering enables better user experiences by allowing you to:

- **Show Warnings to Users**: Display validation warnings without blocking execution
- **Require User Confirmation**: Let users review and acknowledge warnings before proceeding
- **Progressive Execution**: First validate with strict rules, then allow users to override warnings
- **Flexible Validation**: Apply different validation strictness based on user roles or contexts

## How It Works

### Default Behavior

Without specifying an allowed severity, only **Error** level validation results block execution:

```typescript
const command = new CreateOrder();
command.orderNumber = 'ORD-12345';

// Default: only errors block execution
const result = await command.execute();

if (!result.isSuccess) {
    // Only errors are present - warnings and info were filtered out
    console.log('Errors:', result.validationResults);
}
```

### Allowing Warnings

To allow warnings (only errors block execution):

```typescript
const command = new CreateOrder();
command.orderNumber = 'ORD-12345';

// Allow warnings - only errors block execution
const result = await command.execute(ValidationResultSeverity.Warning);

if (!result.isSuccess) {
    // Only errors are present
    console.log('Errors:', result.validationResults);
}
```

### Allowing Information

To allow both warnings and information (only errors block):

```typescript
const command = new CreateOrder();
command.orderNumber = 'ORD-12345';

// Allow information - only errors block execution
const result = await command.execute(ValidationResultSeverity.Information);

if (!result.isSuccess) {
    // Only errors are present
    console.log('Errors:', result.validationResults);
}
```

## Common Patterns

### Warning Confirmation Workflow

A typical pattern is to first execute without allowing warnings, show them to the user, and then re-execute with warnings allowed if the user confirms:

```typescript
import { CreateOrder } from './generated/commands';
import { ValidationResultSeverity } from '@cratis/arc';

async function createOrderWithWarningConfirmation() {
    const command = new CreateOrder();
    command.orderNumber = 'ORD-12345';
    command.customerId = '550e8400-e29b-41d4-a716-446655440000';
    
    // First attempt: default behavior (only errors block)
    let result = await command.execute();
    
    if (!result.isSuccess) {
        // Check if we have only warnings (no errors)
        const hasOnlyWarnings = result.validationResults.every(
            v => v.severity === ValidationResultSeverity.Warning
        );
        
        if (hasOnlyWarnings) {
            // Show warnings to user
            const warnings = result.validationResults.map(v => v.message).join('\n');
            const userConfirmed = await showWarningDialog(
                'Warning',
                `The following warnings were found:\n\n${warnings}\n\nDo you want to proceed anyway?`
            );
            
            if (userConfirmed) {
                // User confirmed - re-execute allowing warnings
                result = await command.execute(ValidationResultSeverity.Warning);
            } else {
                // User cancelled
                return;
            }
        } else {
            // We have errors - show them to user
            const errors = result.validationResults.map(v => v.message).join('\n');
            showErrorDialog('Validation Errors', errors);
            return;
        }
    }
    
    if (result.isSuccess) {
        console.log('Order created successfully!');
    }
}

function showWarningDialog(title: string, message: string): Promise<boolean> {
    // Implementation depends on your UI framework
    // Return true if user confirms, false if user cancels
}

function showErrorDialog(title: string, message: string): void {
    // Implementation depends on your UI framework
}
```

### Helper Function

You can create a reusable helper function for the warning confirmation pattern:

```typescript
import { ICommand, CommandResult, ValidationResultSeverity } from '@cratis/arc';

interface ConfirmationOptions {
    title?: string;
    allowInformation?: boolean;
}

async function executeWithConfirmation<TResponse>(
    command: ICommand<any, TResponse>,
    confirmCallback: (messages: string[]) => Promise<boolean>,
    options?: ConfirmationOptions
): Promise<CommandResult<TResponse>> {
    const allowedSeverity = options?.allowInformation 
        ? ValidationResultSeverity.Information 
        : ValidationResultSeverity.Warning;
    
    // First attempt with default behavior
    let result = await command.execute();
    
    if (!result.isSuccess && !result.hasExceptions) {
        // Check if all validation results are warnings/info
        const maxSeverity = Math.max(...result.validationResults.map(v => v.severity));
        
        if (maxSeverity <= allowedSeverity) {
            // Only warnings/info present
            const messages = result.validationResults.map(v => v.message);
            const confirmed = await confirmCallback(messages);
            
            if (confirmed) {
                // Re-execute with allowed severity
                result = await command.execute(allowedSeverity);
            }
        }
    }
    
    return result;
}

// Usage
const command = new CreateOrder();
const result = await executeWithConfirmation(
    command,
    async (messages) => {
        return confirm(`Warnings:\n${messages.join('\n')}\n\nProceed anyway?`);
    }
);
```

## React Integration

For React applications, you can create a custom hook:

```typescript
import { useState } from 'react';
import { ICommand, CommandResult, ValidationResultSeverity } from '@cratis/arc';

interface UseCommandWithConfirmationResult<TResponse> {
    execute: () => Promise<void>;
    result?: CommandResult<TResponse>;
    warnings?: string[];
    isLoading: boolean;
    confirmAndExecute: () => Promise<void>;
}

export function useCommandWithConfirmation<TResponse>(
    command: ICommand<any, TResponse>
): UseCommandWithConfirmationResult<TResponse> {
    const [result, setResult] = useState<CommandResult<TResponse>>();
    const [warnings, setWarnings] = useState<string[]>();
    const [isLoading, setIsLoading] = useState(false);
    
    const execute = async () => {
        setIsLoading(true);
        try {
            const cmdResult = await command.execute();
            setResult(cmdResult);
            
            if (!cmdResult.isSuccess && !cmdResult.hasExceptions) {
                const hasOnlyWarnings = cmdResult.validationResults.every(
                    v => v.severity === ValidationResultSeverity.Warning
                );
                
                if (hasOnlyWarnings) {
                    setWarnings(cmdResult.validationResults.map(v => v.message));
                }
            }
        } finally {
            setIsLoading(false);
        }
    };
    
    const confirmAndExecute = async () => {
        setIsLoading(true);
        try {
            const cmdResult = await command.execute(ValidationResultSeverity.Warning);
            setResult(cmdResult);
            setWarnings(undefined);
        } finally {
            setIsLoading(false);
        }
    };
    
    return {
        execute,
        result,
        warnings,
        isLoading,
        confirmAndExecute
    };
}

// Usage in a component
function CreateOrderForm() {
    const command = new CreateOrder();
    const { execute, result, warnings, isLoading, confirmAndExecute } = useCommandWithConfirmation(command);
    
    const handleSubmit = async () => {
        await execute();
    };
    
    return (
        <div>
            <button onClick={handleSubmit} disabled={isLoading}>
                Create Order
            </button>
            
            {warnings && (
                <div className="warning-dialog">
                    <h3>Warnings</h3>
                    <ul>
                        {warnings.map((w, i) => <li key={i}>{w}</li>)}
                    </ul>
                    <button onClick={confirmAndExecute}>Proceed Anyway</button>
                    <button onClick={() => setWarnings(undefined)}>Cancel</button>
                </div>
            )}
            
            {result?.isSuccess && <div>Order created successfully!</div>}
        </div>
    );
}
```

## Backend Implementation

The severity filtering happens both on the client and server:

### Client-Side Filtering

Before sending the request, the client filters validation results to determine if execution should proceed:

1. Client-side validators run (if configured)
2. Validation results are filtered based on `allowedSeverity`
3. If only allowed severities remain, the request proceeds to the server
4. The `X-Allowed-Severity` header is sent with the request

### Server-Side Filtering

The backend also filters validation results:

1. The `CommandEndpointMapper` reads the `X-Allowed-Severity` header
2. The `CommandPipeline` runs all validation filters
3. Validation results are filtered based on the allowed severity
4. Only validation results with severity > `allowedSeverity` block execution

For backend implementation details, see [Backend Validation Severity Filtering](../../backend/commands/validation-severity-filtering.md).

## API Reference

### ValidationResultSeverity Enum

```typescript
enum ValidationResultSeverity {
    Unknown = 0,
    Information = 1,
    Warning = 2,
    Error = 3
}
```

### ICommand.execute()

```typescript
interface ICommand<TCommandContent, TCommandResponse> {
    /**
     * Execute the command.
     * @param allowedSeverity Optional maximum severity level to allow.
     *                       Validation results with severity higher than this will cause the command to fail.
     *                       If not specified, only Error severity blocks execution.
     */
    execute(allowedSeverity?: ValidationResultSeverity): Promise<CommandResult<TCommandResponse>>;
}
```

### Filtering Logic

The filtering logic works as follows:

- `allowedSeverity` not specified (default): Only `Error` severity results block execution
- `allowedSeverity = ValidationResultSeverity.Warning`: Only `Error` severity results block execution (warnings allowed)
- `allowedSeverity = ValidationResultSeverity.Information`: Only `Error` and `Warning` severity results block execution (information allowed)

Results with severity **greater than** the `allowedSeverity` will block execution.

## Best Practices

### When to Use Severity Filtering

✅ **Good Use Cases:**

- Orders with non-critical warnings (e.g., "Low stock" warning)
- Forms with recommendations that users can override
- Operations with soft validation rules
- Multi-step wizards where some warnings can be deferred

❌ **Avoid:**

- Critical business rules (always use Error severity)
- Security-related validations
- Data integrity checks
- Regulatory compliance requirements

### Security Considerations

- Never use severity filtering to bypass security validations
- Always use `Error` severity for authorization failures
- Critical business rules should always be `Error` severity
- Don't rely on client-side severity filtering for security (server validates too)

### Performance Tips

- Severity filtering doesn't add significant overhead
- The same validation runs regardless of allowed severity
- Use appropriate severity levels in your validators
- Consider caching validation results if executing multiple times

## Troubleshooting

### Warnings Still Block Execution

**Cause**: Not specifying `allowedSeverity` parameter or validators returning `Error` severity.

**Solution**:
- Pass `ValidationResultSeverity.Warning` to `execute()`
- Check that validators are using `Warning` severity for non-critical issues
- Verify that custom validators set the correct severity level

### Errors Are Allowed Through

**Cause**: Using too high of an `allowedSeverity` value or validators incorrectly assigning severity.

**Solution**:
- Never use `ValidationResultSeverity.Error` as `allowedSeverity`
- Verify validators are using `Error` severity for critical issues
- Check backend validator implementations

### Server Returns Different Results

**Cause**: Server-side and client-side validators may have different rules.

**Solution**:
- Server always has final authority on validation
- Ensure client-side validators match server-side rules
- Use FluentValidation with proxy generation for consistency

## Related Documentation

- [Command Validation](./command-validation.md) - Pre-flight validation without execution
- [Validation](./validation/index.md) - Client-side validation rules
- [Backend Validation Severity Filtering](../../backend/commands/validation-severity-filtering.md) - Server implementation
- [Command Filters](../../backend/commands/command-filters.md) - Backend validation pipeline
