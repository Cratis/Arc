# Model Bound Queries

For a more lightweight approach, queries can be their own performers. This is achieved by adorning your read model record with the `[ReadModel]` attribute and implementing static methods for query operations directly on the record type.

```csharp
[ReadModel]  // The ReadModel attribute is needed
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(_ => true).ToList();
    }
}
```

> **Note**: If you're using the Cratis Arc [proxy generator](../proxy-generation/), the method name
> will become the query name for the generated TypeScript file and class.

## Key Features

Model-bound queries provide a streamlined approach to querying by:

- **Co-locating queries with data models** - Keeping query logic close to the data it operates on
- **Eliminating controller boilerplate** - No need for separate controller classes
- **Automatic dependency injection** - Dependencies are resolved and injected automatically
- **Simple static method pattern** - Clean, straightforward method signatures
- **Full async support** - Methods can be asynchronous for database operations
- **Multiple query methods** - A single read model can have many query operations
- **Flexible return types** - Support for collections, single objects, and observables
- **Built-in authorization** - Use standard ASP.NET Core authorization attributes

## When to Use Model-Bound Queries

Model-bound queries are ideal when you:

- Want to keep query logic close to your data models
- Prefer a more functional approach with static methods
- Don't need complex routing scenarios
- Want to minimize boilerplate controller code
- Have straightforward query operations without complex middleware requirements
- Are building simple CRUD-style APIs

## Key Requirements

The `[ReadModel]` attribute is required on your record type, and static methods must:

- Be `public` and `static`
- Can have any descriptive name for the query operation
- Can take dependencies as parameters (injected via dependency injection)
- Can be async by returning `Task<T>`
- Should return the record itself, collections of the record type, or custom result types
- Can be observable by returning `ISubject<T>` (do not combine with `Task<T>`)

## Related Topics

- [Static Methods](static-methods.md) - Understanding the method requirements and patterns
- [Dependency Injection](dependency-injection.md) - Method-level dependency injection for services and repositories
- [Query Arguments](query-arguments.md) - How to handle parameters and input validation
- [Return Types](return-types.md) - Different ways to return data from your queries
- [Authorization](authorization.md) - Securing your query methods with roles and policies

## Basic Async Example

Model-bound queries support asynchronous operations:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static async Task<IEnumerable<DebitAccount>> GetAllAccountsAsync(IMongoCollection<DebitAccount> collection)
    {
        var result = await collection.FindAsync(_ => true);
        return result.ToList();
    }
}
```

## Multiple Query Methods

A single read model can contain multiple query methods for different operations:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection) =>
        collection.Find(_ => true).ToList();

    public static DebitAccount GetAccountById(AccountId id, IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Id == id).FirstOrDefault();

    public static IEnumerable<DebitAccount> GetAccountsByOwner(CustomerId ownerId, IMongoCollection<DebitAccount> collection) =>
        collection.Find(a => a.Owner == ownerId).ToList();
}
```

> **Note**: The [proxy generator](../../proxy-generation/) automatically creates TypeScript types for your query methods,
> making them strongly typed on the frontend as well.
