# Query Arguments

Controller-based queries can accept arguments to filter, customize, or parameterize the data they return. Arguments can come from route parameters, query strings, or request bodies.

> **💡 Proxy Generation**: The [proxy generator](../../proxy-generation/index.md) automatically analyzes your query arguments and creates strongly-typed TypeScript interfaces, ensuring type safety between your backend and frontend.

## Route Parameters

Route parameters are embedded in the URL path and are typically used for primary identifiers:

```csharp
[Route("api/accounts")]
public class Accounts : Controller
{
    readonly IMongoCollection<DebitAccount> _collection;

    public Accounts(IMongoCollection<DebitAccount> collection) => _collection = collection;

    [HttpGet("{id}")]
    public DebitAccount GetAccountById(AccountId id)
    {
        return _collection.Find(a => a.Id == id).FirstOrDefault();
    }

    [HttpGet("owner/{ownerId}")]
    public IEnumerable<DebitAccount> GetAccountsByOwner(CustomerId ownerId)
    {
        return _collection.Find(a => a.Owner == ownerId).ToList();
    }

    [HttpGet("{id}/balance")]
    public decimal GetAccountBalance(AccountId id)
    {
        var account = _collection.Find(a => a.Id == id).FirstOrDefault();
        return account?.Balance ?? 0;
    }
}
```

## Query String Parameters

Query string parameters are appended to the URL after a `?` and are typically used for optional filters or configuration:

```csharp
[HttpGet]
public IEnumerable<DebitAccount> GetAccounts([FromQuery] string? nameFilter = null)
{
    var filter = Builders<DebitAccount>.Filter.Empty;
    
    if (!string.IsNullOrEmpty(nameFilter))
    {
        filter = Builders<DebitAccount>.Filter.Regex(
            account => account.Name, 
            new BsonRegularExpression(nameFilter, "i"));
    }
    
    return _collection.Find(filter).ToList();
}

[HttpGet("search")]
public async Task<IEnumerable<DebitAccount>> SearchAccounts(
    [FromQuery] string? name = null,
    [FromQuery] decimal? minBalance = null,
    [FromQuery] decimal? maxBalance = null,
    [FromQuery] bool includeInactive = false)
{
    var filterBuilder = Builders<DebitAccount>.Filter;
    var filters = new List<FilterDefinition<DebitAccount>>();

    if (!string.IsNullOrEmpty(name))
    {
        filters.Add(filterBuilder.Regex(a => a.Name, new BsonRegularExpression(name, "i")));
    }

    if (minBalance.HasValue)
    {
        filters.Add(filterBuilder.Gte(a => a.Balance, minBalance.Value));
    }

    if (maxBalance.HasValue)
    {
        filters.Add(filterBuilder.Lte(a => a.Balance, maxBalance.Value));
    }

    if (!includeInactive)
    {
        filters.Add(filterBuilder.Gt(a => a.Balance, 0));
    }

    var combinedFilter = filters.Any() 
        ? filterBuilder.And(filters) 
        : filterBuilder.Empty;

    var result = await _collection.FindAsync(combinedFilter);
    return result.ToList();
}
```

## Complex Query Objects

For complex queries with multiple parameters, you can create dedicated query objects:

```csharp
public record AccountSearchQuery(
    string? Name,
    decimal? MinBalance,
    decimal? MaxBalance,
    bool IncludeInactive,
    string? OwnerName);

[HttpGet("advanced-search")]
public IEnumerable<DebitAccount> SearchAccountsAdvanced([FromQuery] AccountSearchQuery query)
{
    var filterBuilder = Builders<DebitAccount>.Filter;
    var filters = new List<FilterDefinition<DebitAccount>>();

    if (!string.IsNullOrEmpty(query.Name))
    {
        filters.Add(filterBuilder.Regex(a => a.Name, new BsonRegularExpression(query.Name, "i")));
    }

    if (query.MinBalance.HasValue)
    {
        filters.Add(filterBuilder.Gte(a => a.Balance, query.MinBalance.Value));
    }

    if (query.MaxBalance.HasValue)
    {
        filters.Add(filterBuilder.Lte(a => a.Balance, query.MaxBalance.Value));
    }

    if (!query.IncludeInactive)
    {
        filters.Add(filterBuilder.Gt(a => a.Balance, 0));
    }

    // Additional complex filtering logic...
    
    var combinedFilter = filters.Any() 
        ? filterBuilder.And(filters) 
        : filterBuilder.Empty;

    return _collection.Find(combinedFilter).ToList();
}
```

