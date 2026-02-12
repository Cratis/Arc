# Query Arguments

Model-bound queries can accept arguments as method parameters. Arguments are automatically bound from the HTTP request and can include route parameters, query string parameters, or complex objects.

## Method Parameters

Arguments are passed as method parameters and are automatically bound from the HTTP request:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static DebitAccount GetAccountById(
        AccountId id, 
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(a => a.Id == id).FirstOrDefault();
    }

    public static IEnumerable<DebitAccount> SearchAccounts(
        string nameFilter,
        decimal? minBalance,
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger)
    {
        logger.LogInformation("Searching accounts with filter: {Filter}", nameFilter);
        
        var filterBuilder = Builders<DebitAccount>.Filter;
        var filters = new List<FilterDefinition<DebitAccount>>();

        if (!string.IsNullOrEmpty(nameFilter))
        {
            filters.Add(filterBuilder.Regex(a => a.Name, new BsonRegularExpression(nameFilter, "i")));
        }

        if (minBalance.HasValue)
        {
            filters.Add(filterBuilder.Gte(a => a.Balance, minBalance.Value));
        }

        var combinedFilter = filters.Any() 
            ? filterBuilder.And(filters) 
            : filterBuilder.Empty;

        return collection.Find(combinedFilter).ToList();
    }
}
```

## Argument Types

Model-bound queries support various argument types:

### Primitive Types

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAccountsByBalance(
        decimal balance,
        bool exactMatch,
        IMongoCollection<DebitAccount> collection)
    {
        return exactMatch 
            ? collection.Find(a => a.Balance == balance).ToList()
            : collection.Find(a => a.Balance >= balance).ToList();
    }
}
```

### Concept Types

Using concept types (value objects) for stronger typing:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAccountsByOwnerConcept(
        CustomerId ownerId,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(a => a.Owner == ownerId).ToList();
    }
}
```

### Enums

```csharp
public enum AccountStatus { Active, Inactive, Suspended }

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAccountsByStatus(
        AccountStatus status,
        IMongoCollection<DebitAccount> collection)
    {
        // Implement status filtering logic
        return status switch
        {
            AccountStatus.Active => collection.Find(a => a.Balance > 0).ToList(),
            AccountStatus.Inactive => collection.Find(a => a.Balance == 0).ToList(),
            AccountStatus.Suspended => collection.Find(a => a.Balance < 0).ToList(),
            _ => collection.Find(_ => false).ToList()
        };
    }
}
```

### Collection Arguments

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAccountsByIds(
        IEnumerable<AccountId> ids,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(a => ids.Contains(a.Id)).ToList();
    }
    
    public static IEnumerable<DebitAccount> GetAccountsByOwners(
        List<CustomerId> ownerIds,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(a => ownerIds.Contains(a.Owner)).ToList();
    }
}
```

## Nullable Arguments

Optional arguments should be nullable:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> FlexibleSearch(
        string? name,
        CustomerId? ownerId,
        decimal? minBalance,
        decimal? maxBalance,
        IMongoCollection<DebitAccount> collection)
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

        return collection.Find(combinedFilter).ToList();
    }
}
```

## Default Values

Provide sensible default values for optional parameters:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetPagedAccounts(
        int page = 0,
        int pageSize = 50,
        string sortBy = "name",
        bool ascending = true,
        IMongoCollection<DebitAccount> collection)
    {
        var query = collection.Find(_ => true);
        
        // Apply sorting
        query = ascending 
            ? query.SortBy(sortBy) 
            : query.SortByDescending(sortBy);
        
        // Apply paging
        return query.Skip(page * pageSize).Limit(pageSize).ToList();
    }
}
```

## Complex Query Objects

For complex search criteria, create dedicated parameter objects:

```csharp
public record AccountSearchCriteria(
    string? NamePattern,
    CustomerId? OwnerId,
    decimal? MinBalance,
    decimal? MaxBalance,
    DateTime? CreatedAfter,
    DateTime? CreatedBefore,
    bool IncludeInactive);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> SearchAccounts(
        AccountSearchCriteria criteria,
        IMongoCollection<DebitAccount> collection)
    {
        var filterBuilder = Builders<DebitAccount>.Filter;
        var filters = new List<FilterDefinition<DebitAccount>>();

        if (!string.IsNullOrEmpty(criteria.NamePattern))
            filters.Add(filterBuilder.Regex(a => a.Name, new BsonRegularExpression(criteria.NamePattern, "i")));

        if (criteria.OwnerId.HasValue)
            filters.Add(filterBuilder.Eq(a => a.Owner, criteria.OwnerId.Value));

        if (criteria.MinBalance.HasValue)
            filters.Add(filterBuilder.Gte(a => a.Balance, criteria.MinBalance.Value));

        if (criteria.MaxBalance.HasValue)
            filters.Add(filterBuilder.Lte(a => a.Balance, criteria.MaxBalance.Value));

        // Add date filters if the model supports them
        // if (criteria.CreatedAfter.HasValue)
        //     filters.Add(filterBuilder.Gte(a => a.CreatedDate, criteria.CreatedAfter.Value));

        if (!criteria.IncludeInactive)
            filters.Add(filterBuilder.Gt(a => a.Balance, 0));

        var combinedFilter = filters.Any() 
            ? filterBuilder.And(filters) 
            : filterBuilder.Empty;

        return collection.Find(combinedFilter).ToList();
    }
}
```

