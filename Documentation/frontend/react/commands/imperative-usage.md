# Imperative Usage

While the [React Hook Usage](./react-usage.md) is the recommended approach for React components, there are scenarios where you need more direct control or are working outside of React's component lifecycle. This guide covers imperative command usage.

## Overview

Imperative usage involves directly instantiating and manipulating command objects without React hooks. This is useful for:

- **Non-React Code**: Service layers, utility functions, or vanilla JavaScript
- **Event Handlers**: Complex operations in event handlers
- **Testing**: Unit tests and integration tests
- **Advanced Scenarios**: Custom command orchestration or batching

## Basic Imperative Usage

Directly instantiate and execute a command:

```typescript
import { OpenDebitAccount } from './generated/commands';

const command = new OpenDebitAccount();
command.accountId = 'a23edccc-6cb5-44fd-a7a7-7563716fb080';
command.name = 'My Account';
command.owner = '84cda809-9201-4d8c-8589-0be37c6e3f18';

const result = await command.execute();

if (result.isSuccess) {
    console.log('Account opened successfully');
}
```

## Setting Initial Values

For change tracking to work, set initial values using `setInitialValues()`:

```typescript
const command = new OpenDebitAccount();

command.setInitialValues({
    accountId: 'a23edccc-6cb5-44fd-a7a7-7563716fb080',
    name: 'My Account',
    owner: '84cda809-9201-4d8c-8589-0be37c6e3f18'
});

// At this point hasChanges is false
console.log(command.hasChanges); // false

command.name = 'My other account';
// Now hasChanges is true
console.log(command.hasChanges); // true
```

## Use Cases

### Service Layer Functions

```typescript
export class AccountService {
    async openAccount(accountData: { accountId: string; name: string; owner: string }) {
        const command = new OpenDebitAccount();
        command.accountId = accountData.accountId;
        command.name = accountData.name;
        command.owner = accountData.owner;

        const result = await command.execute();
        
        if (!result.isSuccess) {
            throw new Error('Failed to open account: ' + result.exceptionMessages.join(', '));
        }
        
        return result.response;
    }
}
```

### Batch Operations

```typescript
async function batchOpenAccounts(accounts: Array<{ accountId: string; name: string; owner: string }>) {
    const results = await Promise.all(
        accounts.map(async (data) => {
            const command = new OpenDebitAccount();
            command.accountId = data.accountId;
            command.name = data.name;
            command.owner = data.owner;
            return command.execute();
        })
    );

    const successful = results.filter(r => r.isSuccess);
    const failed = results.filter(r => !r.isSuccess);

    return {
        successful: successful.length,
        failed: failed.length,
        failedReasons: failed.map(r => r.exceptionMessages)
    };
}
```

### Event Handler

```typescript
async function handleAccountCreation(event: CustomEvent) {
    const { accountId, name, owner } = event.detail;

    const command = new OpenDebitAccount();
    command.accountId = accountId;
    command.name = name;
    command.owner = owner;

    const result = await command.execute();

    if (result.isSuccess) {
        // Trigger success event
        window.dispatchEvent(new CustomEvent('account-opened', { 
            detail: result.response 
        }));
    } else {
        // Trigger error event
        window.dispatchEvent(new CustomEvent('account-error', { 
            detail: result.exceptionMessages 
        }));
    }
}
```

### Testing

```typescript
import { describe, it, expect } from 'vitest';
import { OpenDebitAccount } from './generated/commands';

describe('OpenDebitAccount', () => {
    it('should successfully open an account with valid data', async () => {
        const command = new OpenDebitAccount();
        command.accountId = 'test-account-id';
        command.name = 'Test Account';
        command.owner = 'test-owner-id';

        const result = await command.execute();

        expect(result.isSuccess).toBe(true);
        expect(result.isValid).toBe(true);
        expect(result.isAuthorized).toBe(true);
    });

    it('should track changes correctly', () => {
        const command = new OpenDebitAccount();
        command.setInitialValues({
            accountId: 'test-id',
            name: 'Original Name',
            owner: 'owner-id'
        });

        expect(command.hasChanges).toBe(false);

        command.name = 'Modified Name';
        expect(command.hasChanges).toBe(true);

        command.name = 'Original Name';
        expect(command.hasChanges).toBe(false);
    });

    it('should validate without executing', async () => {
        const command = new OpenDebitAccount();
        command.accountId = '';
        command.name = '';
        command.owner = '';

        const result = await command.validate();

        expect(result.isValid).toBe(false);
        expect(result.validationResults.length).toBeGreaterThan(0);
    });
});
```

