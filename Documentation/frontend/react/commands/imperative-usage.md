---
title: Execute Cratis Arc commands without React hooks
description: Instantiate generated Cratis Arc command proxies in service functions or event handlers, establish a change baseline, and handle execution results.
---

Instantiate a generated Cratis Arc command directly when a service function or event handler needs to submit work without a React hook. Assign its input properties, call `execute()`, and inspect the returned result before using its response. For component-bound editing, [React hooks](./react-usage.md) keep the command connected to React rendering and context.

## Overview

Direct construction gives the calling code ownership of the command's lifetime and UI state. It does not read a React provider or register the instance with a command scope. A new instance uses the global origin, API base path, and microservice settings; set instance configuration and the HTTP-header callback explicitly when the caller needs them.

The examples assume an illustrative generated `OpenDebitAccount` proxy with string `accountId`, `name`, and `owner` properties. Use the types and validation rules from your own generated proxy; the placeholder import path represents your application's generated output.

## Basic Imperative Usage

Create a command for one operation, supply its inputs, and await execution:

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

When editing existing content, call `setInitialValues()` to establish a baseline: the values that count as unchanged. Subsequent property assignments are compared with that baseline. A one-shot creation command does not need a baseline before it can execute.

```typescript
const command = new OpenDebitAccount();

command.setInitialValues({
    accountId: 'a23edccc-6cb5-44fd-a7a7-7563716fb080',
    name: 'My Account',
    owner: '84cda809-9201-4d8c-8589-0be37c6e3f18',
});

// At this point hasChanges is false
console.log(command.hasChanges); // false

command.name = 'My other account';
// Now hasChanges is true
console.log(command.hasChanges); // true
```

## Use Cases

### Service Layer Functions

A service can translate an unsuccessful result into an application error. This example returns the response only after checking `isSuccess`; for field-level feedback, keep the full result rather than reducing it to exception messages.

```typescript
export class AccountService {
    async openAccount(accountData: { accountId: string; name: string; owner: string }) {
        const command = new OpenDebitAccount();
        command.accountId = accountData.accountId;
        command.name = accountData.name;
        command.owner = accountData.owner;

        const result = await command.execute();

        if (!result.isSuccess) {
            throw new Error(
                'Failed to open account: ' + result.exceptionMessages.join(', '),
            );
        }

        return result.response;
    }
}
```

### Batch Operations

Use a separate instance for each account so concurrent operations do not overwrite one another's content. `Promise.all()` starts these independent requests concurrently; it does not make them a transaction or roll back successful accounts when another fails.

```typescript
async function batchOpenAccounts(
    accounts: Array<{ accountId: string; name: string; owner: string }>,
) {
    const results = await Promise.all(
        accounts.map(async (data) => {
            const command = new OpenDebitAccount();
            command.accountId = data.accountId;
            command.name = data.name;
            command.owner = data.owner;
            return command.execute();
        }),
    );

    const successful = results.filter((r) => r.isSuccess);
    const failed = results.filter((r) => !r.isSuccess);

    return {
        successful: successful.length,
        failed: failed.length,
        failedReasons: failed.map((r) => r.exceptionMessages),
    };
}
```

### Event Handler

Outside React, the caller decides how to publish the outcome. Here, a custom browser event carries either the successful response or exception messages. Add validation and authorization feedback if listeners need to distinguish those failures.

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
        window.dispatchEvent(
            new CustomEvent('account-opened', {
                detail: result.response,
            }),
        );
    } else {
        // Trigger error event
        window.dispatchEvent(
            new CustomEvent('account-error', {
                detail: result.exceptionMessages,
            }),
        );
    }
}
```

### Testing

The change-tracking test below operates locally on the proxy. The execution test needs a reachable backend with matching routes, authorization, and data, or a controlled HTTP stub. The validation test assumes rules that reject empty values; required-property checks alone reject null and undefined, not empty strings. These examples are not an isolated unit-test suite without that setup.

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
            owner: 'owner-id',
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

Call `validate()` to check a draft without executing its handler. Local blocking failures can return before a server request; otherwise the backend runs its validation filters. The empty name below is invalid only if the command defines a rule that rejects it.

```typescript
const command = new OpenDebitAccount();
command.accountId = 'test-id';
command.name = ''; // Invalid - empty name
command.owner = 'owner-id';

const validationResult = await command.validate();

if (!validationResult.isValid) {
    console.error('Validation failed:');
    validationResult.validationResults.forEach((error) => {
        console.error(`- ${error.members.join(', ')}: ${error.message}`);
    });
}
```

Validation feedback can become stale if content changes while the check is pending. `execute()` validates again, so always inspect its result. Follow [validation](./validation.md) for revision guards and message handling.

## Using with Factories

A factory can establish a consistent starting point for an editor. Calling `setInitialValues(data)` accepts that content as unchanged; it does not submit it. In this example, only properties declared by the generated proxy participate in command content, so `initialBalance` needs a matching generated property if you intend to send it.

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
    owner: 'user-123',
});

await command.execute();
```

## Error Handling

An unsuccessful command normally returns a `CommandResult` rather than throwing. Check authorization, validation, and exception details before using `response`. The wrapper below converts those outcomes into thrown errors for callers that use exceptions; it also logs and rethrows unexpected errors. Choose application-safe messages before presenting them to users.

```typescript
async function executeCommand(command: OpenDebitAccount) {
    try {
        const result = await command.execute();

        if (!result.isSuccess) {
            if (!result.isAuthorized) {
                throw new Error('Unauthorized');
            }
            if (!result.isValid) {
                const errors = result.validationResults.map((v) => v.message).join(', ');
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

A React click handler can create a one-shot command when there is no persistent draft to bind. Because this instance does not use the hook, the component manages its own processing state and must configure the command separately if it depends on provider settings. This example shows only success feedback; add failure feedback for an application UI.

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

:::tip[Prefer the hook in React components]
Use the [React hook approach](./react-usage.md) in components for automatic re-rendering and integration with React's lifecycle.
:::

## Best Practices

Keep one command instance per independent draft or operation, and retain the generated content and response types. Establish initial values when editing existing content rather than marking every input assignment as a new baseline.

After successful execution, the submitted snapshot becomes the baseline. Unsuccessful execution leaves the existing baseline unchanged. Use `CommandResult` for save feedback rather than inferring success from `hasChanges`; [data binding](./data-binding.md#execution-baseline-limitations) explains how edits made during execution are tracked.

## When NOT to Use Imperative Usage

Use the generated hook when inputs need to render each edit or the command needs the nearest React context and scope. Direct property assignment on a manually constructed instance does not connect it to React rendering. For forms that need managed validation and submission, use [CommandForm](../command-form/index.md) instead of building that lifecycle around a one-shot instance.

## See Also

- [Commands Overview](./index.mdx)
- [React Hook Usage](./react-usage.md) - Recommended approach for React
- [Data Binding](./data-binding.md)
- [Validation](./validation.md)
- [CommandForm](../command-form/index.md)
- [Core Commands](../../core/commands/index.md) - Lower-level command concepts
