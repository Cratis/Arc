# Development and Topologies

## Development Users and Tenants

For development scenarios, Arc automatically exposes HTTP endpoints that development tools use to discover available users and tenants. This eliminates the need to hard-code user lists in your frontend or development environment — just implement the providers and Arc surfaces them.

### Available Endpoints

When you implement the development providers, Arc automatically exposes:

- `/.cratis/users` — Returns all available development users
- `/.cratis/tenants` — Returns all available development tenants

These endpoints are discovered and mapped automatically. No additional configuration needed.

### Implementing a Users Provider

Create a class implementing `ICanProvideUsers`. Arc will discover it automatically.

```csharp
using Cratis.Arc.Identity;

public class DevelopmentUsersProvider : ICanProvideUsers
{
    public Task<IEnumerable<User>> Provide()
    {
        var users = new List<User>
        {
            new User(
                new ClientPrincipal
                {
                    UserId = "alice@contoso.com",
                    UserDetails = "Alice Developer",
                    IdentityProvider = "aad",
                    UserRoles = new[] { "admin", "developer" }
                },
                Details: new { Department = "Engineering" }
            ),
            new User(
                new ClientPrincipal
                {
                    UserId = "bob@contoso.com",
                    UserDetails = "Bob Tester",
                    IdentityProvider = "aad",
                    UserRoles = new[] { "tester" }
                },
                Details: new { Department = "QA" }
            )
        };

        return Task.FromResult<IEnumerable<User>>(users);
    }
}
```

### Implementing a Tenants Provider

Similarly, create a class implementing `ICanProvideTenants`:

```csharp
using Cratis.Arc.Tenancy;

public class DevelopmentTenantsProvider : ICanProvideTenants
{
    public Task<IEnumerable<Tenant>> Provide()
    {
        var tenants = new List<Tenant>
        {
            new Tenant(
                Id: new TenantId("acme-corp"),
                Name: new TenantName("ACME Corporation")
            ),
            new Tenant(
                Id: new TenantId("widget-inc"),
                Name: new TenantName("Widget Inc")
            )
        };

        return Task.FromResult<IEnumerable<Tenant>>(tenants);
    }
}
```

### How Development Tooling Uses These Endpoints

Development tools (like the Cratis Portal or custom dev dashboards) use these endpoints to populate dropdown menus and user selectors. Instead of hard-coding a list of test users or maintaining them in configuration, your code is the source of truth:

- Frontend fetches `/cratis/users` to populate user-selection dropdowns
- Frontend fetches `/cratis/tenants` to populate tenant-selection dropdowns
- Developers can switch context without rebuilding

You can have **multiple providers** — all registered providers are discovered and their results merged. This is useful when users or tenants come from different sources (database, configuration, external service).

### Multiple Providers

You can implement multiple providers of the same interface. All will be discovered and their results combined. This is useful when users or tenants come from different sources (configuration, database, external service) — each source gets its own provider, and Arc merges them automatically.

## Service Topologies

In a microservices architecture, you have several implementation options:

1. **Single service** — Implement `IProvideIdentityDetails` in the main service
2. **Multiple services** — Let ingress or reverse proxy call multiple services and merge the results
3. **Dedicated identity service** — Aggregate identity data in a specialized service

Choose the topology that best fits your architecture and operational model.
