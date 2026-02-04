# Controller Based Queries

You can represent queries as regular ASP.NET Core Controller actions with HTTP GET methods.

```csharp
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance);

[Route("api/accounts")]
public class Accounts : Controller
{
    readonly IMongoCollection<DebitAccount> _collection;

    public Accounts(IMongoCollection<DebitAccount> collection) => _collection = collection;

    [HttpGet]
    public IEnumerable<DebitAccount> AllAccounts() => _collection.Find(_ => true).ToList();
}
```

> Note: This particular model represents its values as concepts - a value type encapsulation that
> makes us not use primitives - thus creating clearer APIs and models.
> If you're using the Cratis Arc [proxy generator](../../proxy-generation/index.md), the method name
> will become the query name for the generated TypeScript file and class.

## Key Features

Controller-based queries provide several powerful features:

- **Standard ASP.NET Core routing** and HTTP verb support
- **Flexible return types** including collections, single objects, and custom response wrappers
- **Dependency injection** for services and repositories
- **Query arguments** via route parameters, query strings, and request bodies
- **Async support** for asynchronous operations
- **Observable queries** for real-time data streaming
- **Custom route templates** for RESTful API design

## When to Use Controller-Based Queries

Controller-based queries are ideal when you:

- Want explicit control over HTTP routing and URL structure
- Need to leverage existing ASP.NET Core features like filters, middleware, or custom attributes
- Are building RESTful APIs with standard HTTP conventions
- Want to separate query logic from your read models
- Need complex routing scenarios with multiple parameters

## Bypassing Query Result Wrappers

By default, controller-based queries return results wrapped in a `QueryResult` structure. If you need to return the raw result from your controller action without this wrapper, you can use the `[AspNetResult]` attribute. For more details, see [Without wrappers](../../without-wrappers.md).

## Related Topics

- [Route Templates](route-templates.md) - Learn about URL routing and parameter binding
- [Query Arguments](query-arguments.md) - How to handle different types of query parameters
- [Return Types](return-types.md) - Understanding different response formats
- [Observable Queries](observable-queries.md) - Real-time data streaming with WebSockets
- [Dependency Injection](dependency-injection.md) - Working with services and repositories

## Basic Example with Async Support

For asynchronous operations, you can return `Task<T>`:

```csharp
[HttpGet]
public async Task<IEnumerable<DebitAccount>> AllAccountsAsync()
{
    var result = await _collection.FindAsync(_ => true);
    return result.ToList();
}
```

> **Note**: The [proxy generator](../../proxy-generation/index.md) automatically creates TypeScript types for your controller methods,
> making them strongly typed on the frontend as well.
