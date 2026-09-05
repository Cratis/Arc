# Observable Queries

Observable queries provide real-time data streaming using WebSockets, enabling reactive user experiences where data changes are pushed to clients as they occur. You achieve this by returning `ISubject<T>` from your controller actions.

The `ISubject<T>` return type automatically establishes a WebSocket connection between the server and client, enabling real-time data updates. This integrates seamlessly with the [ObservableQuery construct in the frontend](../../../frontend/react/queries/observable-queries.md) through the proxy generator, creating strongly-typed reactive data flows.

## Basic Observable Query

The key to an observable query is to return the `ISubject<T>` produced by the MongoDB
`Observe()` extension method. `Observe()` watches the collection and pushes a fresh
snapshot every time the data changes — Arc establishes and manages the WebSocket
connection for you. The examples below use `_collection`, an injected
`IMongoCollection<DebitAccount>` field on the controller.

```csharp
[HttpGet("observable")]
public ISubject<IEnumerable<DebitAccount>> AllAccountsObservable()
{
    return _collection.Observe();
}
```

## Observable with Arguments

Observable queries can accept arguments just like regular queries:

```csharp
[HttpGet("owner/{ownerId}/observable")]
public ISubject<IEnumerable<DebitAccount>> GetAccountsByOwnerObservable(CustomerId ownerId)
{
    return _collection.Observe(account => account.Owner == ownerId);
}

[HttpGet("filtered-observable")]
public ISubject<IEnumerable<DebitAccount>> GetFilteredAccountsObservable(
    [FromQuery] decimal minBalance = 0)
{
    return _collection.Observe(account => account.Balance >= minBalance);
}
```

## Single Object Observable

For observing changes to a single object:

```csharp
[HttpGet("{id}/observable")]
public ISubject<DebitAccount> GetAccountObservable(AccountId id)
{
    return _collection.ObserveSingle(account => account.Id == id);
}
```

## Custom Observable Logic

For computed or derived data, build on top of the collection's observable. `Observe()`
returns an `IObservable<IEnumerable<T>>` that emits a new snapshot whenever the data
changes, so you can use System.Reactive operators such as `Select` to project each
snapshot into a computed shape:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

[HttpGet("summary")]
public ISubject<AccountSummary> GetAccountSummaryObservable()
{
    var summary = new ReplaySubject<AccountSummary>(1);

    _collection.Observe()
        .Select(accounts => new AccountSummary(accounts.Count(), accounts.Sum(a => a.Balance)))
        .Subscribe(summary);

    return summary;
}
```

## Multiple Data Source Observables

Observe changes across multiple data sources:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

public record CombinedData(IEnumerable<DebitAccount> Accounts, IEnumerable<Customer> Customers);

[HttpGet("combined-observable")]
public ISubject<CombinedData> GetCombinedDataObservable()
{
    var combined = new ReplaySubject<CombinedData>(1);

    _accountCollection.Observe()
        .CombineLatest(_customerCollection.Observe(),
            (accounts, customers) => new CombinedData(accounts, customers))
        .Subscribe(combined);

    return combined;
}
```

## Observable with Computed Values

Create observables that compute derived values:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

[HttpGet("computed-metrics")]
public ISubject<AccountMetrics> GetAccountMetricsObservable()
{
    var metrics = new ReplaySubject<AccountMetrics>(1);

    _collection.Observe()
        .Select(accounts => new AccountMetrics(
            TotalAccounts: accounts.Count(),
            TotalBalance: accounts.Sum(a => a.Balance),
            AverageBalance: accounts.Any() ? accounts.Average(a => a.Balance) : 0,
            ActiveAccounts: accounts.Count(a => a.Balance > 0),
            HighValueAccounts: accounts.Count(a => a.Balance > 100000)))
        .Subscribe(metrics);

    return metrics;
}
```

## Filtered Observables with Dynamic Criteria

Allow clients to specify filter criteria for observables:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

public record ObservableFilter(
    decimal? MinBalance,
    decimal? MaxBalance,
    string? NamePattern,
    CustomerId? OwnerId);

[HttpPost("filtered-observable")]
public ISubject<IEnumerable<DebitAccount>> GetFilteredObservable([FromBody] ObservableFilter filter)
{
    var predicate = BuildFilterPredicate(filter);
    var filtered = new ReplaySubject<IEnumerable<DebitAccount>>(1);

    _collection.Observe()
        .Select(accounts => accounts.Where(predicate).ToList().AsEnumerable())
        .Subscribe(filtered);

    return filtered;
}

Func<DebitAccount, bool> BuildFilterPredicate(ObservableFilter filter)
{
    return account =>
        (!filter.MinBalance.HasValue || account.Balance >= filter.MinBalance.Value) &&
        (!filter.MaxBalance.HasValue || account.Balance <= filter.MaxBalance.Value) &&
        (string.IsNullOrEmpty(filter.NamePattern) || account.Name.Contains(filter.NamePattern, StringComparison.OrdinalIgnoreCase)) &&
        (!filter.OwnerId.HasValue || account.Owner == filter.OwnerId.Value);
}
```

