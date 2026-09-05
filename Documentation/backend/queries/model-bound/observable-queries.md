# Observable Queries

Observable queries in model-bound scenarios provide the same real-time data streaming capabilities as controller-based queries, but implemented as static methods directly on your read model records. You achieve this by returning `ISubject<T>` from static methods on your `[ReadModel]` decorated record.

The `ISubject<T>` return type automatically establishes a WebSocket connection between the server and client, enabling real-time data updates. This integrates seamlessly with the [ObservableQuery construct in the frontend](../../../frontend/react/queries/observable-queries.md) through the proxy generator, creating strongly-typed reactive data flows.

## Basic Observable Query

Define an observable query as a static method on your read model and return the
`ISubject<T>` produced by the MongoDB `Observe()` extension method. `Observe()` watches
the collection and pushes a fresh snapshot every time the underlying data changes — Arc
handles the WebSocket connection and the change monitoring for you:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetAllAccountsObservable(IMongoCollection<DebitAccount> collection)
    {
        return collection.Observe();
    }

    public static IEnumerable<DebitAccount> GetAllAccounts(IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(_ => true).ToList();
    }
}
```

## Observable with Arguments

Observable queries can accept arguments just like regular queries. Pass a filter
expression to `Observe()` and the observable only streams the matching documents:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetAccountsByOwnerObservable(
        CustomerId ownerId,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Observe(account => account.Owner == ownerId);
    }

    public static ISubject<IEnumerable<DebitAccount>> GetFilteredAccountsObservable(
        IMongoCollection<DebitAccount> collection,
        decimal minBalance = 0)
    {
        return collection.Observe(account => account.Balance >= minBalance);
    }
}
```

## Single Object Observable

For observing changes to a single object, return `ISubject<T>` (not a collection) and
use `ObserveSingle()`, which streams the latest version of the one matching document:

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<DebitAccount> GetAccountObservable(
        AccountId id,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.ObserveSingle(account => account.Id == id);
    }
}
```

## Custom Observable Logic

For computed or derived data, build on top of the collection's observable. `Observe()`
returns an `IObservable<IEnumerable<T>>` that emits a new snapshot whenever the data
changes, so you can use System.Reactive operators such as `Select` to project each
snapshot into a computed shape. The result is itself an `ISubject<T>` via
`ReplaySubject<T>`, so subscribers always receive the most recent computed value:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

public record AccountSummary(int Count, decimal TotalBalance);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<AccountSummary> GetAccountSummaryObservable(IMongoCollection<DebitAccount> collection)
    {
        var summary = new ReplaySubject<AccountSummary>(1);

        collection.Observe()
            .Select(accounts => new AccountSummary(accounts.Count(), accounts.Sum(a => a.Balance)))
            .Subscribe(summary);

        return summary;
    }
}
```

## Multiple Data Source Observables

Observe changes across multiple data sources by injecting multiple collections and
combining their observables with `CombineLatest`. The combined observable re-emits
whenever *either* source changes:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

public record CombinedData(IEnumerable<DebitAccount> Accounts, IEnumerable<Customer> Customers);

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<CombinedData> GetCombinedDataObservable(
        IMongoCollection<DebitAccount> accountCollection,
        IMongoCollection<Customer> customerCollection)
    {
        var combined = new ReplaySubject<CombinedData>(1);

        accountCollection.Observe()
            .CombineLatest(customerCollection.Observe(),
                (accounts, customers) => new CombinedData(accounts, customers))
            .Subscribe(combined);

        return combined;
    }
}
```

## Observable with Computed Values

Create observables that compute derived values by projecting each snapshot with
`Select`. Every change to the underlying collection recomputes and re-emits the metrics:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

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
        var metrics = new ReplaySubject<AccountMetrics>(1);

        collection.Observe()
            .Select(accounts => new AccountMetrics(
                TotalAccounts: accounts.Count(),
                TotalBalance: accounts.Sum(a => a.Balance),
                AverageBalance: accounts.Any() ? accounts.Average(a => a.Balance) : 0,
                ActiveAccounts: accounts.Count(a => a.Balance > 0),
                HighValueAccounts: accounts.Count(a => a.Balance > 100000)))
            .Subscribe(metrics);

        return metrics;
    }
}
```

## Filtered Observables with Dynamic Criteria

