---
title: Observe Entity Framework Core query results
description: Use Cratis Arc to refresh Entity Framework Core query results after changes, with provider-specific notifications and an owned observation lifetime.
---

Cratis Arc's Entity Framework Core observation refreshes query results when it detects changes to the underlying table. Use it after ordinary EF reads work, when clients need updated results without requesting each refresh. Arc detects writes through its SaveChanges interceptor and, where configured, database notifications; SQLite does not detect external writes.

## Configure the host

`WithEntityFrameworkCore()` registers observation services. For explicit registration in an existing Arc host, import `Cratis.Arc.EntityFrameworkCore`, `Cratis.Arc.EntityFrameworkCore.Observe`, `Microsoft.EntityFrameworkCore`, and `Microsoft.Extensions.DependencyInjection`, then use this startup fragment:

```csharp
services.AddEntityFrameworkCoreObservation();
services.AddDbContextWithConnectionString<OrdersDbContext>("Data Source=orders.db");
```

Arc's context helpers add the observation interceptor when those services are available. With ordinary EF registration, add `.AddObservation(serviceProvider)` in the options callback that receives both the service provider and options builder. Register the context's concrete type so observation can resolve it in a fresh dependency-injection scope for the initial query and each refresh.

Keep normal Arc registration and activation in the host. `Observe()` uses Arc's initialized service provider and current query context, which carries client paging and sorting. Registering EF services alone is not enough.

## Observe results

These are query-method fragments using your application's context/entities. Here `Customer.Id` and `customerId` use the application's `CustomerId : ConceptAs<Guid>` domain identity. Import `Microsoft.EntityFrameworkCore` and `System.Reactive.Linq`.

```csharp
var orders = dbContext.Orders.Observe(order => order.IsPending);
var customer = dbContext.Customers.ObserveSingle(customer => customer.Email == email);
var byId = dbContext.Customers.ObserveById<Customer, CustomerId>(customerId);
```

`Observe()` returns `ISubject<IEnumerable<TEntity>>`, a Reactive Extensions (Rx) subject that publishes the current collection. `ObserveSingle()` and `ObserveById()` return `ISubject<TEntity>`. When no entity matches, the single-entity methods emit `null` and keep the subscription open. This applies to the initial read, a delete, or an update that moves the entity out of the filter. A subscriber joining later receives the current value, including `null`. Treat `null` as "not found" on the client.

`ObserveById()` filters on a mapped public CLR `Id` property when one exists; otherwise it uses the entity's observation key. Observation uses a single, non-shadow EF primary key for identity, falling back to a mapped public CLR `Id` for composite, keyless, or shadow-key entities. Shadow-only keys without a usable CLR `Id` fail at setup. Indexer primary keys are supported on named shared-type sets such as `dbContext.Set<Dictionary<string, object?>>("PropertyBag")`.

To customize loading, use the second callback:

```csharp
var orders = dbContext.Orders.Observe(
    order => order.IsPending,
    configure: set => set.Include(order => order.Lines));
```

Arc applies the filter and current query-context paging and sorting to the configured query, including sorting by mapped indexer properties. The initial query runs synchronously and can throw before a subject is returned. Later query failures are logged and sent to subscribers through `OnError`, ending the observation and initiating cleanup. Cancellation and disposal exceptions during teardown do not fault the subject.

### Compose a primary observation with auxiliary sources

When combining observations from several DbSets, choose which source owns client paging, sorting, and the total count. Leave that primary observation unchanged. On each supporting observation, pass `ignoreQueryContext: true` to read all entities matching its filter without client paging or sorting. This also prevents the supporting source from writing the query's `TotalItems` at startup or on changes. Without the option, every observation uses the client query context:

```csharp
var primary = dbContext.Orders.Observe();
var auxiliary = dbContext.Customers.Observe(ignoreQueryContext: true);
var selected = dbContext.Customers.ObserveSingle(customer => customer.Email == email, ignoreQueryContext: true);
var byId = dbContext.Customers.ObserveById<Customer, CustomerId>(customerId, ignoreQueryContext: true);
```

