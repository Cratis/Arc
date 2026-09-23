# Development and Topologies

## Development Users and Tenants

For development scenarios, Arc automatically exposes HTTP endpoints that development tools use to discover available users and tenants. This eliminates the need to hard-code user lists in your frontend or development environment — just implement the providers and Arc surfaces them.

The primary consumer of these endpoints is [Lens](/tools/lens/), the Cratis browser extension for exercising a running Arc application during development. Lens reads `/.cratis/tenants` and `/.cratis/users` to populate its tenant and user pickers, then injects the corresponding identity and tenant headers into every request your frontend makes while Lens is active — see [Lens: where the tenant and user roster comes from](/tools/lens/#where-the-tenant-and-user-roster-comes-from) for the extension side of this contract, with screenshots against seeded demo data. This is also the [Tenancy](../tenancy/) discovery seam's development counterpart: it answers "which tenants exist", not "which tenant is this request for".

### Available Endpoints

Normal Arc activation maps these endpoints unless replacements with the same endpoint names already exist, even when no development providers are registered:

- `/.cratis/users` — Returns all available development users
- `/.cratis/tenants` — Returns all available development tenants

Without providers they return empty arrays. With providers they combine their results. **There is no Development environment check:** the endpoints are explicitly anonymous in Production too. ASP.NET fallback authorization policies do not protect explicitly anonymous endpoints. Exclude development-only provider implementations from production discovery and restrict these paths at trusted ingress when discovery is not intended to be public. A user/tenant list is not proof of authentication or membership.

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

`ICanProvideTenants` is a development-only seam: it has nothing to do with tenant *resolution* at request time (see [Tenancy](../tenancy/)). It exists purely so a tool like Lens can ask your running application, at any moment, "which tenants do you know about right now?" — sourced from wherever your application already keeps that list (a fixed set, a database, a configuration section), rather than a hand-maintained copy pasted into the tool.

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

Development tools — [Lens](/tools/lens/) chief among them, plus the Cratis Portal or custom dev dashboards — use these endpoints to populate dropdown menus and user/tenant selectors. Instead of hard-coding a list of test users or maintaining them in configuration, your code is the source of truth:

- The tool fetches `/.cratis/users` to populate user-selection dropdowns
- The tool fetches `/.cratis/tenants` to populate tenant-selection dropdowns
- Developers can switch context without rebuilding

With Lens specifically: once a tenant or user is selected in the extension popup, every subsequent request from the page under inspection carries the matching tenant header and identity headers, so your backend's [tenant resolution](../tenancy/resolvers.md) and [identity provider](./provider-flow.md) see the switch exactly as they would from a real caller — no restart, no manually crafted headers.

You can have **multiple providers** — all registered providers are discovered and their results merged. This is useful when users or tenants come from different sources (database, configuration, external service).

### Multiple Providers

You can implement multiple providers of the same interface. All will be discovered and their results combined. This is useful when users or tenants come from different sources (configuration, database, external service) — each source gets its own provider, and Arc merges them automatically.

## Service Topologies

In a microservices architecture, you have several implementation options:

1. **Single service** — Implement `IProvideIdentityDetails` in the main service
2. **Multiple services** — Let ingress or reverse proxy call multiple services and merge the results
3. **Dedicated identity service** — Aggregate identity data in a specialized service

Choose the topology that best fits your architecture and operational model. Cross-service aggregation is application-owned; do not forward the unsigned identity cookie as authorization evidence. Preserve trusted authentication and enforce permissions at each service boundary.
