---
title: Cratis Arc identity development and service topologies
description: Supply development users and tenants to Lens through Cratis Arc providers, and choose where to compose identity details across services.
---

Cratis Arc development providers let Lens discover test users and tenants from your running application, so you can switch context without maintaining a separate roster in the tool. These lists supply development data; they do not authenticate a caller or prove tenant membership. For deployed applications, keep identity presentation separate from each service's authentication and authorization.

## Development Users and Tenants

Implement `ICanProvideUsers` and `ICanProvideTenants` to make your application's development roster available over HTTP. [Lens](/tools/lens/), the Cratis browser extension, reads `/.cratis/users` and `/.cratis/tenants` to populate its user and tenant pickers. See [where the Lens roster comes from](/tools/lens/#where-the-tenant-and-user-roster-comes-from) for the extension side of this contract.

A tenant roster answers which tenants the provider lists. It does not resolve the tenant for a request; [Tenancy](../tenancy/) handles that separately.

### Available Endpoints

Normal Arc activation maps these endpoints unless `Cratis:Arc:Introspection:IdentityDiscovery` is `false` or replacements with the same endpoint names already exist, even when no development providers are registered:

- `/.cratis/users` — Returns all available development users
- `/.cratis/tenants` — Returns all available development tenants

Setting `IdentityDiscovery` to `false` also removes the identity-details schema, without changing catalogs or `/.cratis/me`. To remove all discovery endpoints, [turn off both discovery switches](../introspection/index.md#turn-off-all-discovery-in-deployed-environments).

Without providers, these endpoints return empty arrays. With providers, they combine the returned lists. Their [discovery access settings](../introspection/index.md#production-access) allow anonymous access in Development and require an authenticated caller elsewhere unless `Cratis:Arc:Introspection:RequireAuthentication` is `false`. Keep development-only providers out of production discovery even when authentication is required: signing in should not expose fixture users.

### Implementing a Users Provider

Create a class implementing `ICanProvideUsers`; Arc discovers it automatically. Each `User` carries a `ClientPrincipal` describing the development identity and a separate details payload. In this example, Alice and Bob have different roles so you can exercise different UI states:

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

Implement `ICanProvideTenants` to supply the tenant picker. Return IDs and names from the list you use for development, whether it lives in code, configuration, or a database. These entries are choices for the tool, not tenant-access decisions or request-time [tenant resolution](../tenancy/):

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

With the application running, open [Lens](/tools/lens/) and check that its user and tenant pickers contain the entries your providers return. An empty picker can mean no provider is registered or its list is empty; also check that Lens can reach the discovery endpoints under your access settings.

Selecting a user or tenant makes Lens attach the corresponding identity and tenant headers to matching requests within its configured scope. The backend must still be configured to accept that development identity through its [authentication setup](../core/authentication.md). Returning a user from `ICanProvideUsers` does not establish a trusted principal by itself.

After switching context, inspect a response that uses [tenant resolution](../tenancy/resolvers.md) or refresh the frontend identity through the [provider flow](./provider-flow.md). This checks the selected context through the backend, rather than only confirming the selection in the popup.

### Multiple Providers

When users or tenants come from different sources, implement a development provider for each source. Arc discovers all registered implementations of `ICanProvideUsers` and `ICanProvideTenants` and combines their results. This differs from `IProvideIdentityDetails`, where Arc selects one provider to compose the caller's details.

## Service Topologies

Start with `IProvideIdentityDetails` in the service that has the display data the frontend needs. If that data spans services, your application can compose it at ingress, in a reverse proxy, or in a dedicated identity service. Arc does not perform that cross-service aggregation for you.

Choose the composition point according to who owns the data and the calls needed to retrieve it. Wherever you assemble the frontend payload, do not forward it as authorization evidence. Preserve trusted authentication and enforce permissions at each service boundary; the [identity provider flow](./provider-flow.md) explains where presentation data enters the response.
