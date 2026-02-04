# Observable Queries

Observable queries provide real-time data streaming using WebSockets, enabling reactive user experiences where data changes are pushed to clients as they occur. You achieve this by returning `ISubject<T>` from your controller actions.

The `ISubject<T>` return type automatically establishes a WebSocket connection between the server and client, enabling real-time data updates. This integrates seamlessly with the [ObservableQuery construct in the frontend](../../../frontend/react/queries.md) through the proxy generator, creating strongly-typed reactive data flows.

## Basic Observable Query

The key to an observable query is to leverage the `ClientObservable<T>` generic type.

```csharp
[HttpGet("observable")]
public ISubject<IEnumerable<DebitAccount>> AllAccountsObservable()
{
    var observable = new ClientObservable<IEnumerable<DebitAccount>>();
    
    // Send initial data
    var accounts = GetCurrentAccounts();
    observable.OnNext(accounts);
    
    // Set up real-time updates (implementation varies by data source)
    SetupDataChangeNotifications(observable);
    
    return observable;
}
```

## Observable with Arguments

Observable queries can accept arguments just like regular queries:

```csharp
[HttpGet("owner/{ownerId}/observable")]
public ISubject<IEnumerable<DebitAccount>> GetAccountsByOwnerObservable(CustomerId ownerId)
{
    var observable = new ClientObservable<IEnumerable<DebitAccount>>();
    
    // Send initial filtered data
    var accounts = GetAccountsByOwner(ownerId);
    observable.OnNext(accounts);
    
    // Set up filtered change notifications
    SetupFilteredDataChangeNotifications(observable, ownerId);
    
    return observable;
}

[HttpGet("filtered-observable")]
public ISubject<IEnumerable<DebitAccount>> GetFilteredAccountsObservable(
    [FromQuery] decimal? minBalance = null)
{
    var observable = new ClientObservable<IEnumerable<DebitAccount>>();
    
    // Send initial filtered data
    var accounts = minBalance.HasValue 
        ? GetAccountsAboveBalance(minBalance.Value)
        : GetAllAccounts();
    observable.OnNext(accounts);
    
    // Set up change notifications with filter
    SetupFilteredDataChangeNotifications(observable, minBalance);
    
    return observable;
}
```

## Single Object Observable

For observing changes to a single object:

```csharp
[HttpGet("{id}/observable")]
public ISubject<DebitAccount> GetAccountObservable(AccountId id)
{
    var observable = new ClientObservable<DebitAccount>();
    
    // Send initial object
    var account = GetAccountById(id);
    if (account is not null)
    {
        observable.OnNext(account);
    }
    
    // Set up change notifications for this specific object
    SetupSingleObjectChangeNotifications(observable, id);
    
    return observable;
}
```

## Custom Observable Logic

For more complex scenarios, you can implement custom observable logic using `ClientObservable<T>`:

```csharp
[HttpGet("summary")]
public ISubject<AccountSummary> GetAccountSummaryObservable()
{
    var observable = new ClientObservable<AccountSummary>();
    
    var calculateSummary = () =>
    {
        var accounts = GetAllAccounts();
        return new AccountSummary(accounts.Count(), accounts.Sum(a => a.Balance));
    };

    // Send initial summary
    observable.OnNext(calculateSummary());

    // Set up change notifications for computed data
    // Implementation depends on your data source and change notification mechanism
    var changeToken = SetupDataChangeNotifications(() =>
    {
        observable.OnNext(calculateSummary());
    });

    // Clean up when client disconnects
    observable.ClientDisconnected = () => changeToken?.Dispose();
    
    return observable;
}
```

## Multiple Data Source Observables

Observe changes across multiple data sources:

```csharp
public record CombinedData(IEnumerable<DebitAccount> Accounts, IEnumerable<Customer> Customers);

[HttpGet("combined-observable")]
public ISubject<CombinedData> GetCombinedDataObservable()
{
    var observable = new ClientObservable<CombinedData>();
    
    var sendUpdate = () =>
    {
        var accounts = GetAllAccounts();
        var customers = GetAllCustomers();
        observable.OnNext(new CombinedData(accounts, customers));
    };

    // Send initial data
    sendUpdate();

    // Set up change notifications for multiple data sources
    var accountChangeToken = SetupAccountChangeNotifications(sendUpdate);
    var customerChangeToken = SetupCustomerChangeNotifications(sendUpdate);

    observable.ClientDisconnected = () =>
    {
        accountChangeToken?.Dispose();
        customerChangeToken?.Dispose();
    };
    
    return observable;
}
```

## Observable with Computed Values

Create observables that compute derived values:

