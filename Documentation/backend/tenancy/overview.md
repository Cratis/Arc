---
uid: Arc.Tenancy
---

# Tenancy Overview

Tenancy means your application serves multiple customers or organizational units while keeping their data and operations isolated. A tenant could be a customer, a business unit, or any logical boundary that must remain separate from others.

## Why Tenancy Matters

Tenancy keeps data and behavior isolated between customers or organizational units while still sharing the same application deployment. Arc provides tenant resolution, tenant context access, and tenant-aware integrations to help you maintain strict separation and predictable behavior.

- **Data isolation**: Each tenant should only see its own data, even when sharing infrastructure.
- **Compliance**: Many regulatory requirements demand strict separation and auditable access.
- **Operational safety**: Isolation reduces the blast radius of mistakes, queries, and deployments.
- **Scalability**: Tenancy enables predictable scaling by segmenting traffic and storage by tenant.

Arc helps you establish a clear tenant boundary by resolving a tenant ID for each request, keeping that context available across the application, and wiring tenant-aware integrations to data stores and event streams.

## Topics

- [Tenancy overview](./overview.md)
- [Resolving tenant IDs](./resolvers.md)
- [Configuration](./configuration.md)
- [Tenant context access](./tenant-context.md)
- [Database isolation](./database-resolvers.md)
- [Custom database resolvers](./custom-database-resolvers.md)
- [Best practices](./best-practices.md)
- [Security considerations](./security.md)
