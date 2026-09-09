---
title: Observe a DbSet
description: Observe EF query results with explicit provider prerequisites and lifecycle limits.
---

Once ordinary EF reads work, observation can refresh a result when the underlying table changes. Arc combines in-process SaveChanges interception with optional provider notifications. **SQLite does not poll external writes.**

## Configure the host

`WithEntityFrameworkCore()` registers observation services. For explicit registration in an existing Arc host, import `Cratis.Arc.EntityFrameworkCore`, `Cratis.Arc.EntityFrameworkCore.Observe`, `Microsoft.EntityFrameworkCore`, and `Microsoft.Extensions.DependencyInjection`, then use this startup fragment:

```csharp
services.AddEntityFrameworkCoreObservation();
services.AddDbContextWithConnectionString<OrdersDbContext>("Data Source=orders.db");
```

Arc's context helpers add the observation interceptor when those services are available. For ordinary EF registration, add `.AddObservation(serviceProvider)` in the two-argument options callback. The context must also be resolvable from a fresh scope: observation resolves its concrete type for initial and subsequent queries.

Keep the normal Arc registration **and activation**. `Observe()` accesses Arc's initialized service provider and current query context; adding EF services alone is not a complete observation host.

## Observe results

These are query-method fragments using your application's context/entities. Here `Customer.Id` and `customerId` use the application's `CustomerId : ConceptAs<Guid>` domain identity. Import `Microsoft.EntityFrameworkCore` and `System.Reactive.Linq`.

```csharp
var orders = dbContext.Orders.Observe(order => order.IsPending);
var customer = dbContext.Customers.ObserveSingle(customer => customer.Email == email);
var byId = dbContext.Customers.ObserveById<Customer, CustomerId>(customerId);
```

`Observe()` returns `ISubject<IEnumerable<TEntity>>`. `ObserveSingle()` and `ObserveById()` return `ISubject<TEntity>` and emit a value only when an entity exists; absence is not a null notification. `ObserveById` requires a public `Id` property. Collection observation uses single-key EF metadata where available, falling back to `Id`.

To customize loading, use the second callback:

```csharp
var orders = dbContext.Orders.Observe(
    order => order.IsPending,
    configure: set => set.Include(order => order.Lines));
```

Arc applies the filter and current query-context paging/sorting to the configured query. Initial querying is synchronous and can throw before a subject is returned. Later re-query errors are logged, not sent as `OnError` to subscribers.

## Provider capabilities

| Provider | In-process writes | External writes and prerequisites |
| --- | --- | --- |
| SQLite | SaveChanges through contexts with Arc's observation interceptor | No external notifier; `StartListening` is a no-op. No polling fallback. |
| PostgreSQL | Same interception path | `LISTEN/NOTIFY`; Arc attempts to create a function and table trigger. The database user needs suitable permissions, or an administrator must supply a compatible trigger/channel. |
| SQL Server | Same interception path | `SqlDependency` with Service Broker enabled and query-notification permissions/compatible queries. Arc attempts to enable Service Broker. No general polling fallback. |

> [!WARNING]
> When Service Broker is disabled, Arc attempts `ALTER DATABASE [databaseName] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE`. With sufficient permissions, this operation can terminate other connections and roll back their transactions. Pre-provision Service Broker through a controlled administrative maintenance/deployment procedure and use least-privilege application credentials. Do not grant `ALTER DATABASE` merely to make `Observe()` initialize.

A PostgreSQL listener can start even when trigger creation fails. That alone does not establish that external changes are observable. Notifier setup failures are logged and observation can continue with in-process notifications only. Monitor logs and verify an external write in a test database before relying on cross-process updates.

Notifications cause a query refresh; they are not a durable change log. Do not assume every intermediate state is delivered, that raw/bulk SQL passes through SaveChanges interception, or that changes to every included relationship independently trigger the root-table observation.

## Lifetime and disposal

Arc's streaming transports dispose their observer subscriptions when clients disconnect; they do **not** complete the EF subject. EF producer cleanup is tied to subject completion, so returning a raw EF subject does not give its producer a disconnect-driven lifetime. An HTTP snapshot, including `waitForFirstResult=true`, does not close this ownership gap.

Before exposing EF observation through an observable query, establish an application-owned producer lifetime and test disconnects, concurrent clients, and shutdown. Do not complete a shared subject just because one client leaves: that would stop other clients too.

For a subject you own directly, retain both the subject and subscription and **complete the subject** when finished. Disposing the Rx subscription alone does not stop this EF observation implementation.

Lifecycle fragment inside an asynchronous method:

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

Observation recreates scopes for reads. Arc's pooled EF registrations keep the configured connection string; they do not infer per-tenant databases from the current tenant. Validate your application's tenant strategy across initial reads, notification callbacks, and pooled reuse before using observation in a multi-tenant application.

Continue with [read-only contexts](./read-only.md) and [registration](./getting-started.md).