```csharp
[HttpGet("computed-metrics")]
public ISubject<AccountMetrics> GetAccountMetricsObservable()
{
    var observable = new ClientObservable<AccountMetrics>();
    
    var computeMetrics = () =>
    {
        var accounts = GetAllAccounts();
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
    var changeToken = SetupDataChangeNotifications(() =>
    {
        observable.OnNext(computeMetrics());
    });

    observable.ClientDisconnected = () => changeToken?.Dispose();
    return observable;
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

[HttpPost("filtered-observable")]
public ISubject<IEnumerable<DebitAccount>> GetFilteredObservable([FromBody] ObservableFilter filter)
{
    var observable = new ClientObservable<IEnumerable<DebitAccount>>();
    
    // Build filter predicate
    var predicate = BuildFilterPredicate(filter);
    
    var sendFilteredData = () =>
    {
        var accounts = GetAllAccounts().Where(predicate);
        observable.OnNext(accounts);
    };

    // Send initial filtered data
    sendFilteredData();

    // Set up change notifications with the same filter
    var changeToken = SetupFilteredDataChangeNotifications(sendFilteredData, filter);

    observable.ClientDisconnected = () => changeToken?.Dispose();
    return observable;
}

private Func<DebitAccount, bool> BuildFilterPredicate(ObservableFilter filter)
{
    return account =>
        (!filter.MinBalance.HasValue || account.Balance >= filter.MinBalance.Value) &&
        (!filter.MaxBalance.HasValue || account.Balance <= filter.MaxBalance.Value) &&
        (string.IsNullOrEmpty(filter.NamePattern) || account.Name.Contains(filter.NamePattern, StringComparison.OrdinalIgnoreCase)) &&
        (!filter.OwnerId.HasValue || account.Owner == filter.OwnerId.Value);
}
```

## Throttled Observables

For high-frequency changes, implement throttling to prevent overwhelming clients:

```csharp
[HttpGet("throttled-observable")]
public ISubject<IEnumerable<DebitAccount>> GetThrottledObservable(
    [FromQuery] int throttleMs = 1000)
{
    var observable = new ClientObservable<IEnumerable<DebitAccount>>();
    var throttleTimer = new System.Timers.Timer(throttleMs);
    var pendingUpdate = false;

    var sendUpdate = () =>
    {
        var accounts = GetAllAccounts();
        observable.OnNext(accounts);
        pendingUpdate = false;
    };

    // Send initial data
    sendUpdate();

    // Set up throttled change notifications
    var changeToken = SetupDataChangeNotifications(() =>
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
```

## Error Handling in Observables

Implement proper error handling for robust observables:

```csharp
[HttpGet("robust-observable")]
public ISubject<IEnumerable<DebitAccount>> GetRobustObservable()
{
    var observable = new ClientObservable<IEnumerable<DebitAccount>>();
    
    try
    {
        var sendUpdate = () =>
        {
            try
            {
                var accounts = GetAllAccounts();
                observable.OnNext(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating observable accounts");
                observable.OnError(ex);
            }
        };

        // Send initial data
        sendUpdate();

        // Set up change notifications with error handling
        var changeToken = SetupDataChangeNotifications(() =>
        {
            try
            {
                sendUpdate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in observable change notification");
                observable.OnError(ex);
            }
        });

        observable.ClientDisconnected = () => changeToken?.Dispose();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error initializing observable");
        observable.OnError(ex);
    }

    return observable;
}
```

## Authentication and Authorization

Observable queries support the same authentication and authorization as regular queries:

```csharp
[Authorize]
[HttpGet("secure-observable")]
public ISubject<IEnumerable<DebitAccount>> GetSecureObservable()
{
    // Only authenticated users can subscribe
    return _collection.Observe();
}

[Authorize(Roles = "Admin")]
[HttpGet("admin-observable")]
public ISubject<IEnumerable<DebitAccount>> GetAdminObservable()
{
    // Only admin users can subscribe
    return _collection.Observe();
}
```

## Best Practices for Observable Queries

1. **Always handle client disconnection** with the `ClientDisconnected` callback when using `ClientObservable<T>` directly
2. **Send initial data immediately** before setting up change monitoring
3. **Use appropriate filters** to minimize unnecessary data transmission
4. **Consider throttling** for high-frequency changes to prevent overwhelming clients
5. **Implement error handling** to gracefully handle data source connection issues
6. **Clean up resources** properly when clients disconnect
7. **Use authentication** to control who can subscribe to observable endpoints
8. **Monitor performance** and consider the impact of many concurrent subscriptions

## Connection Management

Arc automatically handles WebSocket connections for observable queries:

- **Connection establishment** - Automatic WebSocket upgrade for observable endpoints
- **Message serialization** - Automatic JSON serialization of observable data
- **Connection cleanup** - Proper disposal of resources when clients disconnect
- **Reconnection handling** - Clients can reconnect and resume subscriptions

## Frontend Integration

Observable queries integrate seamlessly with frontend frameworks through the proxy generator and the [ObservableQuery construct](../../../frontend/react/queries.md):

```typescript
// Generated TypeScript proxy automatically handles WebSocket connections
const accountsObservable = await accountsProxy.getAllAccountsObservable();

accountsObservable.subscribe(accounts => {
    // Handle real-time account updates
    updateUI(accounts);
});
```

The `ISubject<T>` return type automatically establishes and manages WebSocket connections, providing:

- **Automatic connection management** - WebSocket connections are established and maintained automatically
- **Strongly-typed data flow** - Full TypeScript support through the proxy generator
- **Reactive integration** - Seamless integration with React hooks like `useObservableQuery()`
- **Reconnection handling** - Automatic reconnection and state recovery on connection loss

> **Important**: When using `ClientObservable<T>` directly, the `ClientDisconnected` callback is essential for cleaning up resources to prevent memory leaks.

> **Note**: The [proxy generator](../../proxy-generation/index.md) automatically creates TypeScript types for your observable queries,
> making them strongly typed on the frontend as well.
