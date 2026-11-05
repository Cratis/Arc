---
title: Cratis Arc tenant context access
description: Read Cratis Arc's current tenant with ITenantIdAccessor, handle default tenants explicitly, and select a tenant scope before resolving background services.
---

Inject `ITenantIdAccessor` to read the tenant selected for an operation in Cratis Arc. In a request, Arc checks that candidate against any registered `ITenantMembership` hooks. Without those hooks, reading the ID supplies context but no proof that the caller may use it. Configure [tenant membership](./membership.md) before using the context to access customer data.

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

The `NotSet` branch skips work when no tenant is selected. `LoadTenantDataAsync` is a placeholder that returns a completed task; it neither loads data nor authorizes access. A production implementation needs membership enforcement before storage access, not just the presence check shown here.

## Use a tenant in background work

Background work without an HTTP request does not invoke request membership hooks. Make the required authorization decision, then begin [an explicit tenant scope](../commands/command-pipeline.md#execute-for-a-specific-tenant). Create the dependency-injection scope for tenant-scoped services inside it, so those services resolve against the selected tenant. Do not capture a request-scoped storage service in a singleton and reuse it for later work.

Explicit scopes require Arc's `TenantIdAccessor` to be the effective `ITenantIdAccessor`. A custom accessor registered before or after Arc remains effective, but resolving `ITenantScope` then throws `ExplicitTenantScopeRequiresArcAccessor`. This prevents a scope from appearing to select a tenant that the custom accessor would not honor.

## TenantId.NotSet

`TenantId.NotSet` means no tenant could be resolved from the current context. Decide whether your application permits that case rather than silently treating it as a named customer.

`TenantId.Default` is the named default, and `IsDefault` includes both `NotSet` and `Default`. Arc's default MongoDB resolver maps both to the same unsuffixed database. The example's `NotSet` check therefore allows the named default; it does not reject every request that would use default storage. Use `IsDefault` when the decision concerns both aliases, and see [database isolation](./database-resolvers.md) for the storage mappings.
