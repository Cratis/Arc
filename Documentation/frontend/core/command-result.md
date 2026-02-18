# CommandResult

When a command is executed, it returns a `CommandResult<TResponse>` that provides comprehensive information about the execution outcome. The result includes success/failure status, validation errors, authorization status, exceptions, and optional response data.

## Structure

The `CommandResult` interface provides the following properties:

```typescript
interface ICommandResult<TResponse = object> {
    readonly correlationId: Guid;
    readonly isSuccess: boolean;
    readonly isAuthorized: boolean;
    readonly isValid: boolean;
    readonly hasExceptions: boolean;
    readonly validationResults: ValidationResult[];
    readonly exceptionMessages: string[];
    readonly exceptionStackTrace: string;
    readonly response?: TResponse;
}
```

## Status Properties

Understanding the different status properties is crucial for proper error handling:

### isSuccess

Indicates whether the command executed successfully **overall**. This is the primary indicator of whether the operation completed as intended.

- `true`: The command executed without any issues (authorized, valid, and no exceptions)
- `false`: The command failed for one or more reasons (could be authorization, validation, or exceptions)

**Use this when:**

- You want to know if the overall operation succeeded
- You need a single boolean check for success/failure
- You're implementing simple success/failure UI feedback

```typescript
const result = await command.execute();
if (result.isSuccess) {
    // Show success message
} else {
    // Handle failure (check other properties for details)
}
```

### isAuthorized

Indicates whether the user is authorized to execute this command.

- `true`: The user has permission to execute the command
- `false`: The user lacks the necessary permissions (HTTP 401/403)

**Use this when:**

- You need to distinguish authorization failures from other errors
- You want to show specific "access denied" messages
- You need to redirect users to login or show permission requests

```typescript
const result = await command.execute();
if (!result.isAuthorized) {
    // Show "Access Denied" message
    // Redirect to login or request permissions
}
```

### isValid

Indicates whether the command payload passed validation rules.

- `true`: All validation rules passed
- `false`: One or more validation rules failed (check `validationResults` for details)

**Use this when:**

- You need to display validation errors to users
- You want to highlight invalid form fields
- You're implementing client-side validation feedback

```typescript
const result = await command.execute();
if (!result.isValid) {
    // Display validation errors from result.validationResults
    result.validationResults.forEach(error => {
        console.log(`${error.members.join('.')}: ${error.message}`);
    });
}
```

### hasExceptions

Indicates whether any exceptions occurred during command execution.

- `true`: One or more exceptions were thrown
- `false`: No exceptions occurred

**Use this when:**

- You need to catch unexpected server errors
- You want to log errors for debugging
- You need to show generic error messages for system failures

```typescript
const result = await command.execute();
if (result.hasExceptions) {
    // Log exception details
    console.error('Exceptions:', result.exceptionMessages);
    console.error('Stack trace:', result.exceptionStackTrace);
    
    // Show user-friendly error message
    showErrorMessage('An unexpected error occurred. Please try again.');
}
```

## Understanding the Relationship

The status properties work together to provide a complete picture:

```typescript
// Scenario 1: Complete success
// isSuccess = true, isAuthorized = true, isValid = true, hasExceptions = false

// Scenario 2: Authorization failure
// isSuccess = false, isAuthorized = false, isValid = true, hasExceptions = false

// Scenario 3: Validation failure
// isSuccess = false, isAuthorized = true, isValid = false, hasExceptions = false

// Scenario 4: Exception occurred
// isSuccess = false, isAuthorized = true, isValid = true, hasExceptions = true

// Scenario 5: Multiple failures (validation + exception)
// isSuccess = false, isAuthorized = true, isValid = false, hasExceptions = true
```

**Key principle:** If `isSuccess` is `false`, check the other properties to determine **why** it failed.

## Accessing Data

### Response Data

When the command returns data, access it through the `response` property:

```typescript
interface CreateAccountResponse {
    accountId: string;
    accountNumber: string;
}

const result = await createAccountCommand.execute();
if (result.isSuccess && result.response) {
    const { accountId, accountNumber } = result.response;
    console.log(`Account created: ${accountNumber}`);
}
```

The response is typed according to the command's generic parameter, providing full type safety.

### Validation Results

Access detailed validation errors through the `validationResults` array:

```typescript
const result = await command.execute();
if (!result.isValid) {
    result.validationResults.forEach(error => {
        console.log(`Severity: ${error.severity}`);
        console.log(`Message: ${error.message}`);
        console.log(`Members: ${error.members.join('.')}`);
        console.log(`State: ${JSON.stringify(error.state)}`);
    });
}
```

Each `ValidationResult` contains:

- `severity`: The severity level of the validation error
- `message`: Human-readable error message
- `members`: Array of property names that failed validation
- `state`: Additional context about the validation failure

### Exception Details

Access exception information when `hasExceptions` is `true`:

```typescript
const result = await command.execute();
if (result.hasExceptions) {
    // Array of exception messages
    result.exceptionMessages.forEach(msg => {
        console.error('Exception:', msg);
    });
    
    // Full stack trace for debugging
    console.error('Stack trace:', result.exceptionStackTrace);
}
```

## Chaining Callbacks

`CommandResult` supports a fluent API for handling different outcomes:

```typescript
const result = await command.execute();

result
    .onSuccess((response) => {
        console.log('Success!', response);
    })
    .onFailed((commandResult) => {
        console.log('Failed:', commandResult);
    })
    .onUnauthorized(() => {
        console.log('Not authorized');
    })
    .onValidationFailure((validationResults) => {
        console.log('Validation errors:', validationResults);
    })
    .onException((messages, stackTrace) => {
        console.error('Exception:', messages, stackTrace);
    });
```

Each callback method returns the `CommandResult` instance, allowing you to chain multiple handlers.

## Best Practices

### 1. Check isSuccess First

Always start by checking `isSuccess` for the overall outcome:

```typescript
const result = await command.execute();
if (result.isSuccess) {
    // Handle success
} else {
    // Check specific failure reasons
    if (!result.isAuthorized) { /* ... */ }
    if (!result.isValid) { /* ... */ }
    if (result.hasExceptions) { /* ... */ }
}
```

### 2. Provide Specific Error Messages

Use the specific status properties to give users meaningful feedback:

```typescript
const result = await command.execute();
if (!result.isSuccess) {
    if (!result.isAuthorized) {
        showMessage('You do not have permission to perform this action.');
    } else if (!result.isValid) {
        showValidationErrors(result.validationResults);
    } else if (result.hasExceptions) {
        showMessage('An unexpected error occurred. Please contact support.');
    }
}
```

### 3. Log Exceptions for Debugging

Always log exception details when `hasExceptions` is `true`:

```typescript
if (result.hasExceptions) {
    logger.error('Command execution failed', {
        messages: result.exceptionMessages,
        stackTrace: result.exceptionStackTrace,
        correlationId: result.correlationId
    });
}
```

### 4. Use Correlation ID for Tracking

The `correlationId` helps track command executions across the system:

```typescript
console.log(`Command executed with correlation ID: ${result.correlationId}`);
```

This is especially useful for debugging and support scenarios.

## Related Topics

- [Commands](./commands.md) - Core command concepts and usage
- [React Commands](../react/commands/index.md) - Using commands in React components
- [Validation](../../backend/commands/validation.md) - Understanding validation rules
