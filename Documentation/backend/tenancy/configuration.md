# Tenancy Configuration

You can configure tenancy programmatically or through configuration files. Both approaches map to the same options and resolver types.

## Programmatic Configuration

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseHeaderTenancy("X-Custom-Tenant");

    // options.UseQueryTenancy("tenant");
    // options.UseClaimTenancy("tenant_id");
    // options.UseSubdomainTenancy("myapp.com", "X-Custom-Tenant");
    // options.UseDevelopmentTenancy("test-tenant");
});
```

## Configuration File (appsettings.json)

```json
{
  "Cratis": {
    "Arc": {
      "Tenancy": {
        "ResolverType": "Header",
        "HttpHeader": "X-Custom-Tenant"
      }
    }
  }
}
```

## Resolver-Specific Settings

### Header Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Header",
    "HttpHeader": "X-Custom-Tenant"
  }
}
```

### Query Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Query",
    "QueryParameter": "tenant"
  }
}
```

### Claim Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Claim",
    "ClaimType": "tenant_id"
  }
}
```

### Subdomain Resolver

`BaseDomain` is the domain the application itself is served from. A host resolves a tenant only when it is exactly one label in front of it; every other host falls back to `HttpHeader`. Leaving `BaseDomain` out makes every request fall back to the header.

```json
{
  "Tenancy": {
    "ResolverType": "Subdomain",
    "BaseDomain": "myapp.com",
    "HttpHeader": "X-Custom-Tenant"
  }
}
```

### Development Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Development",
    "DevelopmentTenantId": "local-tenant"
  }
}
```

