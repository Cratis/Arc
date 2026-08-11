# Resolving Tenant IDs

Arc resolves tenant IDs through pluggable strategies. Each request is evaluated by the configured resolver, and the resulting tenant ID becomes the active tenant context for the request lifecycle.

## Built-In Resolvers

### Header Resolver (Default)

Resolves the tenant ID from an HTTP header.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseHeaderTenancy("X-Custom-Tenant");
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

Resolves the tenant ID from the subdomain of a configured base domain, and falls back to the configured HTTP header for every host that does not carry one.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseSubdomainTenancy("myapp.com", "X-Custom-Tenant");
});
```

A host carries a tenant only when it is **exactly one label in front of the base domain**. A request to `acme.myapp.com` resolves the tenant as `acme`. A request to `myapp.com` falls back to the `X-Custom-Tenant` header. This pattern is useful for SaaS applications where each tenant is routed through its own subdomain.

The number of labels in a host is never used to decide whether it carries a tenant, so the following all fall back to the header:

| Host | Resolved tenant |
|---|---|
| `acme.myapp.com` | `acme` |
| `myapp.com` (the base domain itself) | the header |
| `acme.staging.myapp.com` (more than one label) | the header |
| `10.0.0.5`, `[::1]` (IP literals) | the header |
| `otherapp.com` (an unrelated domain) | the header |
| *(any host, when no base domain is configured)* | the header |

Hosts are normalized before matching, so a trailing dot, a port, mixed casing and an internationalized name all resolve the same tenant: `ACME.MyApp.com.`, `acme.myapp.com:5000` and `acme.myapp.com` all resolve `acme`, and `münchen.myapp.com` resolves the punycode label `xn--mnchen-3ya` — the same tenant as `xn--mnchen-3ya.myapp.com`.

Use the base domain to decide what your application's own host is. If the application is served from `www.myapp.com`, configure that as the base domain and `www.myapp.com` falls back to the header while `acme.www.myapp.com` resolves `acme`. With `myapp.com` as the base domain, `www` is an ordinary tenant label like any other — no host name is treated as special.

Omitting the base domain keeps the registration valid but leaves the resolver with no way to tell a tenant host from the application's own host, so every request falls back to the header:

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseSubdomainTenancy("X-Custom-Tenant");
});
```

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

