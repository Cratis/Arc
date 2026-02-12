# Proxy Generation

Cratis Arc automatically generates TypeScript proxies for your backend commands and queries, providing seamless integration between your React frontend and backend APIs.

> **Setup and Configuration**: For detailed information about setting up proxy generation, configuration options, and build integration, see the [Backend Proxy Generation](../../backend/proxy-generation/index.md) documentation.

## Type Safety and IntelliSense

One of the key benefits of the generated proxies is compile-time type safety. Your IDE will provide:

- **IntelliSense**: Auto-completion for all available commands and queries
- **Type Checking**: Compile-time verification of parameter types and return values  
- **Refactoring Support**: Automatic updates when backend APIs change
- **Navigation**: Go-to-definition support for exploring generated types

This eliminates the need to reference Swagger documentation or manually write API integration code, as everything is generated and type-safe directly in your TypeScript codebase.

## Frontend Usage

Once proxy generation is configured in your backend projects, you'll get automatically generated TypeScript proxies that provide compile-time type safety and intellisense support for all your commands and queries.

### Prerequisites

Install the base [`@cratis/arc`](https://www.npmjs.com/package/@cratis/arc) NPM package in your React project, as the generated proxies inherit from and leverage types found in this package:

```bash
npm install @cratis/arc
```

## Commands

Commands represent actions you want to perform and correspond to **HttpPost** operations on your backend controllers. The generated proxies inherit from the `Command` type found in `@cratis/arc/commands` and provide type-safe access to all command parameters.

### Example Generated Command

For a backend controller action like this:

```csharp
[Route("/api/accounts/debit")]
public class DebitAccounts : Controller
{
    [HttpPost]
    public Task OpenDebitAccount([FromBody] OpenDebitAccount create)
    {
        // Implementation...
    }
}
```

You'll get a generated TypeScript command that flattens all parameters (from route, query string, and body) into properties:

```typescript
import { OpenDebitAccount } from './generated/commands';

const command = new OpenDebitAccount();
// Set properties and execute
```

The generated command automatically handles route parameters, query string arguments, and request body serialization.

For detailed information on using commands in React, see the [Commands documentation](./commands.md).

## Queries

Queries represent data retrieval operations that correspond to **HttpGet** operations on your backend controllers. They can return either single items or collections and support parameters from routes or query strings.

### Example Generated Query

For a backend controller action like this:

```csharp
[HttpGet]
public IEnumerable<DebitAccount> AllAccounts()
{
    // Get data and return
}
```

You'll get a generated TypeScript query that provides a React hook via the `.use()` method:

```typescript
import { AllAccounts } from './generated/queries';

const MyComponent = () => {
    const [result, perform] = AllAccounts.use();
    
    // result is of type QueryResultWithState<DebitAccount[]>
    // Contains: data, isPerforming, error, etc.
    
    return (
        <div>
            {result.isPerforming && <span>Loading...</span>}
            {result.data?.map(account => <div key={account.id}>{account.name}</div>)}
        </div>
    );
};
```

The return type `QueryResultWithState<>` provides additional metadata about the query state, including whether the query is currently executing (`isPerforming`), making it easy to implement loading indicators and error handling.

### Observable Queries

Observable queries provide real-time updates to your React components, typically using WebSockets for live data synchronization.

> **Backend Setup**: To learn how to implement observable queries on the backend, see the [Observable Queries section](../../backend/queries/controller-based/observable-queries.md) in the backend documentation.

Observable queries are generated the same way as regular queries, but they don't provide a manual `perform` method in the returned tuple. Instead, they automatically subscribe to updates and re-render your React components when the underlying data changes, providing a transparent and seamless real-time experience.

## Generated File Structure

The proxy generator maintains your backend folder structure while generating TypeScript files based on the namespaces of your source files. Each namespace segment typically becomes a subfolder in the generated output.

For detailed information about configuring the output structure, including how to skip namespace segments and customize the generated folder hierarchy, see the [Backend Proxy Generation Configuration](../../backend/proxy-generation/configuration.md) section.

