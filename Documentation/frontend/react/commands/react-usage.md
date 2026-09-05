# React Hook Usage

Working with commands in React requires integration with React's component lifecycle and rendering pipeline. The proxy-generated command classes provide a static `.use()` method that makes commands work seamlessly with React.

## Overview

The `.use()` method integrates commands with React's rendering pipeline and provides automatic re-rendering for state changes such as `hasChanges`. It internally calls the `useCommand()` hook and returns a tuple containing:

1. **Command instance** - The actual command object with all properties and methods
2. **SetCommandValues function** - A function to update multiple command properties at once

## Basic Usage

```typescript
import { OpenDebitAccount } from './generated/commands';

export const MyComponent = () => {
    const [openDebitAccount, setCommandValues] = OpenDebitAccount.use();

    return (
        <></>
    );
};
```

## With Initial Values

The `.use()` method accepts an optional `initialValues` parameter to set the command's initial state:

```typescript
export const MyComponent = () => {
    const [openDebitAccount, setCommandValues] = OpenDebitAccount.use({
        accountId: 'a23edccc-6cb5-44fd-a7a7-7563716fb080',
        name: 'My Account',
        owner: '84cda809-9201-4d8c-8589-0be37c6e3f18'
    });

    return (
        <></>
    );
};
```

## Working with the Command Instance

The first element of the tuple gives you access to the command's properties, methods, and state:

```typescript
export const MyComponent = () => {
    const [command] = OpenDebitAccount.use();

    const handleSubmit = async () => {
        if (command.hasChanges) {
            const result = await command.execute();
            // Handle result - see CommandResult documentation
        }
    };

    return (
        <input 
            value={command.name} 
            onChange={(e) => command.name = e.target.value}
        />
    );
};
```

### Available Properties and Methods

**Properties:**
- `hasChanges` - Boolean indicating if any property has changed from initial values
- Command-specific properties (generated from your backend command)

**Methods:**
- `execute()` - Executes the command and returns a `CommandResult`
- `validate()` - Validates the command without executing it (see [Validation](./validation.md))
- `setInitialValues(values)` - Sets the initial values for change tracking

## Updating Multiple Properties

The second element of the tuple allows you to update multiple properties at once, which is useful when loading data from a query or API:

```typescript
export const MyComponent = () => {
    const [command, setCommandValues] = OpenDebitAccount.use();

    useEffect(() => {
        // Load data from an API or query
        fetchAccountData().then(data => {
            setCommandValues(data);
        });
    }, []);

    return (
        <></>
    );
};
```

This approach:
- Updates all specified properties in one operation
- Triggers a single re-render
- Maintains change tracking correctly

## Complete Example

Here's a complete example showing form handling with a command:

```typescript
import { OpenDebitAccount } from './generated/commands';
import { useState } from 'react';

export const OpenAccountForm = () => {
    const [command, setCommandValues] = OpenDebitAccount.use({
        accountId: crypto.randomUUID(),
        name: '',
        owner: ''
    });
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        
        if (!command.hasChanges) {
            return; // No changes to save
        }

        setSubmitting(true);
        setError(null);

        try {
            const result = await command.execute();
            
            if (result.isSuccess) {
                // Handle success - maybe navigate away or show success message
                console.log('Account opened successfully');
            } else {
                // Handle validation or authorization errors
                if (!result.isValid) {
                    setError('Validation failed: ' + result.validationResults.map(v => v.message).join(', '));
                } else if (!result.isAuthorized) {
                    setError('You are not authorized to perform this action');
                } else {
                    setError('An error occurred');
                }
            }
        } catch (err) {
            setError('Network error: ' + err.message);
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <div>
                <label>
                    Account Name:
                    <input
                        type="text"
                        value={command.name}
                        onChange={(e) => command.name = e.target.value}
                        required
                    />
                </label>
            </div>
            
            <div>
                <label>
                    Owner ID:
                    <input
                        type="text"
                        value={command.owner}
                        onChange={(e) => command.owner = e.target.value}
                        required
                    />
                </label>
            </div>

            {error && <div className="error">{error}</div>}
            
            <button 
                type="submit" 
                disabled={!command.hasChanges || submitting}
            >
                {submitting ? 'Opening Account...' : 'Open Account'}
            </button>
        </form>
    );
};
```

## React Re-rendering

The `.use()` hook ensures your component re-renders when:
- Command properties change
- `hasChanges` state changes
- Command execution completes

This provides a seamless integration between the command pattern and React's declarative UI model.

## Best Practices

1. **Use Destructuring**: Destructure only what you need from the tuple

   ```typescript
   const [command] = OpenDebitAccount.use(); // If you don't need setCommandValues
   ```

2. **Initialize with Data**: Pass initial values when you have them

   ```typescript
   const [command] = OpenDebitAccount.use(existingData);
   ```

3. **Check hasChanges**: Prevent unnecessary submissions

   ```typescript
   if (!command.hasChanges) return;
   ```

4. **Handle All Result States**: Always check `isSuccess`, `isValid`, and `isAuthorized`

5. **Use CommandForm**: For complex forms, consider using [CommandForm](../command-form/index.md) which handles much of this automatically

## See Also

- [Commands Overview](./index.md)
- [Data Binding](./data-binding.md)
- [Validation](./validation.md)
- [Command Scope](./scope.md)
- [CommandForm](../command-form/index.md) - Declarative form component for commands
- [Imperative Usage](./imperative-usage.md) - Advanced non-React usage
