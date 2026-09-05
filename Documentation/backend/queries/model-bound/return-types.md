# Return Types

Model-bound queries support various return types, allowing you to structure your API responses according to your application's needs.

## Collections

### IEnumerable&lt;T&gt;

The most common return type for queries that return multiple records:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection) 
        => collection.Find(_ => true);
}
```

### List&lt;T&gt; and Arrays

You can also return concrete collection types:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static List<DebitAccount> GetAccountsList(IMongoCollection<DebitAccount> collection)
        => collection.Find(_ => true).ToList();
        
    public static DebitAccount[] GetAccountsArray(IMongoCollection<DebitAccount> collection)
        => collection.Find(_ => true).ToArray();
}
```

## Single Objects

Return individual records or null when not found:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static DebitAccount? GetFirstAccount(IMongoCollection<DebitAccount> collection)
        => collection.Find(_ => true).FirstOrDefault();
        
    public static DebitAccount GetAccountById(AccountId id, IMongoCollection<DebitAccount> collection)
        => collection.Find(a => a.Id == id).FirstOrDefault();
}
```

## Custom Return Types

Create custom types for complex query results:

```csharp
public record AccountSummary(int TotalAccounts, decimal TotalBalance, decimal AverageBalance);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static AccountSummary GetAccountSummary(IMongoCollection<DebitAccount> collection)
    {
        var accounts = collection.Find(_ => true).ToList();
        return new AccountSummary(
            accounts.Count, 
            accounts.Sum(a => a.Balance),
            accounts.Count > 0 ? accounts.Average(a => a.Balance) : 0);
    }
}
```

## Async Return Types

Wrap any return type in `Task<T>` for asynchronous operations:

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
    
    public static async Task<AccountSummary> GetAccountSummaryAsync(IMongoCollection<DebitAccount> collection)
    {
        var accounts = await collection.Find(_ => true).ToListAsync();
        return new AccountSummary(
            accounts.Count, 
            accounts.Sum(a => a.Balance),
            accounts.Count > 0 ? accounts.Average(a => a.Balance) : 0);
    }
}
```

## Observable Return Types

For real-time queries that push updates to subscribers, return `ISubject<T>`:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetAccountsObservable(
        IMongoCollection<DebitAccount> collection) => 
            collection.Observe(); // Leveraging MongoDB Extension methods
            
    public static ISubject<DebitAccount> GetAccountObservable(
        AccountId id,
        IMongoCollection<DebitAccount> collection) =>
            collection.ObserveSingle(a => a.Id == id);
}
```

> **Important**: Do not combine `Task<T>` with `ISubject<T>`. Observable methods should return `ISubject<T>` directly, not `Task<ISubject<T>>`.

## Complex Business Objects

Return rich domain objects with computed properties:

```csharp
public record AccountDetails(
    AccountId Id,
    AccountName Name,
    CustomerId Owner,
    decimal Balance,
    decimal AvailableCredit,
    AccountStatus Status,
    DateTime LastActivity);

public record CustomerInfo(CustomerId Id, string Name, string Email);

