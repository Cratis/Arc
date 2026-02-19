# Validation

CommandForm integrates seamlessly with the Arc command validation system to provide automatic validation feedback and error handling.

## Overview

CommandForm automatically validates field inputs and displays errors based on:

- **Required Fields**: Fields marked with `required` prop
- **Type Validation**: Built-in HTML5 validation (email, URL, number ranges, etc.)
- **Command Validation Rules**: Backend validation rules defined on your command
- **Custom Validation**: Custom validators you define

## Required Fields

Mark fields as required using the `required` prop:

```tsx
<CommandForm<RegisterUser> command={RegisterUser}>
    <InputTextField<RegisterUser> value={c => c.email} type="email" title="Email" required />
    <InputTextField<RegisterUser> value={c => c.password} type="password" title="Password" required />
    <CheckboxField<RegisterUser> value={c => c.agreeToTerms} title="Terms" label="I agree" required />
</CommandForm>
```

Required fields:
- Show visual indicator when invalid
- Prevent form submission when empty
- Display error messages when validation fails

## Automatic Error Display

By default, CommandForm displays error messages below each invalid field:

```tsx
// Errors shown automatically for invalid/required fields
<CommandForm<CreateAccount> command={CreateAccount}>
    <InputTextField<CreateAccount> value={c => c.username} title="Username" required />
    {/* Error appears here if username is empty or invalid */}
    
    <InputTextField<CreateAccount> value={c => c.email} type="email" title="Email" required />
    {/* Error appears here if email is invalid format */}
</CommandForm>
```

### Disabling Error Display

Disable automatic errors to implement custom error rendering:

```tsx
<CommandForm<CreateAccount> command={CreateAccount} showErrors={false}>
    <InputTextField<CreateAccount> value={c => c.username} title="Username" required />
    {/* No automatic error rendering */}
</CommandForm>
```

See [Customization](./customization.md) for custom error rendering patterns.

## HTML5 Validation

Field components leverage HTML5 validation attributes:

```tsx
<CommandForm<UpdateProfile> command={UpdateProfile}>
    {/* Email format validation */}
    <InputTextField<UpdateProfile>
        value={c => c.email} 
        type="email" 
        title="Email" 
        required 
    />
    
    {/* URL format validation */}
    <InputTextField<UpdateProfile>
        value={c => c.website} 
        type="url" 
        title="Website" 
        placeholder="https://example.com"
    />
    
    {/* Number range validation */}
    <NumberField<UpdateProfile>
        value={c => c.age} 
        title="Age" 
        min={18} 
        max={120} 
        required 
    />
    
    {/* Pattern matching */}
    <InputTextField<UpdateProfile>
        value={c => c.phone} 
        type="tel" 
        title="Phone Number"
    />
</CommandForm>
```

## Backend Validation

CommandForm automatically propagates validation results from backend command handlers:

### Command Definition (C#)

```csharp
public record CreateUser(string Email, string Username);

public class CreateUserHandler : ICommandHandler<CreateUser>
{
    public async Task<CommandResult> Execute(CreateUser command)
    {
        // Backend validation
        if (await UserExists(command.Email))
        {
            return CommandResult.Failed("A user with this email already exists");
        }
        
        if (command.Username.Length < 3)
        {
            return CommandResult.Failed("Username must be at least 3 characters");
        }
        
        // Process command...
        return CommandResult.Success();
    }
}
```

### Form Usage

```tsx
<CommandForm<CreateUser> command={CreateUser}>
    <InputTextField<CreateUser> value={c => c.email} type="email" title="Email" required />
    <InputTextField<CreateUser> value={c => c.username} title="Username" required />
</CommandForm>
```

When the form is submitted:
1. Frontend validation runs first (required, type checking)
2. Command is sent to backend if frontend validation passes
3. Backend validation rules execute
4. Validation errors are returned and displayed in the form
5. The form remains interactive for corrections

## Accessing Validation State

Use the `useCommandFormContext` hook to access validation state programmatically:

