# Multi-Tenancy

Cratis Arc provides comprehensive multi-tenancy support with pluggable tenant ID resolution strategies. The framework supports multiple methods for tenant identification including HTTP headers, query parameters, user claims, and fixed development values.

## Overview

Multi-tenancy in Arc is built around these core components:

- **`ITenantIdResolver`** - Pluggable interface for resolving tenant IDs from different sources
- **`TenantIdMiddleware`** - Automatically resolves and sets the tenant ID for each request
- **`ITenantIdAccessor`** - Provides access to the current tenant ID throughout your application
- **`TenancyOptions`** - Configures the resolver type and its specific settings
- **`TenantId.NotSet`** - Sentinel value representing an unresolved tenant context

The system automatically handles tenant context propagation across async operations using `AsyncLocal<T>`, ensuring that the tenant ID is available throughout the entire request lifecycle.

## Tenant ID Resolvers

Arc provides four built-in resolver types:

### Header Resolver (Default)

Resolves the tenant ID from an HTTP header:

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseHeaderTenancy("X-Tenant-ID"); // Custom header name
});
```

**Default configuration:**
- Header name: `x-cratis-tenant-id`

### Query Parameter Resolver

Resolves the tenant ID from a query string parameter:

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseQueryTenancy("tenant"); // Custom parameter name
});
```

**Default configuration:**
- Parameter name: `tenantId`

### Claim Resolver

Resolves the tenant ID from a claim in the authenticated user's principal:

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseClaimTenancy("tenant_id"); // Custom claim type
});
```

**Default configuration:**
- Claim type: `tenant_id`

### Development Resolver

Uses a fixed tenant ID for local development:

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseDevelopmentTenancy("my-test-tenant");
});
```

**Default configuration:**
- Tenant ID: `development`

## Configuration

### Programmatic Configuration

Multi-tenancy is configured through the `ArcOptions` class using extension methods:

```csharp
builder.AddCratisArcCore(options =>
{
    // Choose your resolver type and configure it
    options.UseHeaderTenancy("X-Custom-Tenant");
    
    // Or use query parameters
    // options.UseQueryTenancy("tenant");
    
    // Or use claims
    // options.UseClaimTenancy("tenant_id");
    
    // Or use development mode
    // options.UseDevelopmentTenancy("test-tenant");
});
```

### Configuration File (appsettings.json)

You can also configure tenancy through your application's configuration:

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

**Configuration properties by resolver type:**

#### Header Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Header",
    "HttpHeader": "X-Custom-Tenant"
  }
}
```

#### Query Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Query",
    "QueryParameter": "tid"
  }
}
```

#### Claim Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Claim",
    "ClaimType": "tenant_id"
  }
}
```

#### Development Resolver

```json
{
  "Tenancy": {
    "ResolverType": "Development",
    "DevelopmentTenantId": "local-tenant"
  }
}
```

## Automatic Tenant Detection

The `TenantIdMiddleware` is automatically registered and configured to run early in the ASP.NET Core pipeline. It performs the following operations:

1. **Tenant Resolution**: Uses the configured `ITenantIdResolver` to resolve the tenant ID
2. **Context Storage**: Stores the tenant ID in the HTTP context items
3. **Async Local Setting**: Sets the tenant ID in an `AsyncLocal<TenantId>` for thread-safe access

The middleware is automatically added to your application pipeline when you configure Arc, so no manual registration is required.

## Accessing the Current Tenant

### Using ITenantIdAccessor

The primary way to access the current tenant in your application is through dependency injection of `ITenantIdAccessor`:

```csharp
using Cratis.Arc.Tenancy;

public class MyService
{
    private readonly ITenantIdAccessor _tenantIdAccessor;

    public MyService(ITenantIdAccessor tenantIdAccessor)
    {
        _tenantIdAccessor = tenantIdAccessor;
    }

    public async Task ProcessDataAsync()
    {
        var tenantId = _tenantIdAccessor.Current;
        
        // Check if tenant is set
        if (tenantId == TenantId.NotSet)
        {
            // Handle unresolved tenant scenario
            return;
        }
        
        // Use the tenant ID for tenant-specific operations
        var data = await GetTenantDataAsync(tenantId);
        
        // Process the data...
    }
}
```

## The TenantId Type

Arc provides a strongly-typed `TenantId` concept that wraps the string value:

```csharp
public record TenantId(string Value) : ConceptAs<string>(Value)
{
    public static readonly TenantId NotSet = new("[NotSet]");
    
    public static implicit operator TenantId(string value) => new(value);
}
```

This provides type safety and prevents mixing up tenant IDs with other string values in your application.

### TenantId.NotSet

The `TenantId.NotSet` value is returned when:
- No tenant ID can be resolved from the current context
- The resolver returns an empty string
- The application is running without tenant context

This allows you to handle non-tenanted scenarios gracefully.

## Integration with Other Components

### MongoDB

When using MongoDB with Arc, the database name automatically includes the tenant ID:

```text
MyDatabase             // when TenantId.NotSet
MyDatabase+AcmeCorp    // when tenant ID is "AcmeCorp"
```

### Chronicle Event Store

When using Chronicle with Arc, the event store namespace is automatically set to the tenant ID:

- When `TenantId.NotSet`: Uses `EventStoreNamespaceName.Default`
- When tenant ID is set: Uses the tenant ID as the namespace

This provides automatic tenant isolation for event streams.

## Best Practices

1. **Consistent Resolver Choice**: Choose the resolver type that best fits your application architecture
2. **Validation**: Consider adding middleware to validate that tenant IDs are valid and that the requesting user has access to the specified tenant
3. **Database Isolation**: The Arc MongoDB integration automatically handles tenant-specific database naming
4. **Logging**: Include the tenant ID in your logging context for better observability
5. **Caching**: When using caching, include the tenant ID as part of cache keys to prevent data leakage
6. **Development**: Use the Development resolver for local testing to avoid setting headers/claims manually

## Security Considerations

- Always validate that the requesting user has permission to access the specified tenant
- Consider implementing tenant validation middleware that runs after tenant detection
- Ensure that tenant IDs cannot be easily guessed or enumerated
- Log tenant switches and access patterns for security monitoring
- When using header or query parameter resolvers, ensure requests are properly authenticated
- The claim resolver is the most secure option as it ties tenant access to authenticated identities