## Observable Query Arguments

Observable queries can also accept arguments:

```csharp
[HttpGet("owner/{ownerId}/observable")]
public ISubject<IEnumerable<DebitAccount>> GetAccountsByOwnerObservable(CustomerId ownerId)
{
    return _collection.Observe(account => account.Owner == ownerId);
}

[HttpGet("filtered-observable")]
public ISubject<IEnumerable<DebitAccount>> GetFilteredAccountsObservable(
    [FromQuery] decimal? minBalance = null)
{
    if (minBalance.HasValue)
    {
        return _collection.Observe(account => account.Balance >= minBalance.Value);
    }
    
    return _collection.Observe();
}
```

## Argument Types

Arc supports various argument types:

### Primitive Types

```csharp
[HttpGet("by-balance")]
public IEnumerable<DebitAccount> GetAccountsByBalance(
    [FromQuery] decimal balance,
    [FromQuery] bool exactMatch = false)
{
    return exactMatch 
        ? _collection.Find(a => a.Balance == balance).ToList()
        : _collection.Find(a => a.Balance >= balance).ToList();
}
```

### Concept Types

Using concept types (value objects) for stronger typing:

```csharp
[HttpGet("by-owner-concept/{ownerId}")]
public IEnumerable<DebitAccount> GetAccountsByOwnerConcept(CustomerId ownerId)
{
    return _collection.Find(a => a.Owner == ownerId).ToList();
}
```

### Collection Arguments

```csharp
[HttpGet("by-ids")]
public IEnumerable<DebitAccount> GetAccountsByIds([FromQuery] AccountId[] ids)
{
    return _collection.Find(a => ids.Contains(a.Id)).ToList();
}

[HttpGet("by-owners")]
public IEnumerable<DebitAccount> GetAccountsByOwners([FromQuery] List<CustomerId> ownerIds)
{
    return _collection.Find(a => ownerIds.Contains(a.Owner)).ToList();
}
```

### Enums

```csharp
public enum AccountStatus { Active, Inactive, Suspended }

[HttpGet("by-status")]
public IEnumerable<DebitAccount> GetAccountsByStatus([FromQuery] AccountStatus status)
{
    // Implement status filtering logic
    return status switch
    {
        AccountStatus.Active => _collection.Find(a => a.Balance > 0).ToList(),
        AccountStatus.Inactive => _collection.Find(a => a.Balance == 0).ToList(),
        AccountStatus.Suspended => _collection.Find(a => a.Balance < 0).ToList(),
        _ => _collection.Find(_ => false).ToList()
    };
}
```

## Nullable Arguments

Optional arguments should be nullable:

```csharp
[HttpGet("flexible-search")]
public IEnumerable<DebitAccount> FlexibleSearch(
    [FromQuery] string? name = null,
    [FromQuery] CustomerId? ownerId = null,
    [FromQuery] decimal? minBalance = null,
    [FromQuery] decimal? maxBalance = null)
{
    var filterBuilder = Builders<DebitAccount>.Filter;
    var filters = new List<FilterDefinition<DebitAccount>>();

    if (!string.IsNullOrEmpty(name))
        filters.Add(filterBuilder.Regex(a => a.Name, new BsonRegularExpression(name, "i")));

    if (ownerId.HasValue)
        filters.Add(filterBuilder.Eq(a => a.Owner, ownerId.Value));

    if (minBalance.HasValue)
        filters.Add(filterBuilder.Gte(a => a.Balance, minBalance.Value));

    if (maxBalance.HasValue)
        filters.Add(filterBuilder.Lte(a => a.Balance, maxBalance.Value));

    var combinedFilter = filters.Any() 
        ? filterBuilder.And(filters) 
        : filterBuilder.Empty;

    return _collection.Find(combinedFilter).ToList();
}
```

