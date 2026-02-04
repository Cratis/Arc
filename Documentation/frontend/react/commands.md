# Commands

Commands represents actions you want to perform against the system.
These are encapsulated as objects that typically is part of the payload for a HTTP Post controller action in the backend.
The command can have validation associated with it and also have business rules associated with it.
In addition to this, the controller can have authorization policies associated with it that applies to the command.

## HTTP Headers

Commands automatically include any HTTP headers provided by the `httpHeadersCallback` configured in the [Arc](./arc.md). This allows you to dynamically include authentication cookies, authorization tokens, or other custom headers with every command request without having to manually configure each command.

## Proxy Generation

With the [proxy generator](./proxy-generation/index.md) you'll get the commands generated directly to use in the frontend.
This means you don't have to look at the Swagger API to know what you have available - the code sits there directly
in the form of a generated proxy object. The generator will look at all HTTP Post actions during compile time and
look for actions marked with `[HttpPost]` that have a parameter marked with `[FromBody]`, and assume that this is your command
representation/payload.

Take the following controller action in C#:

```csharp
[HttpPost]
public Task OpenDebitAccount([FromBody] OpenDebitAccount create) => 
    _eventLog.Append(create.AccountId, new DebitAccountOpened(create.Name, create.Owner));
```

And the command:

```csharp
public record OpenDebitAccount(AccountId AccountId, AccountName Name, CustomerId Owner);
```

The proxy generator creates a TypeScript class that extends `Command` and provides:

- Properties matching the command payload
- Property validation support
- A static `.use()` method for React integration (see [React Usage](#react-hook-usage) below)
- Automatic change tracking for data binding

## React Hook Usage

Working with the raw command can be less than intuitive in a React context which has a different
approach to lifecycle. The proxy-generated command classes provide a static `.use()` method that integrates
with React's rendering pipeline and provides re-rendering for state changes such as `hasChanges`.

The `.use()` method internally calls the `useCommand()` hook and returns a tuple containing:

1. **Command instance** - The actual command object with all properties and methods
2. **SetCommandValues function** - A function to update multiple command properties at once

### Basic Usage

```typescript
export const MyComponent = () => {
    const [openDebitAccount, setCommandValues] = OpenDebitAccount.use();

    return (
        <></>
    );
};
```

### With Initial Values

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

### Using the Tuple Values

**Command Instance:**
The first element gives you access to the command's properties, methods, and state:

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

**SetCommandValues Function:**
The second element allows you to update multiple properties at once, which is useful when loading data from a query or API:

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

This approach ensures React components re-render when the command state changes, providing a seamless integration
between the command pattern and React's declarative UI model.

## Command Result

When a command is executed, it returns a `CommandResult` that provides detailed information about the execution outcome,
including success/failure status, validation errors, authorization status, and any response data.

For comprehensive information about handling command results, including understanding the differences between
`isSuccess`, `isValid`, `isAuthorized`, and accessing response data and validation errors, see the
[CommandResult documentation](../core/command-result.md).

```typescript
const result = await command.execute();

if (result.isSuccess) {
    // Command succeeded
    console.log('Response:', result.response);
} else {
    // Handle different failure scenarios
    if (!result.isAuthorized) {
        // Show access denied message
    }
    if (!result.isValid) {
        // Display validation errors
        console.log('Validation errors:', result.validationResults);
    }
    if (result.hasExceptions) {
        // Log exceptions
        console.error('Exceptions:', result.exceptionMessages);
    }
}
```

## Data binding and initial values

The command holds properties that is the payload of what you want to have happen.
These properties are subject to validation rules and also business rules.
They are also often sourced from a read model coming from a [query](./queries.md).
Meaning that you are performing operations that in many cases are in practice updates
to existing data.

Instead of binding your frontend components to the read models from the queries, you
can bind to the properties on the command. The benefit of this is that you will then
have any validation rules that is running on the frontend automatically run as values
change. In addition, the command itself will be tracking whether or not there are changes
from the original data. On the command you'll find a property called `hasChanges` that
will return `true` if it has changes and `false` if not.

To be able to do this, the command needs to be given a set of initial values that it will
use to compare current state against. This can be done either through the React hook's initial values
parameter (recommended) or by calling `setInitialValues()` directly.

At this stage `hasChanges` will be returning `false`.
If you alter a property value, `hasChanges` will become `true`.

If you have a component that has sub components that all work with different commands,
there is a way to track the state of `hasChanges` for all these. Read more about the [command scope](./command-scope.md) for this.

## Imperative Usage (Advanced)

For scenarios where you need more control or are working outside of React's component lifecycle,
you can use commands imperatively by directly instantiating them:

```typescript
const command = new OpenDebitAccount();
command.accountId = 'a23edccc-6cb5-44fd-a7a7-7563716fb080';
command.name = 'My Account';
command.owner = '84cda809-9201-4d8c-8589-0be37c6e3f18';
const result = await command.execute();
```

### Setting Initial Values Imperatively

```typescript
const command = new OpenDebitAccount();
command.setInitialValues({
    accountId: 'a23edccc-6cb5-44fd-a7a7-7563716fb080',
    name: 'My Account',
    owner: '84cda809-9201-4d8c-8589-0be37c6e3f18'
});

// At this point hasChanges is false
command.name = 'My other account';
// Now hasChanges is true
```

**Note:** For React components, prefer using the [React Hook Usage](#react-hook-usage) approach, as it provides
automatic re-rendering and better integration with React's lifecycle.

