# Route Templates

Controller-based queries use standard ASP.NET Core routing to define URL patterns and bind parameters from the URL path.

## Basic Route Configuration

Use the `[Route]` attribute on your controller class to define the base route:

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    readonly IMongoCollection<DebitAccount> _collection;

    public Accounts(IMongoCollection<DebitAccount> collection) => _collection = collection;

    [HttpGet]
    public IEnumerable<DebitAccount> GetAll() { /* ... */ }

    [HttpGet("{id}")]
    public DebitAccount GetById(AccountId id) { /* ... */ }
}
```

## Route Parameters

### Single Parameter

Route parameters are defined with curly braces in the route template:

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    [HttpGet("{id}")]
    public DebitAccount GetById(AccountId id)
    {
        return _collection.Find(a => a.Id == id).FirstOrDefault();
    }

    [HttpGet("{id}/balance")]
    public decimal GetBalance(AccountId id)
    {
        var account = _collection.Find(a => a.Id == id).FirstOrDefault();
        return account?.Balance ?? 0;
    }
}
```

### Multiple Parameters

Routes can include multiple parameters:

```csharp
[HttpGet("owner/{ownerId}/account/{accountId}")]
public DebitAccount GetAccountByOwner(CustomerId ownerId, AccountId accountId)
{
    return _collection.Find(a => a.Owner == ownerId && a.Id == accountId).FirstOrDefault();
}

[HttpGet("date/{year}/{month}")]
public IEnumerable<DebitAccount> GetAccountsByDate(int year, int month)
{
    // Implementation for date-based filtering
    return _collection.Find(_ => true).ToList();
}
```

## Named Routes

You can name routes for URL generation:

```csharp
[HttpGet("{id}", Name = "GetAccount")]
public DebitAccount GetById(AccountId id)
{
    return _collection.Find(a => a.Id == id).FirstOrDefault();
}
```

## Route Constraints

Add constraints to route parameters to improve matching:

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    // Only match numeric IDs
    [HttpGet("{id:int}")]
    public DebitAccount GetByNumericId(int id) { /* ... */ }

    // Only match GUID format
    [HttpGet("{id:guid}")]
    public DebitAccount GetByGuidId(Guid id) { /* ... */ }

    // Minimum length constraint
    [HttpGet("name/{name:minlength(3)}")]
    public IEnumerable<DebitAccount> GetByName(string name) { /* ... */ }

    // Range constraint
    [HttpGet("page/{pageNumber:int:min(1)}")]
    public IEnumerable<DebitAccount> GetPage(int pageNumber) { /* ... */ }
}
```

## Optional Parameters

Make route parameters optional with a question mark:

```csharp
[HttpGet("owner/{ownerId}/category/{category?}")]
public IEnumerable<DebitAccount> GetByOwnerAndCategory(CustomerId ownerId, string? category = null)
{
    if (string.IsNullOrEmpty(category))
    {
        return _collection.Find(a => a.Owner == ownerId).ToList();
    }
    
    // Filter by category if provided
    return _collection.Find(a => a.Owner == ownerId /* && category filter */index.md).ToList();
}
```

## Action-Specific Routes

Override the controller route for specific actions:

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    [HttpGet]
    public IEnumerable<DebitAccount> GetAll() { /* ... */ }

    [HttpGet("search")]
    public IEnumerable<DebitAccount> Search([FromQuery] string term) { /* ... */ }

    [HttpGet("by-owner/{ownerId}")]
    public IEnumerable<DebitAccount> GetByOwner(CustomerId ownerId) { /* ... */ }

    // Complete override of the base route
    [HttpGet("~/api/special/accounts/summary")]
    public AccountSummary GetSummary() { /* ... */ }
}
```

## Complex Route Patterns

### Hierarchical Resources

Model parent-child relationships in your routes:

