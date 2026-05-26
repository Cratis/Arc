# Development and Topologies

## Development Users and Tenants Endpoints

For development tooling scenarios, Arc can expose suggested users and tenants on:

- `/.cratis/users`
- `/.cratis/tenants`

These endpoints are discovered automatically when your application contains:

- `ICanProvideUsers` in `Cratis.Arc.Identity`
- `ICanProvideTenants` in `Cratis.Arc.Tenancy`

Users endpoint model:

- `MicrosoftIdentity` (`ClientPrincipal`)
- `Details` (`object`)

Tenants endpoint model:

- `Id` (`TenantId`)
- `Name` (`TenantName`)

## Service Topologies

In a microservices architecture, you have several implementation options:

1. Single service: Implement `IProvideIdentityDetails` in the main service
2. Multiple services: Let ingress or reverse proxy call multiple services and merge the results
3. Dedicated identity service: Aggregate identity data in a specialized service

Choose the topology that best fits your architecture and operational model.
