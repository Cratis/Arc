# Static Methods

Model-bound queries are implemented as static methods on your read model records. Understanding the method requirements and patterns is essential for effective query implementation.

## Method Requirements

Static methods on your read model record must follow these requirements:

- **Must be `public` and `static`**
- **Can have any descriptive name** that describes the query operation
- **Can take dependencies as parameters** (injected via dependency injection)
- **Can be async** by returning `Task<T>`
- **Should return appropriate types** - the record itself, collections, or custom result types
- **Can be observable** by returning `ISubject<T>` (do not combine with `Task<T>`)

## Basic Static Method Pattern

The simplest query method takes a dependency and returns data:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(_ => true).ToList();
    }
    
    public static DebitAccount GetAccountById(AccountId id, IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(a => a.Id == id).FirstOrDefault();
    }
}
```

## Async Methods

For database operations that support async, return `Task<T>`:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static async Task<IEnumerable<DebitAccount>> GetAllAccountsAsync(IMongoCollection<DebitAccount> collection)
    {
        var result = await collection.FindAsync(_ => true);
        return result.ToList();
    }
    
    public static async Task<DebitAccount?> GetAccountByIdAsync(AccountId id, IMongoCollection<DebitAccount> collection)
    {
        var result = await collection.FindAsync(a => a.Id == id);
        return result.FirstOrDefault();
    }
}
```

## Multiple Dependencies

Methods can take multiple dependencies from the service collection:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static async Task<IEnumerable<DebitAccount>> SearchAccounts(
        string searchTerm,
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger,
        IConfiguration configuration)
    {
        var maxResults = configuration.GetValue<int>("MaxSearchResults", 100);
        logger.LogInformation("Searching accounts with term: {SearchTerm}", searchTerm);
        
        var filter = Builders<DebitAccount>.Filter.Regex(
            a => a.Name, 
            new BsonRegularExpression(searchTerm, "i"));
        
        var result = await collection.FindAsync(filter);
        var accounts = result.Limit(maxResults).ToList();
        
        logger.LogInformation("Found {AccountCount} accounts", accounts.Count);
        return accounts;
    }
}
```

## Parameter Order Flexibility

Dependencies can be placed in any order - they are resolved by type:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    // Dependencies can come first
    public static IEnumerable<DebitAccount> GetAccountsByOwner(
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger,
        CustomerId ownerId)
    {
        logger.LogInformation("Getting accounts for owner: {OwnerId}", ownerId);
        return collection.Find(a => a.Owner == ownerId).ToList();
    }
    
    // Or query parameters can come first
    public static IEnumerable<DebitAccount> GetAccountsByBalance(
        decimal minBalance,
        bool includeZero,
        IMongoCollection<DebitAccount> collection)
    {
        var filter = includeZero 
            ? Builders<DebitAccount>.Filter.Gte(a => a.Balance, minBalance)
            : Builders<DebitAccount>.Filter.Gt(a => a.Balance, minBalance);
            
        return collection.Find(filter).ToList();
    }
}
```

## Generic Dependencies

