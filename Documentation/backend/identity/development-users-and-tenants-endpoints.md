# Development Users and Tenants Endpoints

For development tooling scenarios, Arc can expose suggested users and tenants on:

- `/.cratis/users`
- `/.cratis/tenants`

These endpoints are intended for development and are discovered automatically when your application contains:

- `ICanProvideUsers` in `Cratis.Arc.Identity`
- `ICanProvideTenants` in `Cratis.Arc.Tenancy`

The users endpoint returns `User` items with:

- `MicrosoftIdentity` (`ClientPrincipal`)
- `Details` (`object`)

The tenants endpoint returns `Tenant` items with:

- `Id` (`TenantId`)
- `Name` (`TenantName`)
