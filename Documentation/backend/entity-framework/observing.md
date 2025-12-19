# Observing DbSet<>

Entity Framework Core observation support allows you to monitor changes to your entities in real-time using reactive extensions. This feature enables you to create observable queries that automatically update when data changes, either through your application or external database modifications.

## Configuration

To enable observation support, you need to configure both the service collection and your DbContext.

### 1. Register Observation Services

Add observation services to your service collection:

```csharp
services.AddEntityFrameworkCoreObservation();
```

This registers the necessary services for tracking entity changes and database-level notifications.

### 2. Configure DbContext

Add observation support to your DbContext configuration:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddObservation(serviceProvider);
}
```

Or when configuring in dependency injection:

```csharp
services.AddDbContext<MyDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString)
           .AddObservation(serviceProvider);
});
```

## Usage

Once configured, you can create observable queries using extension methods on `DbSet<TEntity>`.

### Observe a Collection

Monitor changes to a collection of entities:

```csharp
var observable = dbContext.MyEntities.Observe();
observable.Subscribe(entities => 
{
    // Handle updated collection
    Console.WriteLine($"Collection updated: {entities.Count()} items");
});
```

### Observe with Filter

Apply filters to observe specific entities:

```csharp
var observable = dbContext.Orders.Observe(order => order.Status == OrderStatus.Pending);
observable.Subscribe(pendingOrders => 
{
    // Handle updates to pending orders only
});
```

### Observe a Single Entity

Monitor changes to a specific entity:

```csharp
var observable = dbContext.Products.ObserveSingle(p => p.Sku == "ABC123");
observable.Subscribe(product => 
{
    // Handle updates to the specific product
});
```

### Observe by Id

Monitor a single entity using its identifier:

```csharp
var observable = dbContext.Customers.ObserveById<Customer, Guid>(customerId);
observable.Subscribe(customer => 
{
    // Handle updates to the customer
});
```

## How It Works

The observation feature combines two notification mechanisms:

1. **In-Process Changes**: Changes made through your application's `DbContext` are tracked via EF Core interceptors
2. **Database-Level Changes**: External changes are detected using database-specific notification mechanisms:
   - SQL Server: Uses `SqlDependency` or polling
   - PostgreSQL: Uses `LISTEN/NOTIFY`
   - SQLite: Uses polling

When any change is detected, the observable query is re-executed and subscribers are notified with the updated results.

## Change Detection

The observation system detects changes when:

- Entities are added, modified, or deleted through `SaveChanges()` or `SaveChangesAsync()`
- External processes modify the database (via database-level notifications)
- Changes match the filter criteria of your observable query

## Best Practices

- Use filters to limit the scope of observations and improve performance
- Dispose of subscriptions when no longer needed to prevent memory leaks
- Consider using `ObserveSingle` or `ObserveById` when monitoring individual entities
- Be mindful of database notification limits and capabilities for your specific database provider

## Example: Real-Time Dashboard

```csharp
public class OrderDashboard
{
    private readonly IDisposable _subscription;

    public OrderDashboard(MyDbContext dbContext)
    {
        _subscription = dbContext.Orders
            .Observe(o => o.Status == OrderStatus.Processing)
            .Subscribe(processingOrders =>
            {
                UpdateDashboard(processingOrders);
            });
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private void UpdateDashboard(IEnumerable<Order> orders)
    {
        // Update UI or metrics
    }
}
```