These fragments use the `dbContext`, `email`, and `customerId` from the earlier examples. The option works on filtered and filterless `Observe` and `ObserveSingle` calls, and on `ObserveById`; you can still supply `configure`.

`TotalItems` describes the primary DbSet's filtered count. It does not count an arbitrary result derived by combining sources. If combining them changes how the result should be paged, compute that result's count explicitly.

## Provider capabilities

| Provider | In-process writes | External writes and prerequisites |
| --- | --- | --- |
| SQLite | SaveChanges through contexts with Arc's observation interceptor | No external notifier; `StartListening` is a no-op. No polling fallback. |
| PostgreSQL | Same interception path | `LISTEN/NOTIFY`; Arc attempts to create a function and table trigger. The database user needs suitable permissions, or an administrator must supply a compatible trigger/channel. |
| SQL Server | Same interception path | `SqlDependency` with Service Broker enabled and query-notification permissions/compatible queries. Arc attempts to enable Service Broker. No general polling fallback. |

:::danger[Enabling Service Broker can terminate connections]
When Service Broker is disabled, Arc attempts `ALTER DATABASE [databaseName] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE`. With sufficient permissions, this operation can terminate other connections and roll back their transactions. Pre-provision Service Broker through a controlled administrative maintenance/deployment procedure and use least-privilege application credentials. Do not grant `ALTER DATABASE` merely to make `Observe()` initialize.
:::

A PostgreSQL listener can start even when trigger creation fails. That alone does not establish that external changes are observable. Notifier setup failures are logged and observation can continue with in-process notifications only. Monitor logs and verify an external write in a test database before relying on cross-process updates.

Notifications cause a query refresh; they are not a durable change log. Do not assume every intermediate state is delivered, that raw/bulk SQL passes through SaveChanges interception, or that changes to every included relationship independently trigger the root-table observation.

## Lifetime and disposal

An observation owns callbacks and database-notifier resources beyond an individual subscriber. Arc's streaming transports dispose their subscriptions when clients disconnect, but they do not complete the EF subject. Returning a raw EF subject therefore does not release those resources on disconnect. An HTTP snapshot, including `waitForFirstResult=true`, does not change that ownership.

Before exposing EF observation through an observable query, establish an application-owned producer lifetime and test disconnects, concurrent clients, and shutdown. Do not complete a shared subject just because one client leaves: that would stop other clients too.

For a subject you own directly, retain both the subject and subscription, then complete the subject when finished. Disposing the Rx subscription alone does not stop EF observation. The following fragment belongs inside an asynchronous method:

```csharp
var subject = dbContext.Orders.Observe(order => order.IsPending);
using var subscription = subject.Subscribe(orders => Console.WriteLine(orders.Count()));
try
{
    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
}
finally
{
    subject.OnCompleted();
}
```

Completion initiates notifier/interceptor cleanup; it is not an awaitable guarantee that notifier disposal has finished. Test cleanup against concurrent notifications with your provider. This lifecycle differs from [MongoDB collection observation](../mongodb/observing-collections.md), whose subject tracks subscriber disposal.

## Scope and isolation

Observation creates fresh scopes for reads and captures the subscribing tenant and connection purpose. It restores that context when a notification causes a refresh. When connection resolution is configured, Arc's context registrations add an interceptor that resolves the connection for the current tenant and purpose each time it opens, including pooled reuse.

This does not by itself establish tenant isolation for every notification path. Database notifiers use the connection string captured from the observing context, and in-process callbacks are registered by table name and schema. Verify your tenant strategy across initial reads, external notifications, callbacks from other tenants, and pooled reuse.

Use [read-only context registration](./read-only.md) if the observed context is for queries only. For the host and provider setup used here, return to [Entity Framework Core registration](./getting-started.md).