```csharp
[Route("api/customers/{customerId}/accounts")]
public class CustomerAccounts : Controller
{
    [HttpGet]
    public IEnumerable<DebitAccount> GetAccountsByCustomer(CustomerId customerId)
    {
        return _collection.Find(a => a.Owner == customerId).ToList();
    }

    [HttpGet("{accountId}")]
    public DebitAccount GetCustomerAccount(CustomerId customerId, AccountId accountId)
    {
        return _collection.Find(a => a.Owner == customerId && a.Id == accountId).FirstOrDefault();
    }

    [HttpGet("{accountId}/transactions")]
    public IEnumerable<Transaction> GetAccountTransactions(CustomerId customerId, AccountId accountId)
    {
        // Implementation for getting transactions
        return new List<Transaction>();
    }
}
```

### Multiple Route Templates

An action can have multiple route templates:

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    [HttpGet("search")]
    [HttpGet("find")]  // Alternative route
    public IEnumerable<DebitAccount> Search([FromQuery] string term)
    {
        var filter = Builders<DebitAccount>.Filter.Regex(
            a => a.Name, 
            new BsonRegularExpression(term, "i"));
        
        return _collection.Find(filter).ToList();
    }
}
```

## Route Values and Concepts

When using Cratis concepts (value objects), the route binding works seamlessly:

```csharp
// The AccountId concept is automatically bound from the route parameter
[HttpGet("{id}")]
public DebitAccount GetAccount(AccountId id)
{
    return _collection.Find(a => a.Id == id).FirstOrDefault();
}

// Multiple concept parameters
[HttpGet("owner/{ownerId}/account/{accountId}")]
public decimal GetAccountBalanceForOwner(CustomerId ownerId, AccountId accountId)
{
    var account = _collection.Find(a => a.Owner == ownerId && a.Id == accountId).FirstOrDefault();
    return account?.Balance ?? 0;
}
```

## Route Tokens

Use route tokens for common patterns:

```csharp
// Using [controller] token
[Route("api/[controller]")]
public class Accounts : Controller
{
    // Matches: /api/accounts

    [HttpGet("[action]")]
    public IEnumerable<DebitAccount> GetAll() { /* ... */ }
    // Matches: /api/accounts/GetAll
}
```

## Query String vs Route Parameters

Choose between route parameters and query strings based on the data's role:

### Route Parameters (part of the resource identity)

```csharp
// Account ID is part of the resource identity
[HttpGet("{id}")]
public DebitAccount GetAccount(AccountId id) { /* ... */ }

// Owner ID identifies a specific subset
[HttpGet("owner/{ownerId}")]
public IEnumerable<DebitAccount> GetByOwner(CustomerId ownerId) { /* ... */ }
```

### Query String Parameters (filtering/options)

```csharp
// Filtering options
[HttpGet("search")]
public IEnumerable<DebitAccount> Search(
    [FromQuery] string? name = null,
    [FromQuery] decimal? minBalance = null,
    [FromQuery] bool includeInactive = false)
{
    // Apply filters based on query parameters
    return _collection.Find(_ => true).ToList();
}
```

## Best Practices

1. **Use meaningful route patterns** - Routes should be intuitive and RESTful
2. **Keep routes simple** - Avoid overly complex route templates
3. **Use constraints** - Add route constraints to improve matching accuracy
4. **Be consistent** - Use consistent naming and structure across your API
5. **Consider hierarchy** - Use hierarchical routes for parent-child relationships
6. **Route parameters for identity** - Use route parameters for resource identifiers
7. **Query strings for filtering** - Use query strings for optional filters and options

## Example: Complete RESTful Route Structure

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    // GET /api/accounts
    [HttpGet]
    public IEnumerable<DebitAccount> GetAll() { /* ... */ }

    // GET /api/accounts/{id}
    [HttpGet("{id}")]
    public DebitAccount GetById(AccountId id) { /* ... */ }

    // GET /api/accounts/search?name=john&minBalance=100
    [HttpGet("search")]
    public IEnumerable<DebitAccount> Search(
        [FromQuery] string? name = null,
        [FromQuery] decimal? minBalance = null) { /* ... */ }

    // GET /api/accounts/owner/{ownerId}
    [HttpGet("owner/{ownerId}")]
    public IEnumerable<DebitAccount> GetByOwner(CustomerId ownerId) { /* ... */ }

    // GET /api/accounts/{id}/balance
    [HttpGet("{id}/balance")]
    public decimal GetBalance(AccountId id) { /* ... */ }
}
```