## Validation

Commands can be validated imperatively without execution:

```typescript
const command = new OpenDebitAccount();
command.accountId = 'test-id';
command.name = ''; // Invalid - empty name
command.owner = 'owner-id';

const validationResult = await command.validate();

if (!validationResult.isValid) {
    console.error('Validation failed:');
    validationResult.validationResults.forEach(error => {
        console.error(`- ${error.property}: ${error.message}`);
    });
}
```

See [Validation](./validation.md) for more details.

## Using with Factories

Create factory functions for common command patterns:

```typescript
function createAccountCommand(data: {
    accountId: string;
    name: string;
    owner: string;
    initialBalance?: number;
}): OpenDebitAccount {
    const command = new OpenDebitAccount();
    command.accountId = data.accountId;
    command.name = data.name;
    command.owner = data.owner;
    
    // Set initial values for change tracking
    command.setInitialValues(data);
    
    return command;
}

// Usage
const command = createAccountCommand({
    accountId: crypto.randomUUID(),
    name: 'Savings Account',
    owner: 'user-123'
});

await command.execute();
```

## Error Handling

Always handle errors properly with imperative usage:

```typescript
async function executeCommand(command: OpenDebitAccount) {
    try {
        const result = await command.execute();

        if (!result.isSuccess) {
            if (!result.isAuthorized) {
                throw new Error('Unauthorized');
            }
            if (!result.isValid) {
                const errors = result.validationResults.map(v => v.message).join(', ');
                throw new Error(`Validation failed: ${errors}`);
            }
            if (result.hasExceptions) {
                throw new Error(result.exceptionMessages.join(', '));
            }
        }

        return result.response;
    } catch (error) {
        console.error('Command execution failed:', error);
        throw error;
    }
}
```

## Integration with React (Advanced)

While React hooks are preferred, you can use imperative commands within React components for specific scenarios:

```typescript
import { useState } from 'react';
import { OpenDebitAccount } from './generated/commands';

export const AccountCreator = () => {
    const [processing, setProcessing] = useState(false);

    const handleQuickCreate = async () => {
        setProcessing(true);

        // Create and execute command imperatively
        const command = new OpenDebitAccount();
        command.accountId = crypto.randomUUID();
        command.name = 'Quick Account';
        command.owner = 'default-owner';

        try {
            const result = await command.execute();
            if (result.isSuccess) {
                alert('Account created!');
            }
        } finally {
            setProcessing(false);
        }
    };

    return (
        <button onClick={handleQuickCreate} disabled={processing}>
            Quick Create Account
        </button>
    );
};
```

**Note:** For React components, prefer using the [React Hook Usage](./react-usage.md) approach, as it provides automatic re-rendering and better integration with React's lifecycle.

## Best Practices

1. **Use React Hooks in Components**: Only use imperative approach when necessary
2. **Handle All Result States**: Check `isSuccess`, `isValid`, `isAuthorized`
3. **Set Initial Values**: Call `setInitialValues()` when you need change tracking
4. **Error Handling**: Always handle errors appropriately
5. **Type Safety**: Leverage TypeScript for type checking
6. **Testing**: Imperative usage is excellent for unit testing
7. **Documentation**: Document why imperative usage was chosen over hooks

## When NOT to Use Imperative Usage

Avoid imperative usage when:
- You're in a React component (use hooks instead)
- You need automatic re-rendering
- You want React lifecycle integration
- Building forms (use [CommandForm](../command-form/index.md))

## See Also

- [Commands Overview](./index.md)
- [React Hook Usage](./react-usage.md) - Recommended approach for React
- [Data Binding](./data-binding.md)
- [Validation](./validation.md)
- [CommandForm](../command-form/index.md)
- [Core Commands](../../core/commands.md) - Lower-level command concepts