Methods can use generic dependencies:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static async Task<IEnumerable<DebitAccount>> GetCachedAccounts(
        IMemoryCache cache,
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger)
    {
        const string cacheKey = "all-accounts";
        
        if (cache.TryGetValue(cacheKey, out IEnumerable<DebitAccount>? cached))
        {
            logger.LogInformation("Returning cached accounts");
            return cached ?? Enumerable.Empty<DebitAccount>();
        }
        
        logger.LogInformation("Loading accounts from database");
        var accounts = await collection.Find(_ => true).ToListAsync();
        
        cache.Set(cacheKey, accounts, TimeSpan.FromMinutes(5));
        return accounts;
    }
}
```

## Configuration and Options

Use `IOptions<T>` or `IConfiguration` for configuration values:

```csharp
public class AccountQueryOptions
{
    public int DefaultPageSize { get; set; } = 50;
    public int MaxPageSize { get; set; } = 200;
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);
}

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetPagedAccounts(
        int page,
        int? pageSize,
        IMongoCollection<DebitAccount> collection,
        IOptions<AccountQueryOptions> options)
    {
        var opts = options.Value;
        var actualPageSize = Math.Min(pageSize ?? opts.DefaultPageSize, opts.MaxPageSize);
        
        return collection.Find(_ => true)
            .Skip(page * actualPageSize)
            .Limit(actualPageSize)
            .ToList();
    }
}
```

## Complex Business Logic

Static methods can implement complex business logic:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static async Task<AccountRiskAssessment> AssessAccountRisk(
        AccountId accountId,
        IMongoCollection<DebitAccount> accountCollection,
        IMongoCollection<Transaction> transactionCollection,
        IRiskCalculator riskCalculator,
        ILogger<DebitAccount> logger)
    {
        logger.LogInformation("Assessing risk for account: {AccountId}", accountId);
        
        var account = await accountCollection.Find(a => a.Id == accountId).FirstOrDefaultAsync();
        if (account is null)
        {
            return new AccountRiskAssessment(accountId, RiskLevel.Unknown, "Account not found");
        }
        
        var recentTransactions = await transactionCollection
            .Find(t => t.AccountId == accountId && t.Date > DateTime.UtcNow.AddDays(-30))
            .ToListAsync();
        
        var riskScore = riskCalculator.CalculateRisk(account, recentTransactions);
        
        logger.LogInformation("Account {AccountId} risk score: {RiskScore}", accountId, riskScore);
        
        return new AccountRiskAssessment(accountId, riskScore.Level, riskScore.Reason);
    }
}
```

## Observable Methods

For real-time queries, return `ISubject<T>`:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetAccountsObservable(
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Observe(); // MongoDB extension method
    }
    
    public static ISubject<DebitAccount> GetAccountObservable(
        AccountId id,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.ObserveSingle(a => a.Id == id);
    }
}
```

## Error Handling

Implement proper error handling in your static methods:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static async Task<DebitAccount?> GetAccountSafely(
        AccountId id,
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger)
    {
        try
        {
            var result = await collection.FindAsync(a => a.Id == id);
            return result.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving account {AccountId}", id);
            return null;
        }
    }
}
```

## Method Naming Conventions

Use descriptive names that clearly indicate what the method does:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    // ✅ Good - descriptive and clear
    public static IEnumerable<DebitAccount> GetActiveAccounts(IMongoCollection<DebitAccount> collection)
        => collection.Find(a => a.Balance > 0).ToList();
    
    public static IEnumerable<DebitAccount> SearchAccountsByName(string name, IMongoCollection<DebitAccount> collection)
        => collection.Find(a => a.Name.Contains(name)).ToList();
    
    public static AccountSummary GetAccountSummaryByOwner(CustomerId ownerId, IMongoCollection<DebitAccount> collection)
    {
        var accounts = collection.Find(a => a.Owner == ownerId).ToList();
        return new AccountSummary(accounts.Count, accounts.Sum(a => a.Balance));
    }
    
    // ❌ Avoid - too generic or unclear
    public static IEnumerable<DebitAccount> Get(IMongoCollection<DebitAccount> collection) => /* ... */;
    public static IEnumerable<DebitAccount> DoSomething(string param, IMongoCollection<DebitAccount> collection) => /* ... */;
}
```

## Best Practices

1. **Use descriptive method names** - Make it clear what the query does
2. **Keep methods focused** - Each method should have a single responsibility
3. **Handle dependencies properly** - Order parameters logically (query params first or dependencies first, be consistent)
4. **Use async when appropriate** - For I/O operations that support it
5. **Implement error handling** - Handle exceptions gracefully and log appropriately
6. **Don't mix async and observable** - Use either `Task<T>` or `ISubject<T>`, not both
7. **Validate inputs** - Check parameters and throw appropriate exceptions for invalid input
8. **Use nullable return types** - When queries might not find results
9. **Leverage dependency injection** - Let the framework resolve your dependencies
10. **Keep business logic in services** - Don't put complex business logic directly in query methods
