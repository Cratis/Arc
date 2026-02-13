# Tenancy Configuration

You can configure tenancy programmatically or through configuration files. Both approaches map to the same options and resolver types.

## Programmatic Configuration

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseHeaderTenancy("X-Custom-Tenant");

    // options.UseQueryTenancy("tenant");
    // options.UseClaimTenancy("tenant_id");
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
        "HttpHeader": "X-Tenant-ID"
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

### Development Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Development",
    "DevelopmentTenantId": "local-tenant"
  }
}
```

