# Commands

Commands represent actions you want to perform against the system. They are encapsulated as objects that serve as the payload for HTTP Post controller actions in the backend. Commands can have validation and business rules associated with them, and controllers can have authorization policies that apply to commands.

## Overview

Commands provide a structured way to:
- Encapsulate business operations and state changes
- Validate input before execution
- Track changes from original values
- Integrate with React's rendering pipeline
- Handle authorization and error scenarios

## Quick Start

**Backend Command (C#):**

```csharp
public record OpenDebitAccount(AccountId AccountId, AccountName Name, CustomerId Owner);
```

**Generated TypeScript:**

```typescript
import { OpenDebitAccount } from './generated/commands';

const [command] = OpenDebitAccount.use({
    accountId: 'a23edccc-6cb5-44fd-a7a7-7563716fb080',
    name: 'My Account',
    owner: '84cda809-9201-4d8c-8589-0be37c6e3f18'
});

await command.execute();
```

## HTTP Headers

Commands automatically include HTTP headers provided by the `httpHeadersCallback` configured in [Arc](../arc.md). This allows you to include authentication cookies, authorization tokens, or other custom headers with every command request.

## Proxy Generation

Commands are automatically generated from your backend using the [proxy generator](../../../backend/proxy-generation.md). The generator scans HTTP Post actions during compile time and creates TypeScript classes that:

- Match your backend command structure
- Provide type-safe properties
- Include validation support
- Offer a `.use()` method for React integration
- Track changes automatically

See [Proxy Generation](../../../backend/proxy-generation.md) for setup details.

## Command Result

When executed, commands return a `CommandResult` with detailed information about the outcome:

```typescript
const result = await command.execute();

if (result.isSuccess) {
    console.log('Success!', result.response);
} else {
    if (!result.isAuthorized) {
        // Handle authorization failure
    }
    if (!result.isValid) {
        // Handle validation errors
    }
}
```

For comprehensive details, see [CommandResult documentation](../../core/command-result.md).

## Topics

| Topic | Description |
|-------|-------------|
| [React Usage](./react-usage.md) | Using the `.use()` hook in React components (recommended). |
| [Data Binding](./data-binding.md) | Binding to command properties and managing initial values for change tracking. |
| [Validation](./validation.md) | Pre-flight validation, progressive validation, and error handling. |
| [Command Scope](./scope.md) | Tracking changes across multiple commands in composite UIs. |
| [Imperative Usage](./imperative-usage.md) | Advanced direct instantiation for non-React scenarios. |

## Related Documentation

- [CommandForm](../command-form/index.md) - Declarative form component for commands
- [Queries](../queries.md) - Data retrieval operations
- [Core Commands](../../core/commands.md) - Lower-level command concepts
- [Proxy Generation](../../../backend/proxy-generation.md) - Setting up command generation

