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
    // options.UseFixedTenancy("acme");
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

`BaseDomain` is **required** and must be the registrable domain you own — at least two letter-digit-hyphen labels, never an address literal. Leaving it out, or setting it to something no host could be matched against, throws `BaseDomainIsNotADomainName` and the host does not start — `UseSubdomainTenancy` throws where you call it, and a value that arrives from configuration is validated while the host is starting. Neither one lets a request through to take its tenant from the client-supplied `HttpHeader` instead. See [the subdomain resolver](./resolvers.md) for the full rules.

```json
{
  "Tenancy": {
    "ResolverType": "Subdomain",
    "BaseDomain": "myapp.com",
    "HttpHeader": "X-Custom-Tenant"
  }
}
```

### Fixed Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Fixed",
    "FixedTenantId": "acme"
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

`FixedTenantId` and `DevelopmentTenantId` are two names for the same value, so either key configures either resolver
type. Set only one of them - when a configuration source supplies both, whichever key the binder visits last wins.

