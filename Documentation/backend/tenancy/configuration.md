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

`BaseDomain` is the domain the application itself is served from. A host resolves a tenant only when it is exactly one label in front of it; every other host falls back to `HttpHeader`.

`BaseDomain` is **required** and must be the registrable domain you own — at least two letter-digit-hyphen labels, never an address literal. Leaving it out, or setting it to something no host could be matched against, throws `BaseDomainIsNotADomainName` on startup rather than letting every request take its tenant from the client-supplied `HttpHeader`. See [the subdomain resolver](./resolvers.md) for the full rules.

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