## Default Values

Provide sensible default values for optional parameters:

```csharp
[HttpGet("paged")]
public IEnumerable<DebitAccount> GetPagedAccounts(
    [FromQuery] int page = 0,
    [FromQuery] int pageSize = 50,
    [FromQuery] string sortBy = "name",
    [FromQuery] bool ascending = true)
{
    var query = _collection.Find(_ => true);
    
    // Apply sorting
    query = ascending 
        ? query.SortBy(sortBy) 
        : query.SortByDescending(sortBy);
    
    // Apply paging
    return query.Skip(page * pageSize).Limit(pageSize).ToList();
}
```

## Request Body Arguments

For complex input that doesn't fit well in URLs, use request body parameters:

```csharp
public record ComplexSearchCriteria(
    string[] SearchTerms,
    Dictionary<string, object> CustomFilters,
    DateRange DateRange,
    SortOptions[] SortBy);

[HttpPost("complex-search")]
public async Task<IEnumerable<DebitAccount>> ComplexSearch([FromBody] ComplexSearchCriteria criteria)
{
    var filterBuilder = Builders<DebitAccount>.Filter;
    var filters = new List<FilterDefinition<DebitAccount>>();

    // Build filters from complex criteria
    foreach (var term in criteria.SearchTerms)
    {
        filters.Add(filterBuilder.Regex(a => a.Name, new BsonRegularExpression(term, "i")));
    }

    // Apply custom filters, date ranges, etc.
    
    var combinedFilter = filters.Any() 
        ? filterBuilder.And(filters) 
        : filterBuilder.Empty;

    var result = await _collection.FindAsync(combinedFilter);
    return result.ToList();
}
```

## Model Binding Attributes

Use model binding attributes to control how arguments are bound:

```csharp
[HttpGet("mixed-binding/{id}")]
public DebitAccount GetAccountMixed(
    [FromRoute] AccountId id,
    [FromQuery] bool includeDetails = false,
    [FromHeader] string acceptLanguage = "en-US")
{
    var account = _collection.Find(a => a.Id == id).FirstOrDefault();
    
    if (includeDetails && account is not null)
    {
        // Add additional details based on language preference
        // Implementation details...
    }
    
    return account;
}
```

## Validation

Add validation attributes to ensure argument quality:

```csharp
[HttpGet("validated-search")]
public IEnumerable<DebitAccount> ValidatedSearch(
    [FromQuery] [Required] [MinLength(3)] string searchTerm,
    [FromQuery] [Range(1, 100)] int pageSize = 20,
    [FromQuery] [Range(0, int.MaxValue)] int page = 0)
{
    // Validation is automatically applied
    var filter = Builders<DebitAccount>.Filter.Regex(
        a => a.Name, 
        new BsonRegularExpression(searchTerm, "i"));
    
    return _collection.Find(filter)
        .Skip(page * pageSize)
        .Limit(pageSize)
        .ToList();
}
```

## Best Practices

1. **Use route parameters for identifiers** - Things that identify specific resources
2. **Use query strings for filters** - Optional parameters that modify results
3. **Use request body for complex data** - When you need to send structured data
4. **Provide default values** - Make optional parameters truly optional
5. **Use nullable types** - For optional parameters that might not be provided
6. **Validate input** - Use validation attributes to ensure data quality
7. **Use concepts over primitives** - Leverage value objects for stronger typing
8. **Keep URLs readable** - Don't overload URLs with too many parameters

> **Note**: The [proxy generator](../../proxy-generation/index.md) automatically creates TypeScript types for your query arguments,
> making them strongly typed on the frontend as well.
