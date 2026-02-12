# Observable Queries

Observable queries in model-bound scenarios provide the same real-time data streaming capabilities as controller-based queries, but implemented as static methods directly on your read model records. You achieve this by returning `ISubject<T>` from static methods on your `[ReadModel]` decorated record.

The `ISubject<T>` return type automatically establishes a WebSocket connection between the server and client, enabling real-time data updates. This integrates seamlessly with the [ObservableQuery construct in the frontend](../../../frontend/react/queries.md) through the proxy generator, creating strongly-typed reactive data flows.

## Basic Observable Query

Define an observable query as a static method on your read model:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetAllAccountsObservable(IMongoCollection<DebitAccount> collection)
    {
        var observable = new ClientObservable<IEnumerable<DebitAccount>>();
        
        // Send initial data
        var accounts = GetAllAccounts(collection);
        observable.OnNext(accounts);
        
        // Set up real-time updates (implementation varies by data source)
        SetupDataChangeNotifications(observable, collection);
        
        return observable;
    }
    
    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(_ => true).ToList();
    }
}
```

## Observable with Arguments

Observable queries can accept arguments just like regular queries:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetAccountsByOwnerObservable(
        CustomerId ownerId,
        IMongoCollection<DebitAccount> collection)
    {
        var observable = new ClientObservable<IEnumerable<DebitAccount>>();
        
        // Send initial filtered data
        var accounts = GetAccountsByOwner(ownerId, collection);
        observable.OnNext(accounts);
        
        // Set up filtered change notifications
        SetupFilteredDataChangeNotifications(observable, collection, ownerId);
        
        return observable;
    }
    
    public static ISubject<IEnumerable<DebitAccount>> GetFilteredAccountsObservable(
        IMongoCollection<DebitAccount> collection,
        decimal? minBalance = null)
    {
        var observable = new ClientObservable<IEnumerable<DebitAccount>>();
        
        // Send initial filtered data
        var accounts = minBalance.HasValue 
            ? GetAccountsAboveBalance(minBalance.Value, collection)
            : GetAllAccounts(collection);
        observable.OnNext(accounts);
        
        // Set up change notifications with filter
        SetupFilteredDataChangeNotifications(observable, collection, minBalance);
        
        return observable;
    }
}
```

## Single Object Observable