Allow clients to specify filter criteria for observables. Observe the whole collection
and apply the dynamic predicate to each emitted snapshot with `Select`, so the filtered
results stay current as the data changes:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

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
        var predicate = BuildFilterPredicate(filter);
        var filtered = new ReplaySubject<IEnumerable<DebitAccount>>(1);

        collection.Observe()
            .Select(accounts => accounts.Where(predicate).ToList().AsEnumerable())
            .Subscribe(filtered);

        return filtered;
    }

    static Func<DebitAccount, bool> BuildFilterPredicate(ObservableFilter filter)
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

For high-frequency changes, use the System.Reactive `Sample` operator to emit at most one
update per time window, preventing a flood of changes from overwhelming clients:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetThrottledObservable(
        IMongoCollection<DebitAccount> collection,
        int throttleMs = 1000)
    {
        var throttled = new ReplaySubject<IEnumerable<DebitAccount>>(1);

        collection.Observe()
            .Sample(TimeSpan.FromMilliseconds(throttleMs))
            .Subscribe(throttled);

        return throttled;
    }
}
```

## Error Handling in Observables

Errors raised while observing the collection propagate through the observable's error
channel automatically. Use the System.Reactive `Do` operator to log them as they flow
through, without altering the stream:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetRobustObservable(
        IMongoCollection<DebitAccount> collection,
        ILogger<DebitAccount> logger)
    {
        var observable = new ReplaySubject<IEnumerable<DebitAccount>>(1);

        collection.Observe()
            .Do(
                onNext: _ => { },
                onError: ex => logger.LogError(ex, "Error observing accounts"))
            .Subscribe(observable);

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
        return collection.Observe();
    }

    [Authorize(Roles = "Admin")]
    public static ISubject<IEnumerable<DebitAccount>> GetAdminObservable(IMongoCollection<DebitAccount> collection)
    {
        // Only admin users can subscribe
        return collection.Observe();
    }
}
```

## Dependency Injection

Model-bound observable queries support the same dependency injection patterns as regular model-bound queries:

```csharp
using System.Reactive.Linq;
using System.Reactive.Subjects;

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static ISubject<IEnumerable<DebitAccount>> GetAccountsWithBusinessLogic(
        IMongoCollection<DebitAccount> collection,
        IAccountValidator validator)
    {
        var valid = new ReplaySubject<IEnumerable<DebitAccount>>(1);

        collection.Observe()
            .Select(accounts => accounts.Where(validator.IsValid).ToList().AsEnumerable())
            .Subscribe(valid);

        return valid;
    }
}
```

## Best Practices for Model-Bound Observable Queries

1. **Prefer the `Observe()` / `ObserveSingle()` extension methods** — they handle change monitoring, initial data, and cleanup for you
2. **Project with System.Reactive operators** (`Select`, `CombineLatest`, `Sample`) when you need computed, combined, or throttled streams
3. **Use appropriate filters** to minimize unnecessary data transmission
4. **Consider throttling** with `Sample` for high-frequency changes to prevent overwhelming clients
5. **Let errors propagate** through the observable's error channel; use `Do` to observe them for logging
6. **Use authentication** to control who can subscribe to observable endpoints
7. **Monitor performance** and consider the impact of many concurrent subscriptions
8. **Keep observable methods static** and follow the same patterns as regular model-bound queries

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

## Waiting for the First HTTP Result

The generated HTTP `GET` endpoint for an observable query returns the current snapshot as JSON. If the observable does not have a current value yet, add `waitForFirstResult=true` to keep the HTTP request open until the first item is produced.

Arc applies a timeout while waiting. By default the timeout is 30 seconds. You can override it with `waitForFirstResultTimeout`, expressed in seconds.

```bash
curl "https://localhost:5001/api/debit-account/observe-single?waitForFirstResult=true"
```

```bash
curl "https://localhost:5001/api/debit-account/observe-single?waitForFirstResult=true&waitForFirstResultTimeout=10"
```

This is useful when debugging observable queries with cURL or any other plain HTTP client and you want the request to block until the first result is available.

See [Use Observable Queries with cURL](../using-observable-queries-with-curl.md) for snapshot, SSE, and long-polling workflows.

## Connection Management

Arc automatically handles WebSocket connections for observable queries:

- **Connection establishment** - Automatic WebSocket upgrade for observable endpoints
- **Message serialization** - Automatic JSON serialization of observable data
- **Connection cleanup** - Proper disposal of resources when clients disconnect
- **Reconnection handling** - Clients can reconnect and resume subscriptions

The same connection management applies whether using controller-based or model-bound approaches.