public record AccountWithCustomerInfo(
    DebitAccount Account,
    CustomerInfo Customer,
    IEnumerable<string> RecentTransactions);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static AccountDetails GetAccountDetails(
        AccountId id,
        IMongoCollection<DebitAccount> accountCollection,
        ICreditService creditService,
        ITransactionService transactionService)
    {
        var account = accountCollection.Find(a => a.Id == id).FirstOrDefault();
        if (account is null)
            throw new AccountNotFoundException(id);
            
        var availableCredit = creditService.GetAvailableCredit(id);
        var lastActivity = transactionService.GetLastActivity(id);
        var status = DetermineAccountStatus(account, lastActivity);
        
        return new AccountDetails(
            account.Id,
            account.Name,
            account.Owner,
            account.Balance,
            availableCredit,
            status,
            lastActivity);
    }
    
    private static AccountStatus DetermineAccountStatus(DebitAccount account, DateTime lastActivity)
    {
        if (account.Balance < 0) return AccountStatus.Overdrawn;
        if (lastActivity < DateTime.UtcNow.AddDays(-90)) return AccountStatus.Dormant;
        return AccountStatus.Active;
    }
}
```

## Projection Types

Create projection types for specific views of your data:

```csharp
public record AccountListItem(AccountId Id, AccountName Name, decimal Balance);
public record AccountSearchResult(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance, double RelevanceScore);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<AccountListItem> GetAccountListItems(IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(_ => true)
            .Project(a => new AccountListItem(a.Id, a.Name, a.Balance))
            .ToEnumerable();
    }
    
    public static IEnumerable<AccountSearchResult> SearchAccountsWithRelevance(
        string searchTerm,
        IMongoCollection<DebitAccount> collection)
    {
        // Implement search with relevance scoring
        var accounts = collection.Find(a => a.Name.Contains(searchTerm)).ToList();
        
        return accounts.Select(a => new AccountSearchResult(
            a.Id, 
            a.Name, 
            a.Owner, 
            a.Balance,
            CalculateRelevance(a.Name, searchTerm)));
    }
    
    private static double CalculateRelevance(string accountName, string searchTerm)
    {
        // Simple relevance calculation
        if (accountName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
            return 1.0;
        if (accountName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            return 0.8;
        return 0.5;
    }
}
```

## Nullable Return Types

Use nullable return types when queries might not find results:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    // Explicitly nullable - might not find account
    public static DebitAccount? FindAccountById(AccountId id, IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(a => a.Id == id).FirstOrDefault();
    }
    
    // Non-nullable - should throw if not found
    public static DebitAccount GetAccountById(AccountId id, IMongoCollection<DebitAccount> collection)
    {
        var account = collection.Find(a => a.Id == id).FirstOrDefault();
        return account ?? throw new AccountNotFoundException(id);
    }
}
```

## Paged Results

For large datasets, return paged results:

```csharp
public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages - 1;
    public bool HasPrevious => Page > 0;
}

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static PagedResult<DebitAccount> GetPagedAccounts(
        int page,
        int pageSize,
        IMongoCollection<DebitAccount> collection)
    {
        var totalCount = (int)collection.CountDocuments(_ => true);
        var items = collection.Find(_ => true)
            .Skip(page * pageSize)
            .Limit(pageSize)
            .ToList();
            
        return new PagedResult<DebitAccount>(items, totalCount, page, pageSize);
    }
}
```

## Error Responses

Handle errors gracefully and return appropriate types:

```csharp
public record QueryResult<T>(T? Data, bool Success, string? ErrorMessage)
{
    public static QueryResult<T> Ok(T data) => new(data, true, null);
    public static QueryResult<T> Error(string message) => new(default, false, message);
}

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static QueryResult<DebitAccount> TryGetAccountById(
        AccountId id,
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger)
    {
        try
        {
            var account = collection.Find(a => a.Id == id).FirstOrDefault();
            return account is not null 
                ? QueryResult<DebitAccount>.Ok(account)
                : QueryResult<DebitAccount>.Error($"Account with ID {id} not found");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving account {AccountId}", id);
            return QueryResult<DebitAccount>.Error("An error occurred while retrieving the account");
        }
    }
}
```

## Best Practices

1. **Use appropriate collection types** - `IEnumerable<T>` for most cases, concrete types when needed
2. **Consider nullability** - Use nullable types when queries might not return results
3. **Use async for I/O** - Wrap return types in `Task<T>` for database operations
4. **Create projection types** - Use specific types for different views of your data
5. **Handle large datasets** - Use paging for queries that might return many results
6. **Don't mix async and observable** - Use either `Task<T>` or `ISubject<T>`, never both
7. **Use custom types for complex results** - Create dedicated types for rich query responses
8. **Consider performance** - Choose return types that support efficient data access
9. **Be consistent** - Use similar return type patterns across related queries
10. **Document complex return types** - Use XML documentation for non-obvious return structures