For observing changes to a single object:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<DebitAccount> GetAccountObservable(
        AccountId id,
        IMongoCollection<DebitAccount> collection)
    {
        var observable = new ClientObservable<DebitAccount>();
        
        // Send initial object
        var account = GetAccountById(id, collection);
        if (account is not null)
        {
            observable.OnNext(account);
        }
        
        // Set up change notifications for this specific object
        SetupSingleObjectChangeNotifications(observable, collection, id);
        
        return observable;
    }
}
```

## Custom Observable Logic

For more complex scenarios, you can implement custom observable logic using `ClientObservable<T>`:

```csharp
public record AccountSummary(int Count, decimal TotalBalance);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<AccountSummary> GetAccountSummaryObservable(IMongoCollection<DebitAccount> collection)
    {
        var observable = new ClientObservable<AccountSummary>();
        
        var calculateSummary = () =>
        {
            var accounts = GetAllAccounts(collection);
            return new AccountSummary(accounts.Count(), accounts.Sum(a => a.Balance));
        };

        // Send initial summary
        observable.OnNext(calculateSummary());

        // Set up change notifications for computed data
        // Implementation depends on your data source and change notification mechanism
        var changeToken = SetupDataChangeNotifications(collection, () =>
        {
            observable.OnNext(calculateSummary());
        });

        // Clean up when client disconnects
        observable.ClientDisconnected = () => changeToken?.Dispose();
        
        return observable;
    }
}
```

## Multiple Data Source Observables

Observe changes across multiple data sources by injecting multiple collections:

```csharp
public record CombinedData(IEnumerable<DebitAccount> Accounts, IEnumerable<Customer> Customers);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<CombinedData> GetCombinedDataObservable(
        IMongoCollection<DebitAccount> accountCollection,
        IMongoCollection<Customer> customerCollection)
    {
        var observable = new ClientObservable<CombinedData>();
        
        var sendUpdate = () =>
        {
            var accounts = GetAllAccounts(accountCollection);
            var customers = GetAllCustomers(customerCollection);
            observable.OnNext(new CombinedData(accounts, customers));
        };

        // Send initial data
        sendUpdate();

        // Set up change notifications for multiple data sources
        var accountChangeToken = SetupAccountChangeNotifications(accountCollection, sendUpdate);
        var customerChangeToken = SetupCustomerChangeNotifications(customerCollection, sendUpdate);

        observable.ClientDisconnected = () =>
        {
            accountChangeToken?.Dispose();
            customerChangeToken?.Dispose();
        };
        
        return observable;
    }
}
```

## Observable with Computed Values

Create observables that compute derived values:

```csharp
public record AccountMetrics(
    int TotalAccounts,
    decimal TotalBalance,
    decimal AverageBalance,
    int ActiveAccounts,
    int HighValueAccounts);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<AccountMetrics> GetAccountMetricsObservable(IMongoCollection<DebitAccount> collection)
    {
        var observable = new ClientObservable<AccountMetrics>();
        
        var computeMetrics = () =>
        {
            var accounts = GetAllAccounts(collection);
            return new AccountMetrics(
                TotalAccounts: accounts.Count(),
                TotalBalance: accounts.Sum(a => a.Balance),
                AverageBalance: accounts.Any() ? accounts.Average(a => a.Balance) : 0,
                ActiveAccounts: accounts.Count(a => a.Balance > 0),
                HighValueAccounts: accounts.Count(a => a.Balance > 100000)
            );
        };

        // Send initial metrics
        observable.OnNext(computeMetrics());

        // Set up change notifications and recompute when data changes
        var changeToken = SetupDataChangeNotifications(collection, () =>
        {
            observable.OnNext(computeMetrics());
        });

        observable.ClientDisconnected = () => changeToken?.Dispose();
        return observable;
    }
}
```

## Filtered Observables with Dynamic Criteria

Allow clients to specify filter criteria for observables:

```csharp
public record ObservableFilter(
    decimal? MinBalance,
    decimal? MaxBalance,
    string? NamePattern,
    CustomerId? OwnerId);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetFilteredObservable(
        ObservableFilter filter,
        IMongoCollection<DebitAccount> collection)
    {
        var observable = new ClientObservable<IEnumerable<DebitAccount>>();
        
        // Build filter predicate
        var predicate = BuildFilterPredicate(filter);
        
        var sendFilteredData = () =>
        {
            var accounts = GetAllAccounts(collection).Where(predicate);
            observable.OnNext(accounts);
        };

        // Send initial filtered data
        sendFilteredData();

        // Set up change notifications with the same filter
        var changeToken = SetupFilteredDataChangeNotifications(collection, sendFilteredData, filter);

        observable.ClientDisconnected = () => changeToken?.Dispose();
        return observable;
    }

    private static Func<DebitAccount, bool> BuildFilterPredicate(ObservableFilter filter)
    {
        return account =>
            (!filter.MinBalance.HasValue || account.Balance >= filter.MinBalance.Value) &&
            (!filter.MaxBalance.HasValue || account.Balance <= filter.MaxBalance.Value) &&
            (string.IsNullOrEmpty(filter.NamePattern) || account.Name.Contains(filter.NamePattern, StringComparison.OrdinalIgnoreCase)) &&
            (!filter.OwnerId.HasValue || account.Owner == filter.OwnerId.Value);
    }
}
```

## Throttled Observables

For high-frequency changes, implement throttling to prevent overwhelming clients:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetThrottledObservable(
        IMongoCollection<DebitAccount> collection,
        int throttleMs = 1000)
    {
        var observable = new ClientObservable<IEnumerable<DebitAccount>>();
        var throttleTimer = new System.Timers.Timer(throttleMs);
        var pendingUpdate = false;

        var sendUpdate = () =>
        {
            var accounts = GetAllAccounts(collection);
            observable.OnNext(accounts);
            pendingUpdate = false;
        };

        // Send initial data
        sendUpdate();

        // Set up throttled change notifications
        var changeToken = SetupDataChangeNotifications(collection, () =>
        {
            if (!pendingUpdate)
            {
                pendingUpdate = true;
                throttleTimer.Elapsed += (s, e) => 
                {
                    sendUpdate();
                    throttleTimer.Stop();
                };
                throttleTimer.Start();
            }
        });

        observable.ClientDisconnected = () =>
        {
            changeToken?.Dispose();
            throttleTimer?.Dispose();
        };

        return observable;
    }
}
```

## Error Handling in Observables