## Throttled Observables

For high-frequency changes, use the System.Reactive `Sample` operator to emit at most one
update per time window, preventing a flood of changes from overwhelming clients:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

[HttpGet("throttled-observable")]
public ISubject<IEnumerable<DebitAccount>> GetThrottledObservable(
    [FromQuery] int throttleMs = 1000)
{
    var throttled = new ReplaySubject<IEnumerable<DebitAccount>>(1);

    _collection.Observe()
        .Sample(TimeSpan.FromMilliseconds(throttleMs))
        .Subscribe(throttled);

    return throttled;
}
```

## Error Handling in Observables

Errors raised while observing the collection propagate through the observable's error
channel automatically. Use the System.Reactive `Do` operator to log them as they flow
through, without altering the stream:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

[HttpGet("robust-observable")]
public ISubject<IEnumerable<DebitAccount>> GetRobustObservable()
{
    var observable = new ReplaySubject<IEnumerable<DebitAccount>>(1);

    _collection.Observe()
        .Do(
            onNext: _ => { },
            onError: ex => _logger.LogError(ex, "Error observing accounts"))
        .Subscribe(observable);

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

1. **Prefer the `Observe()` / `ObserveSingle()` extension methods** — they handle change monitoring, initial data, and cleanup for you
2. **Project with System.Reactive operators** (`Select`, `CombineLatest`, `Sample`) when you need computed, combined, or throttled streams
3. **Use appropriate filters** to minimize unnecessary data transmission
4. **Consider throttling** with `Sample` for high-frequency changes to prevent overwhelming clients
5. **Let errors propagate** through the observable's error channel; use `Do` to observe them for logging
6. **Use authentication** to control who can subscribe to observable endpoints
7. **Monitor performance** and consider the impact of many concurrent subscriptions

## Connection Management

Arc automatically handles WebSocket connections for observable queries:

- **Connection establishment** - Automatic WebSocket upgrade for observable endpoints
- **Message serialization** - Automatic JSON serialization of observable data
- **Connection cleanup** - Proper disposal of resources when clients disconnect
- **Reconnection handling** - Clients can reconnect and resume subscriptions

## Waiting for the First HTTP Result

The regular HTTP `GET` endpoint for a controller-based observable query returns the current snapshot as JSON. If the observable has not produced a value yet, add `waitForFirstResult=true` to keep the HTTP request open until the first item arrives.

Arc applies a timeout while waiting. By default the timeout is 30 seconds. You can override it with `waitForFirstResultTimeout`, expressed in seconds.

```bash
curl "https://localhost:5001/api/observable-controller-queries/observe/delayed-single?waitForFirstResult=true"
```

```bash
curl "https://localhost:5001/api/observable-controller-queries/observe/delayed-single?waitForFirstResult=true&waitForFirstResultTimeout=10"
```

This makes it easy to debug observable controller actions with cURL without switching to WebSockets or SSE.

See [Use Observable Queries with cURL](../using-observable-queries-with-curl.md) for snapshot, SSE, and long-polling workflows.

## Frontend Integration

Observable queries integrate seamlessly with frontend frameworks through the proxy generator and the [ObservableQuery construct](../../../frontend/react/queries/observable-queries.md):

```typescript

accountsObservable.subscribe(accounts => {
    // Handle real-time account updates
    updateUI(accounts);
});
```

The `ISubject<T>` return type automatically establishes and manages WebSocket connections, providing:

> **Important**: The `Observe()` and `ObserveSingle()` extension methods manage their own subscriptions and clean up automatically when a client disconnects, so you do not need to write any teardown code.

- **Automatic connection management** - WebSocket connections are established and maintained automatically
- **Strongly-typed data flow** - Full TypeScript support through the proxy generator
- **Reactive integration** - Seamless integration with React hooks like `useObservableQuery()`
- **Reconnection handling** - Automatic reconnection and state recovery on connection loss

> **Note**: The [proxy generator](../../proxy-generation/index.md) automatically creates TypeScript types for your observable queries,
> making them strongly typed on the frontend as well.
