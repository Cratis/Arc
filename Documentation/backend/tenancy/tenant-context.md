# Tenant Context Access

Once a tenant ID is resolved, Arc stores it as the current tenant context for the request. You can access this context anywhere through dependency injection.

## Accessing the Current Tenant

```csharp
using Cratis.Arc.Tenancy;

public class CustomerService(ITenantIdAccessor tenantIdAccessor)
{
    public async Task HandleAsync()
    {
        var tenantId = tenantIdAccessor.Current;

        if (tenantId == TenantId.NotSet)
        {
            return;
        }

        await LoadTenantDataAsync(tenantId);
    }

    private static Task LoadTenantDataAsync(TenantId tenantId) => Task.CompletedTask;
}
```

## TenantId.NotSet

`TenantId.NotSet` signals that no tenant could be resolved from the current context. Handle this case explicitly so you avoid mixing tenant-aware and non-tenant operations.