## Parameter Order

You can mix query parameters with dependency parameters. Dependencies are resolved by type, so order is flexible:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    // Query parameters first
    public static IEnumerable<DebitAccount> GetAccountsByOwnerWithLogging(
        CustomerId ownerId,
        bool includeZeroBalance,
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger)
    {
        logger.LogInformation("Getting accounts for owner {OwnerId}, includeZero: {IncludeZero}", 
            ownerId, includeZeroBalance);

        var filter = includeZeroBalance
            ? Builders<DebitAccount>.Filter.Eq(a => a.Owner, ownerId)
            : Builders<DebitAccount>.Filter.And(
                Builders<DebitAccount>.Filter.Eq(a => a.Owner, ownerId),
                Builders<DebitAccount>.Filter.Gt(a => a.Balance, 0));

        return collection.Find(filter).ToList();
    }
    
    // Dependencies first
    public static IEnumerable<DebitAccount> GetAccountsByBalanceRange(
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger,
        decimal minBalance,
        decimal maxBalance)
    {
        logger.LogInformation("Getting accounts with balance between {Min} and {Max}", 
            minBalance, maxBalance);

        var filter = Builders<DebitAccount>.Filter.And(
            Builders<DebitAccount>.Filter.Gte(a => a.Balance, minBalance),
            Builders<DebitAccount>.Filter.Lte(a => a.Balance, maxBalance));

        return collection.Find(filter).ToList();
    }
}
```

## Validation Attributes

Use validation attributes to ensure argument quality:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> SearchWithValidation(
        [Required] [MinLength(3)] string searchTerm,
        [Range(1, 100)] int pageSize,
        [Range(0, int.MaxValue)] int page,
        IMongoCollection<DebitAccount> collection)
    {
        // Validation is automatically applied by the framework
        var filter = Builders<DebitAccount>.Filter.Regex(
            a => a.Name, 
            new BsonRegularExpression(searchTerm, "i"));
        
        return collection.Find(filter)
            .Skip(page * pageSize)
            .Limit(pageSize)
            .ToList();
    }
}
```

## Observable Query Arguments

Observable queries can also accept arguments:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetAccountsByOwnerObservable(
        CustomerId ownerId,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Observe(a => a.Owner == ownerId);
    }
    
    public static ISubject<DebitAccount> GetAccountObservable(
        AccountId id,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Observe(a => a.Id == id);
    }
}
```

## URL Binding

Arguments are automatically bound from different parts of the HTTP request:

### Route Parameters

Based on the method name and parameter names, route parameters are inferred:

```csharp
// This would typically map to: GET /api/debitaccount/getaccountbyid/{id}
public static DebitAccount GetAccountById(AccountId id, IMongoCollection<DebitAccount> collection)
{
    return collection.Find(a => a.Id == id).FirstOrDefault();
}
```

### Query String Parameters

Parameters that aren't in the route become query string parameters:

```csharp
// This would map to: GET /api/debitaccount/searchaccounts?nameFilter=abc&minBalance=100
public static IEnumerable<DebitAccount> SearchAccounts(
    string nameFilter,
    decimal? minBalance,
    IMongoCollection<DebitAccount> collection)
{
    // Implementation...
    return collection.Find(_ => true).ToList();
}
```

## Best Practices

1. **Use descriptive parameter names** - They become part of your API contract
2. **Make optional parameters nullable** - Use nullable types for optional arguments
3. **Provide default values** - For commonly used optional parameters
4. **Use concept types** - Leverage value objects for stronger typing
5. **Validate inputs** - Use validation attributes for parameter validation
6. **Keep parameter lists reasonable** - For many parameters, consider using parameter objects
7. **Order parameters logically** - Group related parameters together
8. **Use appropriate types** - Choose the most specific type that makes sense
9. **Handle null inputs gracefully** - Check for null values and handle appropriately

## URL Generation

Arc generates URLs based on your method names and parameters:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    // GET /api/debitaccount/getallaccounts
    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection) => /* ... */;
    
    // GET /api/debitaccount/getaccountbyid/{id}
    public static DebitAccount GetAccountById(AccountId id, IMongoCollection<DebitAccount> collection) => /* ... */;
    
    // GET /api/debitaccount/searchaccounts?name={name}&minBalance={minBalance}
    public static IEnumerable<DebitAccount> SearchAccounts(string? name, decimal? minBalance, IMongoCollection<DebitAccount> collection) => /* ... */;
}
```

> **Note**: The [proxy generator](../../proxy-generation/index.md) automatically creates TypeScript types for your query arguments,
> making them strongly typed on the frontend as well.
