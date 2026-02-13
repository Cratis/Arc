# Database Isolation

Arc provides tenant-aware database naming to keep tenant data segregated while sharing infrastructure.

## MongoDB Database Naming

The default MongoDB resolver appends the tenant ID to the base database name using a `+` separator:

```text
{BaseDatabaseName}             // when TenantId.NotSet
{BaseDatabaseName}+{TenantId}  // when tenant ID is set
```

Examples:

```text
MyDatabase             // when TenantId.NotSet
MyDatabase+acme-corp   // when tenant ID is "acme-corp"
MyDatabase+tenant-123  // when tenant ID is "tenant-123"
```

## Entity Framework Core Database Naming

Entity Framework Core follows the same naming pattern by default, ensuring that each tenant maps to its own database name.

## Chronicle Event Store Namespace

Chronicle uses the tenant ID as the event store namespace when a tenant is resolved. When no tenant is resolved, it uses the default namespace.

