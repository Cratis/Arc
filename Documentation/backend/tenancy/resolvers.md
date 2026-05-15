# Resolving Tenant IDs

Arc resolves tenant IDs through pluggable strategies. Each request is evaluated by the configured resolver, and the resulting tenant ID becomes the active tenant context for the request lifecycle.

## Built-In Resolvers

### Header Resolver (Default)

Resolves the tenant ID from an HTTP header.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseHeaderTenancy("X-Tenant-ID");
});
```

Default header name: `x-cratis-tenant-id`

### Query Parameter Resolver

Resolves the tenant ID from a query string parameter.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseQueryTenancy("tenant");
});
```

Default parameter name: `tenantId`

### Claim Resolver

Resolves the tenant ID from a claim on the authenticated user.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseClaimTenancy("tenant_id");
});
```

Default claim type: `tenant_id`

### Subdomain Resolver

Resolves the tenant ID from the first segment of the request hostname. When the hostname has no subdomain (a single segment), it falls back to the configured HTTP header.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseSubdomainTenancy("X-Tenant-ID");
});
```

A request to `acme.myapp.com` resolves the tenant as `acme`. A request to `myapp.com` falls back to the `X-Tenant-ID` header. This pattern is useful for SaaS applications where each tenant is routed through its own subdomain.

Default fallback header: `x-cratis-tenant-id`

### Development Resolver

Uses a fixed tenant ID for local development.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseDevelopmentTenancy("my-test-tenant");
});
```

Default tenant ID: `development`

