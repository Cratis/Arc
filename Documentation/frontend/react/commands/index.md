# Commands

Commands represent actions you want to perform against the system. Arc generates their HTTP surface from model-bound commands or discovers controller actions. Either style can use validation and authorization; neither requires Chronicle or event sourcing.

## Overview

Commands provide a structured way to:

- Encapsulate business operations and state changes
- Validate input before execution
- Track changes from original values
- Integrate with React's rendering pipeline
- Handle authorization and error scenarios

## Quick Start

**Backend payload fragment (C#):** this DTO requires an already-defined controller endpoint accepting it. A record alone is not enough for proxy discovery; for a complete model-bound example, follow [getting started](/arc/frontend/getting-started/).

```csharp
public record OpenDebitAccount(AccountId AccountId, AccountName Name, CustomerId Owner);
```

**Consumer fragment:** call this hook inside a component, and execute from its event handler. This example assumes string-backed `AccountId` and `CustomerId`; use Fundamentals `Guid` values instead if the generated model maps Guid-backed concepts.

```tsx
import { OpenDebitAccount } from './generated/commands';

export function OpenAccount() {
    const [command] = OpenDebitAccount.use({
        accountId: 'a23edccc-6cb5-44fd-a7a7-7563716fb080',
        name: 'My Account',
        owner: '84cda809-9201-4d8c-8589-0be37c6e3f18'
    });
    async function open() {
        const result = await command.execute();
        console.log(result.isSuccess ? 'Opened.' : 'Could not open account.');
    }
    return <button onClick={() => void open()}>Open account</button>;
}
```

## HTTP Headers

Command fetches include `httpHeadersCallback` headers configured in [Arc](../arc.md). Supply Authorization or application headers there; cookies are browser-managed, not configurable `Cookie` request headers.

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

For comprehensive details, see [Command Result documentation](../../core/commands/command-result.md).

## Topics

| Topic | Description |
| ------- | ------------- |
| [React Usage](./react-usage.md) | Using the `.use()` hook in React components (recommended). |
| [Data Binding](./data-binding.md) | Binding to command properties and managing initial values for change tracking. |
| [Validation](./validation.md) | Pre-flight validation, progressive validation, and error handling. |
| [Command Scope](./scope.md) | Tracking changes across multiple commands in composite UIs. |
| [Imperative Usage](./imperative-usage.md) | Advanced direct instantiation for non-React scenarios. |

## Related Documentation

- [CommandForm](../command-form/index.md) - Declarative form component for commands
- [Queries](../queries/index.md) - Data retrieval operations
- [Core Commands](../../core/commands/index.md) - Lower-level command concepts
- [Proxy Generation](../../../backend/proxy-generation.md) - Setting up command generation