Implement proper error handling for robust observables:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetRobustObservable(
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger)
    {
        var observable = new ClientObservable<IEnumerable<DebitAccount>>();
        
        try
        {
            var sendUpdate = () =>
            {
                try
                {
                    var accounts = GetAllAccounts(collection);
                    observable.OnNext(accounts);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating observable accounts");
                    observable.OnError(ex);
                }
            };

            // Send initial data
            sendUpdate();

            // Set up change notifications with error handling
            var changeToken = SetupDataChangeNotifications(collection, () =>
            {
                try
                {
                    sendUpdate();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in observable change notification");
                    observable.OnError(ex);
                }
            });

            observable.ClientDisconnected = () => changeToken?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing observable");
            observable.OnError(ex);
        }

        return observable;
    }
}
```

## Authentication and Authorization

Observable queries support the same authentication and authorization as regular queries:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    [Authorize]
    public static ISubject<IEnumerable<DebitAccount>> GetSecureObservable(IMongoCollection<DebitAccount> collection)
    {
        // Only authenticated users can subscribe
        var observable = new ClientObservable<IEnumerable<DebitAccount>>();
        // Implementation...
        return observable;
    }

    [Authorize(Roles = "Admin")]
    public static ISubject<IEnumerable<DebitAccount>> GetAdminObservable(IMongoCollection<DebitAccount> collection)
    {
        // Only admin users can subscribe
        var observable = new ClientObservable<IEnumerable<DebitAccount>>();
        // Implementation...
        return observable;
    }
}
```

## Dependency Injection

Model-bound observable queries support the same dependency injection patterns as regular model-bound queries:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetAccountsWithBusinessLogic(
        IMongoCollection<DebitAccount> collection,
        IAccountValidator validator,
        ILogger<DebitAccount> logger)
    {
        var observable = new ClientObservable<IEnumerable<DebitAccount>>();
        
        var sendValidAccounts = () =>
        {
            try
            {
                var accounts = GetAllAccounts(collection);
                var validAccounts = accounts.Where(validator.IsValid);
                observable.OnNext(validAccounts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting valid accounts");
                observable.OnError(ex);
            }
        };

        sendValidAccounts();
        
        var changeToken = SetupDataChangeNotifications(collection, sendValidAccounts);
        observable.ClientDisconnected = () => changeToken?.Dispose();
        
        return observable;
    }
}
```

## Best Practices for Model-Bound Observable Queries

1. **Always handle client disconnection** with the `ClientDisconnected` callback when using `ClientObservable<T>` directly
2. **Send initial data immediately** before setting up change monitoring
3. **Use appropriate filters** to minimize unnecessary data transmission
4. **Consider throttling** for high-frequency changes to prevent overwhelming clients
5. **Implement error handling** to gracefully handle data source connection issues
6. **Clean up resources** properly when clients disconnect
7. **Use authentication** to control who can subscribe to observable endpoints
8. **Monitor performance** and consider the impact of many concurrent subscriptions
9. **Keep observable methods static** and follow the same patterns as regular model-bound queries

## Frontend Integration

Observable queries integrate seamlessly with frontend frameworks through the proxy generator and the [ObservableQuery construct](../../../frontend/react/queries.md):

```typescript
// Generated TypeScript proxy automatically handles WebSocket connections
const accountsObservable = await DebitAccount.getAllAccountsObservable();

accountsObservable.subscribe(accounts => {
    // Handle real-time account updates
    updateUI(accounts);
});
```

The `ISubject<T>` return type automatically establishes and manages WebSocket connections, providing:

> **Important**: When using `ClientObservable<T>` directly, the `ClientDisconnected` callback is essential for cleaning up resources to prevent memory leaks.

- **Automatic connection management** - WebSocket connections are established and maintained automatically
- **Strongly-typed data flow** - Full TypeScript support through the proxy generator
- **Reactive integration** - Seamless integration with React hooks like `useObservableQuery()`
- **Reconnection handling** - Automatic reconnection and state recovery on connection loss

> **Note**: The [proxy generator](../../proxy-generation/index.md) automatically creates TypeScript types for your observable queries,
> making them strongly typed on the frontend as well.

## Connection Management

Arc automatically handles WebSocket connections for observable queries:

- **Connection establishment** - Automatic WebSocket upgrade for observable endpoints
- **Message serialization** - Automatic JSON serialization of observable data
- **Connection cleanup** - Proper disposal of resources when clients disconnect
- **Reconnection handling** - Clients can reconnect and resume subscriptions

The same connection management applies whether using controller-based or model-bound approaches.

