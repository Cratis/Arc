# Return Types

Controller-based queries support various data types for return values, giving you flexibility in how you structure your API responses.

## Single Object

Return a single instance of your read model:

```csharp
[HttpGet("{id}")]
public DebitAccount GetAccount(AccountId id)
{
    return _collection.Find(a => a.Id == id).FirstOrDefault();
}
```

## Collections

### IEnumerable&lt;T&gt;

```csharp
[HttpGet]
public IEnumerable<DebitAccount> GetAccounts()
{
    return _collection.Find(_ => true).ToList();
}
```

### List&lt;T&gt;

```csharp
[HttpGet]
public List<DebitAccount> GetAccountsList()
{
    return _collection.Find(_ => true).ToList();
}
```

### Arrays

```csharp
[HttpGet]
public DebitAccount[] GetAccountsArray()
{
    return _collection.Find(_ => true).ToArray();
}
```

## Query Results

For more control over the response metadata, you can return `QueryResult<T>`:

```csharp
[HttpGet]
public QueryResult<IEnumerable<DebitAccount>> GetAccountsWithMetadata()
{
    var accounts = _collection.Find(_ => true).ToList();
    return new QueryResult<IEnumerable<DebitAccount>>
    {
        Data = accounts,
        // Additional metadata will be populated automatically
    };
}
```

## Async Return Types

All return types can be wrapped in `Task<T>` for asynchronous operations:

```csharp
[HttpGet]
public async Task<IEnumerable<DebitAccount>> GetAccountsAsync()
{
    var result = await _collection.FindAsync(_ => true);
    return result.ToList();
}

[HttpGet("{id}")]
public async Task<DebitAccount> GetAccountAsync(AccountId id)
{
    var result = await _collection.FindAsync(a => a.Id == id);
    return result.FirstOrDefault();
}
```

## Custom Response Types

You can create custom types for complex query results:

```csharp
public record AccountSummary(int TotalAccounts, decimal TotalBalance, decimal AverageBalance);

[HttpGet("summary")]
public AccountSummary GetAccountSummary()
{
    var accounts = _collection.Find(_ => true).ToList();
    return new AccountSummary(
        accounts.Count,
        accounts.Sum(a => a.Balance),
        accounts.Count > 0 ? accounts.Average(a => a.Balance) : 0
    );
}
```

## Observable Return Types

For real-time queries, return `ISubject<T>` or `IObservable<T>`:

```csharp
[HttpGet("observable")]
public ISubject<IEnumerable<DebitAccount>> GetAccountsObservable()
{
    return _collection.Observe();
}
```

See [Observable Queries](observable-queries.md) for more details on real-time data streaming.

## Nullable Return Types

When a query might not return data, use nullable types:

```csharp
[HttpGet("{id}")]
public DebitAccount? GetAccount(AccountId id)
{
    return _collection.Find(a => a.Id == id).FirstOrDefault();
}
```

## Best Practices

1. **Use appropriate collection types** - `IEnumerable<T>` for most cases, `List<T>` when you need specific list operations
2. **Consider nullability** - Use nullable types when queries might return no results
3. **Async for I/O operations** - Always use async methods when dealing with database operations
4. **Custom types for complex data** - Create dedicated response types for complex query results
5. **QueryResult for metadata** - Use `QueryResult<T>` when you need to include additional response metadata

## Response Wrappers

By default, controller-based queries wrap results in a `QueryResult` structure. To bypass this wrapper and return raw results, use the `[AspNetResult]` attribute. For more details, see [Without wrappers](../../asp-net-core/without-wrappers.md).