```tsx
import { useCommandFormContext } from '@cratis/applications-react/commands';

function MyForm() {
    const { getFieldError, commandResult } = useCommandFormContext();
    const emailError = getFieldError('email');
    const hasAnyErrors = commandResult?.validationResults && commandResult.validationResults.length > 0;
    
    return (
        <CommandForm<CreateAccount> command={CreateAccount} showErrors={false}>
            <InputTextField<CreateAccount> value={c => c.email} type="email" title="Email" required />
            
            {/* Check for specific field errors */}
            {emailError && (
                <div className="error">
                    {emailError}
                </div>
            )}
            
            <InputTextField<CreateAccount> value={c => c.username} title="Username" required />
            
            {/* Check for any errors */}
            {hasAnyErrors && (
                <div className="error-summary">
                    Please fix the errors above before submitting.
                </div>
            )}
        </CommandForm>
    );
}
```

## Progressive Validation

Validate as users interact with the form using the `useCommandInstance` hook:

```tsx
import { useCommandInstance } from '@cratis/applications-react/commands';
import { useEffect } from 'react';

function MyForm() {
    const command = useCommandInstance(CreateAccount);
    const [canSubmit, setCanSubmit] = useState(false);
    
    useEffect(() => {
        // Validate whenever command changes
        const validate = async () => {
            const result = await command.validate();
            setCanSubmit(result.isValid);
        };
        
        if (command.hasChanges) {
            validate();
        }
    }, [command.hasChanges]);
    
    return (
        <CommandForm<CreateAccount> command={CreateAccount}>
            <InputTextField<CreateAccount> value={c => c.email} type="email" title="Email" required />
            <InputTextField<CreateAccount> value={c => c.username} title="Username" required />
            
            <button type="submit" disabled={!canSubmit}>
                Create Account
            </button>
        </CommandForm>
    );
}
```

## Field-Level Validation

Validate individual fields on blur for immediate feedback:

```tsx
function RegistrationForm() {
    const command = useCommandInstance(RegisterUser);
    const [emailError, setEmailError] = useState<string>();
    
    const handleEmailBlur = async () => {
        // Validate just the email field
        const result = await command.validate();
        
        if (result.hasErrors('email')) {
            setEmailError(result.getErrorsFor('email')[0]);
        } else {
            setEmailError(undefined);
        }
    };
    
    return (
        <CommandForm<RegisterUser> command={RegisterUser} showErrors={false}>
            <InputTextField<RegisterUser>
                value={c => c.email} 
                type="email" 
                title="Email" 
                required 
                onBlur={handleEmailBlur}
            />
            {emailError && <div className="error">{emailError}</div>}
        </CommandForm>
    );
}
```

## Validation Results

The command `validate()` method returns a `CommandResult` with:

```typescript
interface CommandResult {
    isSuccess: boolean;
    isValid: boolean;
    isAuthorized: boolean;
    validationResults: ValidationResult[];
    errors: Record<string, string[]>;
    
    hasErrors(property?: string): boolean;
    getErrorsFor(property: string): string[];
}
```

### Example

```tsx
const result = await command.validate();

if (!result.isValid) {
    console.log('Validation failed');
    console.log('All errors:', result.errors);
    console.log('Email errors:', result.getErrorsFor('email'));
}

if (!result.isAuthorized) {
    console.log('User not authorized to execute this command');
}
```

## Best Practices

1. **Use Required Fields**: Mark essential fields with `required` for client-side validation
2. **Type-Specific Fields**: Use appropriate input types (email, url, number) for built-in validation
3. **Backend Validation**: Always validate on the server for security and data integrity
4. **Progressive Feedback**: Consider validating on field blur for better UX
5. **Clear Messages**: Provide clear, actionable error messages
6. **Accessible Errors**: Ensure error messages are associated with their fields for screen readers
7. **Visual Feedback**: Use consistent visual styling for invalid fields

## Command Validation System

For comprehensive details on the command validation system:

- **TypeScript/React**: See [Core Command Validation](../../../core/command-validation.md)
- **Backend**: See [Backend Command Validation](../../../../backend/commands/command-validation.md)
- **Command Usage**: See [Commands Overview](../index.md)

## See Also

- [CommandForm Overview](./index.md)
- [Built-in Field Types](./field-types.md)
- [Customization](./customization.md)
- [Advanced Usage](./advanced.md)
