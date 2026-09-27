---
title: Tenant Context Access
description: Read the resolved tenant through dependency injection, and carry a tenant into background work deliberately.
---

Once a tenant ID is resolved, Arc exposes the current tenant context through dependency injection. This is selection, not proof that the caller is a member. Background operations also need an intentional tenant execution context; use [an explicit tenant scope](../commands/command-pipeline.md#execute-for-a-specific-tenant) and create the DI scope for tenant-scoped services inside it. Do not capture a request-scoped storage service in a singleton. Explicit scopes require Arc's `TenantIdAccessor` to be the effective `ITenantIdAccessor`; a custom accessor registered before or after Arc remains effective, but resolving `ITenantScope` then throws `ExplicitTenantScopeRequiresArcAccessor` instead of silently selecting a different tenant.

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

The fragment illustrates reading context only; `LoadTenantDataAsync` does not actually load or authorize data. Production code must enforce membership before accessing storage.

## TenantId.NotSet

`TenantId.NotSet` signals that no tenant could be resolved from the current context. Handle this case explicitly so you avoid mixing tenant-aware and non-tenant operations. `TenantId.Default` is the named default, and `IsDefault` includes both `NotSet` and `Default`; Arc's default MongoDB resolver maps both to the same unsuffixed database. Checking only `NotSet` intentionally distinguishes those values and is not a complete default-storage check.
